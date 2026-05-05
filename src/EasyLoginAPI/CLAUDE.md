# EasyLogin Backend

Clean Architecture .NET 10 auth template. SQL Server + ASP.NET Core Identity + JWT.

## Project Structure

```
EasyLogin.Domain          → Pure POCOs, zero NuGet deps
EasyLogin.Application     → MediatR, FluentValidation, interfaces, DTOs, commands/queries
EasyLogin.Infrastructure  → EF Core, Identity, JWT, email, Mapster, repositories
EasyLoginAPI              → Controllers, GlobalExceptionHandler, Program.cs, Scalar docs
```

## Key Decisions

- **.NET 10** — not 9
- **CQRS via MediatR 14** — all business logic in handlers; controllers only dispatch
- **FluentValidation 12** — validators in `Application/Auth/Validators/`; `ValidationBehavior` runs in pipeline before handler
- **LoggingBehavior** — logs every request/response via MediatR pipeline
- **Scalar** for API docs — not Swashbuckle
- **Refresh tokens in JSON body** — not HttpOnly cookie; SHA-256 hash stored in DB (`RefreshTokenHash`), raw token sent to client
- **JWT**: HS256, claims: `sub`, `email`, `firstName`, `lastName`, `jti`, role claims; `AccessTokenExpiryMinutes=15`, `RefreshTokenExpiryDays=7`
- **Password policy**: min 8 chars, ≥1 uppercase, ≥1 digit
- **Admin seed**: `ADMIN_EMAIL` / `ADMIN_PASSWORD` env vars via `DataSeeder.SeedAsync()` (runs migrations too)
- **Pagination**: max page size 100, enforced in `GetAllUsersQueryHandler`
- **CORS**: dev allows `http://localhost:4200`; prod reads `AllowedOrigins` config (comma-separated)
- **Serilog**: daily rolling log to `logs/easylogin-.log`
- **Domain vs Identity split**: `ApplicationUser` (Domain POCO) ↔ `AppIdentityUser` (Identity entity) mapped via Mapster
- **Lockout**: Identity built-in; 5 fail attempts → 15 min lock; checked + bumped in `UserRepository.ValidateCredentialsAsync` via `AccessFailedAsync`/`IsLockedOutAsync`/`ResetAccessFailedCountAsync`
- **Code style**: use explicit types instead of `var` — declare the actual type on every local variable
- **Audit**: append-only `AuditLogs` table covering auth events + User/Role mutations (no reads). Schema splits actor (who) from target (whom): `ActorUserId`/`ActorEmail`, `TargetType`/`TargetId`/`TargetDisplay`. `AuditLogger` (Infrastructure) auto-fills actor from JWT claims when handler doesn't pass it. UA parsed via `UAParser` NuGet. Direct injection in handlers (no MediatR pipeline) — synchronous write. Updates capture before/after diff into `MetadataJson` via `AuditDiffBuilder`

## API Routes

| Method | Route | Auth | Handler |
|--------|-------|------|---------|
| POST | `/api/auth/register` | None | `RegisterCommand` |
| POST | `/api/auth/login` | None | `LoginCommand` |
| POST | `/api/auth/forgot-password` | None | `ForgotPasswordCommand` |
| POST | `/api/auth/reset-password` | None | `ResetPasswordCommand` |
| POST | `/api/auth/refresh` | None | `RefreshTokenCommand` |
| POST | `/api/auth/revoke` | Authorized | `RevokeTokenCommand` |
| GET | `/api/admin/users` | Admin | `GetAllUsersQuery` (paginated) |
| GET | `/api/admin/users/{id}` | Admin | `GetUserByIdQuery` |
| POST | `/api/admin/users` | Admin | `AdminCreateUserCommand` |
| PUT | `/api/admin/users/{id}` | Admin | `UpdateUserCommand` |
| DELETE | `/api/admin/users/{id}` | Admin | `DeleteUserCommand` |
| GET | `/api/admin/roles` | Admin | `GetAllRolesQuery` |
| POST | `/api/admin/roles` | Admin | `CreateRoleCommand` |
| DELETE | `/api/admin/roles/{id}` | Admin | `DeleteRoleCommand` |
| GET | `/api/user/profile` | Authorized | `GetCurrentUserQuery` |
| GET | `/api/superadmin/audit` | SuperAdmin | `GetAuditLogsQuery` (paginated, filterable) |

## Email System

- **Provider selection**: `Email:Provider` config — `"SendGrid"` or `"Smtp"` (factory in `InfrastructureServiceExtensions`)
- **SendGrid**: SDK-based, reads `Email:SendGridApiKey` and `Email:From`
- **SMTP**: MailKit, reads `Email:SmtpHost/SmtpPort/SmtpUser/SmtpPassword`; dev default = `localhost:1025`
- **Templates**: HTML files embedded in assembly at `EasyLogin.Infrastructure.EmailTemplates.{templateName}.html`; placeholder syntax `{{{key}}}`
- **IEmailTemplateRenderer**: interface in Application, implemented by `EmbeddedEmailTemplateRenderer` in Infrastructure

## Adding a New Feature

1. Add interface to `Application/Interfaces/` if new external dependency needed
2. Add command/query + DTO to `Application/Auth/Commands/` or `Queries/`
3. Add FluentValidation validator to `Application/Auth/Validators/`
4. Implement handler (inject interfaces, not concrete services)
5. Add Infrastructure implementation if new interface added
6. Register in `ApplicationServiceExtensions` or `InfrastructureServiceExtensions`
7. Add controller endpoint → dispatch via `_mediator.Send()`

## Docker

- `docker-compose.yml` at repo root; `.env` file holds secrets
- **db**: SQL Server 2022, port 1433, volume `sqldata`, health check
- **api**: builds from `./src/EasyLoginAPI`, port 8080, depends on db health
- **frontend**: builds from `./src/EasyLoginUI`, port 4200, depends on api
