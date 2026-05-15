# EasyLogin Backend

Clean Architecture .NET 10 auth template. Multi-tenant. SQL Server + ASP.NET Core Identity + JWT + SignalR-ready scaffolding.

## Project Structure

```
EasyLogin.Domain          → Pure POCOs (entities, enums), zero NuGet deps
EasyLogin.Application     → MediatR, FluentValidation, interfaces, DTOs
                            ├── Auth/        → login, 2FA, invites, users, roles, audit, overview
                            └── Tenants/     → tenant + tenant-role + tenant-user CQRS
EasyLogin.Infrastructure  → EF Core, Identity, JWT, email, SMS, Mapster, repositories
                            ├── Persistence/         → AppDbContext, Configurations/, Migrations/
                            ├── Identity/            → AppIdentityUser, MappingProfiles/
                            ├── Services/            → TokenService, TwoFactorService, Email*, TwilioSmsSender, AppUrlProvider, AuditLogger
                            └── Repositories/        → User, Role, Tenant, TenantRole, Overview, AuditLogQuery
EasyLoginAPI              → Controllers, GlobalExceptionHandler, Program.cs, Scalar docs
```

## Layering Rules

- **Domain** has no project references. Entities expose plain properties + small enums (`UserStatus`, `AuditEventType`).
- **Application** references Domain only. Defines all interfaces (`IUserRepository`, `IEmailService`, `ITokenService`, `ITwoFactorService`, `ISmsSender`, `IAuditLogger`, `ICurrentUserService`, `IAppUrlProvider`, `IEmailTemplateRenderer`, …).
- **Infrastructure** implements Application interfaces. Owns EF entities (`AppIdentityUser`, `AppIdentityRole`) and Mapster profiles that map them to Domain types.
- **API** references Application + Infrastructure (composition root only).

## Key Decisions

- **.NET 10** — not 9.
- **CQRS via MediatR 14** — every endpoint is `mediator.Send(new XCommand/Query(...))`. No business logic in controllers.
- **FluentValidation 12** — validators in `Application/Auth/Validators/` and `Application/Tenants/Validators/`. `ValidationBehavior` runs in the pipeline before each handler.
- **LoggingBehavior** — logs every request/response via MediatR pipeline.
- **Scalar** for API docs (not Swashbuckle).
- **JWT**: HS256, claims: `sub`, `email`, `firstName`, `lastName`, `jti`, `tenantId` (when applicable), role claims. `AccessTokenExpiryMinutes=15`, `RefreshTokenExpiryDays=7`.
- **Refresh tokens in JSON body** — SHA-256 hash stored in DB (`RefreshTokenHash`), raw token sent to client. Not HttpOnly cookie.
- **Password policy**: min 8 chars, ≥1 uppercase, ≥1 digit (no non-alphanumeric required).
- **Lockout**: Identity built-in; 5 failed attempts → 15 min lock. Checked + bumped in `UserRepository.ValidateCredentialsAsync` via `AccessFailedAsync` / `IsLockedOutAsync` / `ResetAccessFailedCountAsync`.
- **Seed data**: `DataSeeder.SeedAsync()` runs migrations, creates system roles, and seeds hardcoded demo users/tenant data.
- **Pagination**: max page size 100, enforced in handlers.
- **CORS**: dev allows `http://localhost:4200`; prod reads `AllowedOrigins` config (comma-separated).
- **Serilog**: daily rolling log to `logs/easylogin-.log`.
- **Domain vs Identity split**: `ApplicationUser` (Domain POCO) ↔ `AppIdentityUser` (Identity entity), mapped via Mapster (`IdentityMappingConfig`).
- **Code style**: explicit types instead of `var` — declare the actual type on every local variable.
- **Database**: no backward-compatibility required — drop and recreate freely; migrations can be reset anytime; never worry about preserving existing data.

## Multi-Tenancy

- **Two role surfaces**:
  - **System roles** (`AspNetRoles` via Identity): `SuperAdmin`, `TenantAdmin`, `User`. Drive top-level controller authorization.
  - **Tenant roles** (`TenantRoles` table): per-tenant, app-defined. Linked to users via `UserTenantRoles` junction.
