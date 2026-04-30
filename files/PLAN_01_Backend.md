# Implementation plan — Backend (.NET 9, Clean Architecture, Docker)

> **For the AI reading this:** Follow each phase in order. Do not skip ahead.
> Ask the developer a question any time you are unsure about a naming convention,
> a configuration value, a business rule, or anything not explicitly stated here.
> Never guess at secrets, connection strings, or environment-specific values — ask first.

---

## Context & decisions already made

| Topic | Decision |
|---|---|
| Framework | .NET 9 |
| Architecture | Clean Architecture (Domain → Application → Infrastructure → API) |
| API style | Controllers only |
| Database | SQL Server via EF Core |
| Auth | ASP.NET Core Identity + JWT access token (15 min) + Refresh token (7 days, stored as a SHA-256 hash in the database) |
| Roles | `Admin` and `User` seeded at startup; extensible via database at runtime |
| Email | SendGrid (production) + MailKit/SMTP (local dev fallback) |
| Mapping | Mapster |
| Validation | FluentValidation |
| CQRS dispatch | MediatR |
| Logging | Serilog (Console + File sinks) |
| Containerisation | Docker + Docker Compose |
| Tests | None in this plan |

---

## Solution structure

```
MyApp.sln
src/
  MyApp.Domain/
  MyApp.Application/
  MyApp.Infrastructure/
  MyApp.API/
docker-compose.yml
docker-compose.override.yml   ← dev overrides (SMTP, MailHog, hot-reload)
.env.example                  ← documents every required environment variable
.gitignore                    ← must include .env and user-secrets
```

Project references follow the dependency rule strictly — outer layers depend inward,
never the reverse:

```
MyApp.API  →  MyApp.Infrastructure  →  MyApp.Application  →  MyApp.Domain
```

---

## Phase 1 — Domain layer

**Goal:** Define the core entities. Shape only — no business logic, no framework attributes.

`MyApp.Domain` has **zero** NuGet or project references. It is pure C# with no
framework dependencies.

### Tasks

