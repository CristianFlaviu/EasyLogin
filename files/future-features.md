# EasyLogin — Roadmap of Next Features

## Context

EasyLogin is a Clean Architecture .NET 10 + Angular auth template intended for resale (audience TBD). User and role management plus a multi-tenant scaffold (Company entity, CompanyId on user, JWT claim, SuperAdmin + CompanyAdmin controllers) are already in place. Email (SendGrid/SMTP), JWT with refresh tokens, MediatR + FluentValidation pipeline, Serilog, and Scalar docs are wired.

The owner asked which concrete features to add next and which buyer market gives most demand. They selected as important: Invitations + onboarding, 2FA, Billing, Audit log. Monetization is undecided. They want a roadmap of 3–5 features (not a deep implementation plan).

## Buyer-market recommendation

**Lead with self-hosted template; keep B2B SaaS optional.**

Reasons grounded in the current asset:
- Clean Architecture + .NET 10 + Angular + Docker + Serilog + Scalar maps directly to the indie starter-template market (Gumroad, CodeCanyon, personal landing page). Typical price band $99–$299.
- The multi-tenant scaffold (CompanyId on every entity, JWT claim, scoped admin controller) is the differentiator vs free Identity samples. That is the moat.
- B2B SaaS requires hosting, marketing, support, SLA, billing maturity — none of which exists yet. Months to years of work before first revenue.
- "Both" dilutes scope. Ship template first; keep Billing module opt-in so a SaaS pivot stays open.

Marketability gaps to close before listing: README with screenshots, one-command `docker compose up` with a seeded demo company, Postman / Bruno collection, LICENSE (single-site vs extended).

## Recommended sequence

### 1. Invitations + onboarding — effort M

Goal: CompanyAdmin sends an emailed invite link; recipient sets password and joins the company with a role.

Why first: CompanyAdmin currently has no humane way to add real users. Unblocks every demo. Token + email plumbing already exists in `ForgotPasswordCommand` / `ResetPasswordCommand` and can be mirrored.

Scope:
- Domain: `Invitation` (Id, CompanyId, Email, RoleId, TokenHash, ExpiresAt, AcceptedAt, InvitedByUserId, Status).
- Commands: `CreateInvitationCommand`, `AcceptInvitationCommand`, `ResendInvitationCommand`, `RevokeInvitationCommand`.
- Query: `ListInvitationsQuery` (scoped by CompanyId).
- Endpoints: `POST/GET/DELETE /api/company/invitations`, `POST /api/auth/accept-invitation`.
- Email template: `Invitation.html` (mirror existing embedded-resource pattern).
- Repository: `IInvitationRepository` + EF configuration; new migration.

Files:
- `EasyLogin.Domain/Entities/Invitation.cs`
- `EasyLogin.Application/Invitations/{Commands,Queries,Dtos,Validators}/*`
- `EasyLogin.Application/Interfaces/IInvitationRepository.cs`
- `EasyLogin.Infrastructure/Persistence/InvitationRepository.cs`, `Configurations/InvitationConfiguration.cs`, `AppDbContext.cs` (DbSet)
- `EasyLogin.Infrastructure/EmailTemplates/Invitation.html`
- `EasyLoginAPI/Controllers/CompanyAdminController.cs` (extend), `AuthController.cs` (accept endpoint)

FE: Invitations table in CompanyAdmin, "Invite user" modal, public `/accept-invite?token=` page (reuse the reset-password form).

### 2. Audit log — effort S

Goal: Append-only record of who did what and when. Compliance signal for buyers.

Why second: Cheapest win. Slots into the existing MediatR pipeline next to `LoggingBehavior`. No FE blocker — admin viewer can ship later.

Scope:
- Domain: `AuditLogEntry` (Id, UserId, CompanyId, Action, EntityType, EntityId, MetadataJson, IpAddress, CreatedAt).
- Marker interface `IAuditable` on mutating commands; new `AuditBehavior` writes after handler success.
- Query: `ListAuditLogQuery` (paged, filter by user/action/date range).
- Endpoints: `GET /api/company/audit-log`, `GET /api/superadmin/audit-log`.

Files:
- `EasyLogin.Domain/Entities/AuditLogEntry.cs`
- `EasyLogin.Application/Common/IAuditable.cs`
- `EasyLogin.Application/Behaviours/AuditBehavior.cs` (sibling to existing `LoggingBehavior`)
- `EasyLogin.Application/AuditLog/Queries/ListAuditLogQuery.cs`
- `EasyLogin.Application/Interfaces/IAuditLogRepository.cs`
- `EasyLogin.Infrastructure/Persistence/AuditLogRepository.cs` + configuration
- `EasyLogin.Application/ApplicationServiceExtensions.cs` (register behavior)
- Mark mutating commands (`AdminCreateUserCommand`, `UpdateUserCommand`, `DeleteUserCommand`, `CreateRoleCommand`, `DeleteRoleCommand`, etc.) with `IAuditable`.

Reuse: `LoggingBehavior` pattern at `EasyLogin.Application/Behaviours/LoggingBehavior.cs`. `CurrentUserService` already exposes UserId + CompanyId from claims.

FE: Read-only paginated audit log table in admin views (filter by user/action/date).

### 3. 2FA (TOTP + recovery codes) — effort M

Goal: Optional TOTP via authenticator app, ten one-shot recovery codes, account lockout on failed attempts.

Why third: Security checkbox every buyer expects. ASP.NET Identity ships TOTP primitives and a `TwoFactorEnabled` flag, so infra cost is low. Wait until invitations land so the onboarding flow can prompt enrolment.