- **Tenant scoping**: `TenantAdminController` derives `CallerTenantId` from `ICurrentUserService.TenantId` (JWT claim). Every tenant handler accepts a tenant id and rejects cross-tenant access.
- **SuperAdmin** sees / mutates anything; can pass an explicit `tenantId` query param to scope to one tenant.
- **Invites**: `InviteToken` entity — email + token + tenant + roles. `/api/auth/invite/validate` + `/api/auth/accept-invite` flow.

## Two-Factor Authentication

- **TOTP** (authenticator app) via `TwoFactorService` using Identity's `GenerateTwoFactorTokenAsync` / `VerifyTwoFactorTokenAsync` with the `Authenticator` provider.
- **Email 2FA** via the `Email` token provider; code mailed through `IEmailService`.
- **SMS sender scaffolded** (`ISmsSender` + `TwilioSmsSender`) — not yet wired to a 2FA path.
- **Login flow**: `POST /auth/login` returns either tokens or a `TwoFactorToken`; client then calls `POST /auth/login/verify-2fa`.

## Audit

Append-only `AuditLogs` table covering auth events + User/Role/Tenant mutations (no reads). Schema splits actor from target: `ActorUserId`/`ActorEmail`, `TargetType`/`TargetId`/`TargetDisplay`. `AuditLogger` (Infrastructure) auto-fills actor from JWT claims when the handler doesn't pass it. UA parsed via `UAParser` NuGet. Direct injection in handlers (no MediatR pipeline) — synchronous write. Updates capture before/after diff into `MetadataJson` via `AuditDiffBuilder`.

## API Routes

### `/api/auth` — public + caller-authenticated self-service

| Method | Route | Auth | Handler |
|--------|-------|------|---------|
| POST | `/login` | None | `LoginCommand` |
| POST | `/login/verify-2fa` | None | `VerifyTwoFactorCommand` |
| POST | `/forgot-password` | None | `ForgotPasswordCommand` |
| POST | `/reset-password` | None | `ResetPasswordCommand` |
| POST | `/confirm-email` | None | `ConfirmEmailCommand` |
| POST | `/resend-confirmation` | None | `ResendEmailConfirmationCommand` |
| GET | `/invite/validate?token=` | None | `ValidateInviteTokenQuery` |
| POST | `/accept-invite` | None | `AcceptInviteCommand` |
| POST | `/refresh` | None | `RefreshTokenCommand` |
| POST | `/revoke` | Authorized | `RevokeTokenCommand` |
| POST | `/2fa/enable` | Authorized | `EnableTwoFactorCommand` |
| POST | `/2fa/confirm` | Authorized | `ConfirmTwoFactorCommand` |
| POST | `/2fa/email/enable` | Authorized | `EnableEmailTwoFactorCommand` |
| POST | `/2fa/email/send-code` | Authorized | `SendEmailTwoFactorCodeCommand` |
| POST | `/2fa/disable` | Authorized | `DisableTwoFactorCommand` |

### `/api/user`

| Method | Route | Auth | Handler |
|--------|-------|------|---------|
| GET | `/profile` | Authorized | `GetCurrentUserQuery` |

### `/api/superadmin` — `[Authorize(Roles="SuperAdmin")]`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/overview` | `GetOverviewQuery` |
| GET | `/overview/logins` | `GetOverviewLoginsQuery` |
| GET | `/overview/sessions` | `GetOverviewActiveSessionsQuery` |
| GET | `/users` | `GetAllUsersQuery` |
| GET | `/users/{id}` | `GetUserByIdQuery` |
| POST | `/users` | `AdminCreateUserCommand` |
| POST | `/users/invite` | `InviteUserCommand` |
| POST | `/users/{id}/resend-invite` | `ResendInviteCommand` |
| PUT | `/users/{id}` | `UpdateUserCommand` |
| DELETE | `/users/{id}` | `DeleteUserCommand` |
| GET | `/roles` | `GetAllRolesQuery` |
| POST | `/roles` | `CreateRoleCommand` |
| DELETE | `/roles/{id}` | `DeleteRoleCommand` |
| GET | `/tenants` | `GetAllTenantsQuery` |
| GET | `/tenants/{id:guid}` | `GetTenantByIdQuery` |
| POST | `/tenants` | `CreateTenantCommand` |
| PUT | `/tenants/{id:guid}` | `UpdateTenantCommand` |
| DELETE | `/tenants/{id:guid}` | `DeleteTenantCommand` |
| GET | `/tenants/{id:guid}/users` | `GetAllUsersQuery` (scoped) |
| GET | `/tenants/{id:guid}/roles` | `GetTenantRolesQuery` |
| GET | `/audit` | `GetAuditLogsQuery` (paginated, filterable) |