1. Create `ApplicationUser` as a plain POCO class. Properties:
   `Id` (string — will be mapped to Identity's Id), `FirstName`, `LastName`,
   `Email`, `RefreshTokenHash` (nullable string — stores the SHA-256 hash of the issued
   refresh token, never the raw value), `RefreshTokenExpiry` (nullable DateTimeOffset),
   `IsActive` (bool, default true), `CreatedAt` (DateTimeOffset, set on construction,
   never updated).

2. Create `ApplicationRole` as a plain POCO class. Properties:
   `Id` (string), `Name`, `Description` (nullable string), `CreatedAt` (DateTimeOffset),
   `IsSystemRole` (bool — true for seeded roles, false for roles added at runtime).

3. These are pure domain representations. The Infrastructure layer will map them to/from
   Identity entities. Do not add EF Core attributes, data annotations, navigation
   properties, or any reference to ASP.NET Core namespaces anywhere in this project.

> **Ask the developer:** Are there any other domain entities needed for this template
> beyond user and role — for example an audit log, a user profile, or a tenant?

---

## Phase 2 — Application layer

**Goal:** Define what the system can do. Commands, queries, interfaces, DTOs, and validators.
No implementation here — only contracts and orchestration logic.

### 2.1 — Interfaces (ports)

Declare these interfaces. Implementations live in Infrastructure.

- `ITokenService`
  - `string GenerateAccessToken(ApplicationUser user, IList<string> roles)`
  - `string GenerateRefreshToken()` — returns a cryptographically random raw string
  - `ClaimsPrincipal GetPrincipalFromExpiredToken(string token)`

- `IEmailService`
  - `Task SendAsync(string to, string subject, string htmlBody)`

- `ICurrentUserService`
  - `string? UserId` — reads from the current HTTP context claim

### 2.2 — DTOs

Create request and response record types for every auth operation. Keep them in
`Application/Auth/Dtos/`. Do not expose domain entities directly through the API.

Request records: `LoginRequest`, `RegisterRequest`, `ForgotPasswordRequest`,
`ResetPasswordRequest`, `RefreshTokenRequest`, `RevokeTokenRequest`.

Response records: `AuthResponse` (contains `accessToken`, `refreshToken`, `expiresIn`),
`UserProfileResponse`, `UserListItemResponse`.

Also create a `PaginatedList<T>` wrapper (with `items`, `pageNumber`, `totalPages`,
`totalCount`, `hasPreviousPage`, `hasNextPage`) for the paginated user list endpoint.

### 2.3 — Commands and queries (MediatR)

One command or query class plus one handler class per operation, co-located in a feature folder
(`Application/Auth/Commands/`, `Application/Auth/Queries/`).

| Name | Type | Description |
|---|---|---|
| `LoginCommand` | Command | Validate credentials, issue token pair |
| `RegisterCommand` | Command | Create user, assign `User` role, return token pair |
| `ForgotPasswordCommand` | Command | Generate reset token via Identity, dispatch email |
| `ResetPasswordCommand` | Command | Apply new password via reset token |
| `RefreshTokenCommand` | Command | Validate hashed refresh token, rotate both tokens |
| `RevokeTokenCommand` | Command | Clear `RefreshTokenHash` and `RefreshTokenExpiry` on the user |
| `GetCurrentUserQuery` | Query | Return profile of the authenticated user |
| `GetAllUsersQuery` | Query | Return paged list of users with their roles (Admin only) |

The `RefreshTokenCommand` handler must:
1. Extract the user ID from the expired access token via `GetPrincipalFromExpiredToken`.
2. Load the user from the database.
3. Hash the incoming raw refresh token with SHA-256.
4. Compare the hash to the stored `RefreshTokenHash`.
5. Verify `RefreshTokenExpiry` has not passed.
6. If valid: generate new access and refresh tokens, hash the new refresh token and store it.
7. If invalid or expired: throw `UnauthorizedAccessException`.

This ensures the database never contains a usable raw token.

### 2.4 — Validators

One `AbstractValidator<T>` per request DTO, co-located with its command.

Minimum rules to enforce:
- All required fields: `NotEmpty()`
- Email fields: `EmailAddress()`
- Password fields: minimum length (ask the developer — suggest 8), at least one digit,
  at least one uppercase letter
- `ConfirmPassword` on `RegisterRequest` and `ResetPasswordRequest`: must match `Password`

> **Ask the developer:** What is the required minimum password length?
> What complexity rules apply — digits, uppercase, special characters?

### 2.5 — MediatR pipeline behaviours

Register a `ValidationBehavior<TRequest, TResponse>` that implements
`IPipelineBehavior<TRequest, TResponse>` (constrained to `IRequest<TResponse>` only —
do not apply it to `INotification` handlers). It runs all registered validators for the
request type and throws a `ValidationException` if any fail. This fires automatically
before every command and query handler.

Optionally, also register a `LoggingBehavior<TRequest, TResponse>` that logs the
request type, execution time, and whether it succeeded or failed via Serilog.
This is useful for debugging and performance tracing.

### 2.6 — Application service registration

Create `ApplicationServiceExtensions` with an `AddApplication(this IServiceCollection services)`
extension method. Register MediatR (scanning the Application assembly), FluentValidation
(scanning the Application assembly), and all pipeline behaviours. `Program.cs` calls
`builder.Services.AddApplication()`.

---

## Phase 3 — Infrastructure layer

**Goal:** Implement every interface declared in Application. Wire up EF Core, Identity,
token generation, and email sending.

### 3.1 — Identity entity mapping

The Domain layer defines `ApplicationUser` and `ApplicationRole` as POCOs. The Infrastructure
layer needs to bridge these to ASP.NET Core Identity.

Create `AppIdentityUser : IdentityUser` in Infrastructure with the same extra properties
as the domain `ApplicationUser` (`FirstName`, `LastName`, `RefreshTokenHash`,
`RefreshTokenExpiry`, `IsActive`, `CreatedAt`).

Create `AppIdentityRole : IdentityRole` with the same extra properties as domain
`ApplicationRole` (`Description`, `CreatedAt`, `IsSystemRole`).

Create Mapster mapping profiles to convert between the domain POCOs and the Identity entities
in both directions. All command/query handlers in Application work with the domain POCOs.
The Infrastructure repositories and Identity calls use the Identity entities and map
to/from the domain types at the boundary.

### 3.2 — Database context

Create `AppDbContext : IdentityDbContext<AppIdentityUser, AppIdentityRole, string>`.
Configure entities using separate `IEntityTypeConfiguration<T>` classes inside a
`Persistence/Configurations/` folder. Do not put configuration logic inside `OnModelCreating`
directly — call `modelBuilder.ApplyConfigurationsFromAssembly(...)` instead.

Important: add a database index on `AppIdentityUser.RefreshTokenHash` so that looking up
a user by token hash does not require a full table scan. Configure this in the
`AppIdentityUserConfiguration` class:
```
builder.HasIndex(u => u.RefreshTokenHash).HasFilter("[RefreshTokenHash] IS NOT NULL");
```

### 3.3 — Role and admin seeding

Create a static `DataSeeder` class with a `SeedAsync(IServiceProvider services)` method.
It must be idempotent — safe to run on every startup.

Steps:
1. Use `RoleManager<AppIdentityRole>` to create `Admin` and `User` roles (set
   `IsSystemRole = true`) if they do not exist.
2. Read admin credentials from configuration/environment variables. If either value is
   missing or empty, throw a clear startup exception — do not continue with a default.
3. Use `UserManager<AppIdentityUser>` to create the admin user if no user with that email
   exists. Assign the `Admin` role.

> **Ask the developer:** What environment variable names should hold the admin seed
> email and password? Suggested names: `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD`.

### 3.4 — TokenService

Implement `ITokenService`:

- `GenerateAccessToken`: build a `JwtSecurityToken` with claims `sub` (user ID), `email`,
  `role` (one claim per role), `jti` (new GUID), `firstName`, `lastName`.
  Sign with `HmacSha256` using the key from configuration. Return the serialised token
  string.
- `GenerateRefreshToken`: return `Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))`.
  This is the raw token — the caller is responsible for hashing before storage.
- `GetPrincipalFromExpiredToken`: use `TokenValidationParameters` with
  `ValidateLifetime = false` to extract claims from an expired token. Throw if the
  signing algorithm is not `HmacSha256`.

JWT configuration keys (read from `appsettings` / env): `Jwt__Key`, `Jwt__Issuer`,
`Jwt__Audience`, `Jwt__AccessTokenExpiryMinutes`, `Jwt__RefreshTokenExpiryDays`.

### 3.5 — EmailService

Two implementations of `IEmailService`:

- `SendGridEmailService`: use the `SendGrid` NuGet SDK. Read `Email__SendGridApiKey`
  and `Email__From` from configuration.
- `SmtpEmailService`: use `MailKit`. Read host, port, user, and password from configuration.
  Used in local Docker dev via MailHog.

Registration: read `Email__Provider` from configuration. If `"SendGrid"` register
`SendGridEmailService`; if `"Smtp"` register `SmtpEmailService`; otherwise throw a
startup exception naming the invalid value.

### 3.6 — CurrentUserService

Implement `ICurrentUserService` using `IHttpContextAccessor`. Read the `sub` claim
from `HttpContext.User`. Register `IHttpContextAccessor` in DI if not already registered.

### 3.7 — Infrastructure service registration

Create `InfrastructureServiceExtensions` with
`AddInfrastructure(this IServiceCollection services, IConfiguration config)`.

Register: `AppDbContext` (SQL Server, connection string from
`ConnectionStrings__DefaultConnection`), ASP.NET Core Identity with `AppIdentityUser`
and `AppIdentityRole` (configured with password rules matching the validators),
`TokenService`, `EmailService` (provider-conditional), `CurrentUserService`,
Mapster mapping configurations,
JWT Bearer authentication (validation parameters matching `TokenService` settings).

`Program.cs` calls `builder.Services.AddInfrastructure(config)`.

---

## Phase 4 — API layer

**Goal:** Expose all operations through controllers. Handle cross-cutting concerns centrally.

### 4.1 — AuthController

Route prefix: `api/auth`. No `[Authorize]` on the controller — apply it per action
where needed.

| Action | Method | Route | Authorize |
|---|---|---|---|
| Register | POST | `register` | No |
| Login | POST | `login` | No |
| ForgotPassword | POST | `forgot-password` | No |
| ResetPassword | POST | `reset-password` | No |
| RefreshToken | POST | `refresh` | No |
| RevokeToken | POST | `revoke` | Yes — any authenticated role |

Each action follows this pattern: receive the request DTO, send it via `IMediator.Send()`,
return an appropriate `IActionResult`. Do not put any business logic in the controller.

Return conventions:
- Success: `200 Ok` or `201 Created` with response DTO
- Validation failure: handled globally (see 4.4)
- Auth failure: `401 Unauthorized` with a generic message
- Duplicate email on register: `409 Conflict`
- Not found: `404 NotFound` with a generic message

### 4.2 — UserController

Route prefix: `api/user`. `[Authorize]` on the controller (any authenticated user).

| Action | Method | Route | Description |
|---|---|---|---|
| GetProfile | GET | `profile` | Returns current user's profile via `GetCurrentUserQuery` |

### 4.3 — AdminController

Route prefix: `api/admin`. `[Authorize(Roles = "Admin")]` on the controller.

| Action | Method | Route | Description |
|---|---|---|---|
| GetUsers | GET | `users` | Returns paged user list via `GetAllUsersQuery` |

Pagination: accept `pageNumber` (default 1) and `pageSize` (default 20) query parameters.
Enforce a maximum page size to prevent abuse.

> **Ask the developer:** What should the maximum page size be? Suggested: 100.

### 4.4 — Global exception handler

Implement `IExceptionHandler` (.NET 9 built-in interface) to handle exceptions centrally.

| Exception type | HTTP status | Response body |
|---|---|---|
| `ValidationException` (FluentValidation) | 400 | `{ errors: { field: [messages] } }` |
| `UnauthorizedAccessException` | 401 | `{ message: "Unauthorised" }` |
| `KeyNotFoundException` | 404 | `{ message: "Not found" }` |
| `InvalidOperationException` (duplicate email) | 409 | `{ message: "..." }` |
| Anything else | 500 | `{ message: "An unexpected error occurred" }` |

Log every 500 error with Serilog at `Error` level including the full exception.
Never include stack traces or inner exception messages in the response body.

### 4.5 — Swagger / OpenAPI

Configure `Swashbuckle` with a JWT Bearer security definition so the Swagger UI
shows an `Authorize` button. Only register Swagger middleware when
`app.Environment.IsDevelopment()` is true.

### 4.6 — CORS

Register a named CORS policy. In development, allow `http://localhost:4200`.
In production, read allowed origins from `AllowedOrigins` configuration (comma-separated).

The CORS policy must call `.AllowCredentials()` if the refresh token is sent via
`HttpOnly` cookie (see the frontend plan's open question on token transport).
If the refresh token is sent in the JSON body instead, `.AllowAnyHeader()` is sufficient.

Apply the policy with `app.UseCors(policyName)`.

> **Ask the developer:** What will the production frontend URL be?

### 4.7 — Program.cs middleware order

The middleware pipeline must be registered in this exact order:

```
UseHttpsRedirection
UseCors               ← must come before UseAuthentication
UseAuthentication
UseAuthorization
MapControllers
```

Call `await DataSeeder.SeedAsync(app.Services)` after `app.Build()` and before `app.Run()`.
Wrap the seed call in a try/catch — log and rethrow on failure so the container exits
with a non-zero code rather than starting in a broken state.

---

## Phase 5 — Docker

### 5.1 — Dockerfile (API)

Multi-stage build:

1. **restore** stage (`mcr.microsoft.com/dotnet/sdk:9.0`): copy `.sln` and all `.csproj`
   files first, run `dotnet restore`. This layer is cached unless dependencies change.
2. **build** stage: copy remaining source, run `dotnet publish -c Release -o /app/publish`.
3. **runtime** stage (`mcr.microsoft.com/dotnet/aspnet:9.0`): copy from publish stage,
   create a non-root user and group, switch to that user, expose port 8080, set `ENTRYPOINT`.

Do not copy `.env`, `appsettings.Development.json`, or any secret files into the image.

### 5.2 — docker-compose.yml (production-like)

```yaml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${SA_PASSWORD}
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$$SA_PASSWORD" -C -Q "SELECT 1" -b
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s

  api:
    build:
      context: .
      dockerfile: src/MyApp.API/Dockerfile
    depends_on:
      db:
        condition: service_healthy
    env_file: .env
    ports:
      - "8080:8080"

volumes:
  sqldata:
```

The connection string in `.env` must use the service name `db` as the SQL Server hostname.

### 5.3 — docker-compose.override.yml (dev)

Additions for local development only:

- Add `mailhog` service (`mailhog/mailhog`, expose port 1025 for SMTP and 8025 for
  the web UI).
- Override `Email__Provider=Smtp` and point SMTP host to `mailhog`.
- Override `ASPNETCORE_ENVIRONMENT=Development` to enable Swagger.
- Mount the API source folder as a volume and use `dotnet watch run` as the command
  for hot-reload during development.

### 5.4 — .env.example

Commit this file. Developers copy it to `.env` and fill in values. `.env` itself is
git-ignored.

```
# SQL Server
SA_PASSWORD=
DB_NAME=MyAppDb

# Connection string (uses 'db' as hostname inside Docker)
ConnectionStrings__DefaultConnection=Server=db;Database=MyAppDb;User Id=sa;Password=<SA_PASSWORD>;TrustServerCertificate=true

# JWT
Jwt__Key=
Jwt__Issuer=MyApp
Jwt__Audience=MyApp

# Email
Email__Provider=SendGrid
Email__From=
Email__SendGridApiKey=

# SMTP (dev only — used by docker-compose.override.yml)
Email__SmtpHost=mailhog
Email__SmtpPort=1025

# Admin seed
SEED_ADMIN_EMAIL=admin@myapp.com
SEED_ADMIN_PASSWORD=

# CORS
AllowedOrigins=http://localhost:4200
```

---

## Phase 6 — Configuration files

### appsettings.json (committed — no secrets)

Contains structural defaults only. All secret or environment-specific values must be
overridden by environment variables or user secrets. Include skeleton sections for
`Jwt`, `Email`, `Serilog`, `AllowedHosts`, and `ConnectionStrings` with empty or
safe default values.

### appsettings.Development.json (committed — dev non-secrets only)

Override `Email__Provider` to `Smtp`. Set `Serilog` minimum level to `Debug`.
Do not include any passwords, keys, or API tokens.

### .gitignore additions

Must include: `.env`, `*.user`, `appsettings.*.local.json`, `**/bin/`, `**/obj/`.

---

## Phase 7 — Final checklist before handing off to frontend

Work through each item. Do not mark complete until manually verified.

- [ ] `docker-compose up` starts all services cleanly with exit code 0
- [ ] EF Core migrations run automatically on startup via `Database.MigrateAsync()`
- [ ] `Admin` and `User` roles exist in the database after first run
- [ ] Admin user exists in the database and can log in
- [ ] `POST /api/auth/register` → creates user with `User` role, returns `AuthResponse`
- [ ] `POST /api/auth/register` with duplicate email → returns `409 Conflict`
- [ ] `POST /api/auth/login` → returns `accessToken` and `refreshToken`
- [ ] `GET /api/user/profile` → `401` with no token, `200` with valid `User` token
- [ ] `GET /api/admin/users` → `403` with `User` token, `200` with `Admin` token
- [ ] `POST /api/auth/refresh` → returns new token pair, old refresh token is invalidated
- [ ] `POST /api/auth/revoke` → refresh token is cleared, subsequent refresh returns `401`
- [ ] `POST /api/auth/forgot-password` → email visible in MailHog at `http://localhost:8025`
- [ ] `POST /api/auth/reset-password` → password is changed, user can log in with new password
- [ ] Swagger UI accessible at `/swagger` in Development, returns `404` in Production
- [ ] No secrets committed to git
- [ ] `RefreshTokenHash` column has a filtered database index

---

## Questions the AI must ask the developer before starting

1. Minimum password length and complexity rules (digits, uppercase, special characters)?
2. Environment variable names for the seeded admin credentials — accept `SEED_ADMIN_EMAIL`
   and `SEED_ADMIN_PASSWORD` or rename?
3. Maximum page size for the admin user list (suggested: 100)?
4. Production frontend URL (needed for the CORS policy)?
5. Any additional domain entities needed beyond `ApplicationUser` and `ApplicationRole`?
6. Should the refresh token be returned in the JSON response body, or set as an `HttpOnly`
   cookie by the API? (This affects CORS configuration and the frontend implementation.)