Scope:
- Reuse `UserManager.GenerateTwoFactorTokenAsync` / `VerifyTwoFactorTokenAsync` and the `TwoFactorEnabled` flag already on `IdentityUser`.
- Domain: `RecoveryCode` (UserId, CodeHash, UsedAt) — separate table preferred over JSON column for query simplicity.
- Commands: `EnableTwoFactorCommand` (returns otpauth URI + shared secret), `ConfirmTwoFactorCommand`, `DisableTwoFactorCommand`, `VerifyTwoFactorCommand` (login step 2), `RegenerateRecoveryCodesCommand`, `UseRecoveryCodeCommand`.
- Modify `LoginCommand`: when `TwoFactorEnabled`, return `RequiresTwoFactor=true` plus a short-lived 2FA challenge token instead of access/refresh tokens.
- Lockout: configure `MaxFailedAccessAttempts` and `DefaultLockoutTimeSpan` in `InfrastructureServiceExtensions`.
- Email template: `TwoFactorEnabled.html` (notification on enable/disable).
- Endpoints: `/api/auth/2fa/{enable,confirm,disable,recovery-codes}`, `POST /api/auth/login/verify-2fa`.

Files:
- `EasyLogin.Domain/Entities/RecoveryCode.cs`
- `EasyLogin.Application/Auth/Commands/{Enable,Confirm,Disable,Verify}TwoFactorCommand.cs`, `RegenerateRecoveryCodesCommand.cs`, `UseRecoveryCodeCommand.cs`
- `EasyLogin.Application/Auth/Commands/LoginCommand.cs` (modify result type)
- `EasyLogin.Application/Auth/Dtos/LoginResultDto.cs` (add `RequiresTwoFactor`, `TwoFactorToken`)
- `EasyLogin.Infrastructure/Services/TokenService.cs` (issue short-lived 2FA challenge token)
- `EasyLogin.Infrastructure/InfrastructureServiceExtensions.cs` (lockout config)
- `EasyLoginAPI/Controllers/AuthController.cs`
- New migration for `RecoveryCode`.

FE: Settings page with "Enable 2FA" QR (qrcode.js), code-confirm step, recovery code download. Login flow detects `requiresTwoFactor` and routes to a code-entry page.

### 4. Billing scaffold (Stripe, opt-in module) — effort L

Goal: Plug-and-play Stripe Checkout + webhooks + plan-gated middleware, feature-flagged off by default so self-hosted buyers can ignore it.

Why fourth: Only valuable if monetization is decided. Building it opt-in keeps both buyer paths open without forcing dead code on template-only buyers.

Scope:
- Domain: `Subscription` (CompanyId, StripeCustomerId, StripeSubscriptionId, PlanId, Status, CurrentPeriodEnd), `Plan` (Code, Name, StripePriceId, LimitsJson), `BillingEvent` (raw webhook envelope for replay/audit).
- Commands/Queries: `StartCheckoutCommand` (returns Stripe Checkout URL), `OpenBillingPortalCommand`, `GetCurrentSubscriptionQuery`, `HandleStripeWebhookCommand`.
- `IStripeService` wrapping the `Stripe.net` SDK.
- Webhook endpoint `POST /api/billing/stripe/webhook` (anonymous, signature-verified).
- Plan-gating action filter `[RequiresPlan("pro")]` reading current company's subscription via `CurrentUserService`.
- Feature flag `Billing:Enabled` in `appsettings.json`; `InfrastructureServiceExtensions` skips registration when false.
- Email templates: `SubscriptionActive.html`, `PaymentFailed.html`.

Files:
- `EasyLogin.Domain/Entities/{Subscription,Plan,BillingEvent}.cs`
- `EasyLogin.Application/Billing/**`
- `EasyLogin.Application/Interfaces/IStripeService.cs`
- `EasyLogin.Application/Common/RequiresPlanAttribute.cs`
- `EasyLogin.Infrastructure/Services/StripeService.cs`
- `EasyLogin.Infrastructure/Persistence/Configurations/{Subscription,Plan,BillingEvent}Configuration.cs`
- `EasyLoginAPI/Controllers/BillingController.cs`
- New migration.

FE: Billing page (current plan, upgrade → Checkout redirect, manage → Portal redirect), plan-locked UI badges.

### 5. Polish bundle — effort S each

- **Rate limiting**: native .NET 10 `RateLimiter` middleware (no third-party package); per-IP on `/auth/*`, per-user elsewhere; touch `Program.cs`.
- **Session list + revoke**: surface refresh-token records as sessions; `GET /api/users/me/sessions`, `DELETE /api/users/me/sessions/{id}`; FE table with "sign out everywhere".
- **Account settings page**: pure FE — change email, change password, 2FA controls, sessions, danger zone (delete account).

## Verification

For every feature:
1. `dotnet ef migrations add <Name> -p EasyLogin.Infrastructure -s EasyLoginAPI` — confirm migration generated cleanly, no shadow properties.
2. `dotnet build` — zero warnings.
3. `docker compose up --build` — API + DB + frontend healthy.
4. Hit endpoints via Scalar (`/scalar/v1`) or the Bruno/Postman collection; assert happy path + auth/role guards.
5. Tail `logs/easylogin-*.log` to confirm `LoggingBehavior` (and, for #2, `AuditBehavior`) wrote entries.
6. Feature-specific smoke tests:
   - **Invitations**: create invite → email arrives → accept link sets password → user listed in company.
   - **Audit log**: mutate a user → entry appears in `GET /api/company/audit-log`.
   - **2FA**: enable → scan QR with authenticator → confirm → next login prompts code → recovery code works once.
   - **Billing**: trigger Stripe CLI `stripe trigger checkout.session.completed` against `/api/billing/stripe/webhook`; subscription row created; `[RequiresPlan]` action returns 200 vs 402 correctly.