### `/api/tenant` — `[Authorize(Roles="TenantAdmin")]`, scoped to caller's tenant

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/overview` | `GetOverviewQuery` |
| GET | `/overview/logins` | `GetOverviewLoginsQuery` |
| GET | `/overview/sessions` | `GetOverviewActiveSessionsQuery` |
| GET | `/context` | `GetTenantByIdQuery` |
| GET | `/users` | `GetAllUsersQuery` |
| GET | `/users/{id}` | `GetUserByIdQuery` |
| POST | `/users` | `CreateTenantUserCommand` |
| POST | `/users/invite` | `InviteTenantUserCommand` |
| POST | `/users/{id}/resend-invite` | `ResendTenantInviteCommand` |
| POST | `/users/{id}/revoke-invite` | `RevokeTenantInviteCommand` |
| POST | `/users/{id}/suspend` | `SuspendTenantUserCommand` |
| PUT | `/users/{id}` | `UpdateTenantUserCommand` |
| DELETE | `/users/{id}` | `DeleteUserCommand` (tenant-scoped) |
| GET | `/roles` | `GetTenantRolesQuery` |
| POST | `/roles` | `CreateTenantRoleCommand` |
| DELETE | `/roles/{id:guid}` | `DeleteTenantRoleCommand` |

## Email System

- **Provider selection**: `Email:Provider` config — `"SendGrid"` or `"Smtp"` (switch in `InfrastructureServiceExtensions`).
- **SendGrid**: SDK-based, reads `Email:SendGridApiKey` and `Email:From`.
- **SMTP**: MailKit, reads `Email:SmtpHost / SmtpPort / SmtpUser / SmtpPassword`; dev default = `localhost:1025`.
- **Templates**: HTML files embedded in assembly at `EasyLogin.Infrastructure.EmailTemplates.{templateName}.html`; placeholder syntax `{{{key}}}`.
- **`IEmailTemplateRenderer`**: interface in Application, implemented by `EmbeddedEmailTemplateRenderer` in Infrastructure.

## SMS System

- **`ISmsSender`** interface (Application) + **`TwilioSmsSender`** implementation (Infrastructure). Currently scaffolded but not yet routed into 2FA flows.

## Adding a New Feature

1. Add interface to `Application/Interfaces/` if a new external dependency is needed.
2. Pick the right folder: `Application/Auth/` for cross-tenant / system features, `Application/Tenants/` for tenant-scoped ones.
3. Add command/query + DTO under `Commands/` or `Queries/`.
4. Add FluentValidation validator alongside.
5. Implement handler (inject interfaces, not concrete services).
6. Add Infrastructure implementation if a new interface was introduced.
7. Register in `ApplicationServiceExtensions` or `InfrastructureServiceExtensions`.
8. Add controller endpoint on the right controller (`Auth`, `User`, `SuperAdmin`, `TenantAdmin`) → dispatch via `_mediator.Send()`.

## Development Conventions

- **Database**: no backward compatibility required — drop and recreate freely; migrations can be reset anytime.
- **Change summary**: after completing a task, add a brief summary of what changed if the scope was non-trivial (new endpoints, schema changes, architectural shifts).

## Docker

- `docker-compose.yml` at repo root; `.env` file holds secrets.
- **db**: SQL Server 2022, port 1433, volume `sqldata`, health check.
- **api**: builds from `./src/EasyLoginAPI`, port 8080, depends on db health.
- **frontend**: builds from `./src/EasyLoginUI`, port 4200, depends on api.
