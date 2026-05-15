# EasyLogin Frontend

Angular 19 SPA — multi-tenant auth template with JWT, role-based access, 2FA, Material 3, dark mode.

## Stack

- **Angular 19.2.x** — standalone components throughout, `@if` / `@for` control flow (no NgModules).
- **Angular Material 19.2.x** — Material 3, azure-blue palette custom theme.
- **jwt-decode** — parse JWT claims client-side.
- **@angular/animations** — required separately (`@angular/animations@19.2.x`).

## Project Structure

```
src/app/
├── core/
│   ├── guards/         → auth.guard, no-auth.guard, role.guard (all functional)
│   ├── interceptors/   → jwt.interceptor.ts
│   ├── models/         → auth.model.ts, user.model.ts
│   └── services/       → auth, api, admin, tenant-admin, theme
├── features/
│   ├── auth/           → login, verify-two-factor, forgot-password, reset-password,
│   │                     confirm-email, accept-invite, register, auth-layout
│   ├── dashboard/      → dashboard + dashboard-detail
│   ├── user/profile/   → current user profile view
│   ├── admin/          → user-list, user-dialog, role-list, role-dialog (shared by SuperAdmin)
│   ├── superadmin/     → tenant-list, tenant-dialog, invite-user-dialog
│   └── tenant/         → user-list, role-list, role-dialog, invite-user-dialog (TenantAdmin scope)
└── shared/
    ├── components/     → navbar, confirm-dialog, page-not-found, unauthorized
    └── directives/     → has-role.directive.ts (`*appHasRole`)
```

## Key Decisions

- **Token storage**: access + refresh tokens stored in **localStorage** (not in-memory).
- **`auth.service.ts`**: `BehaviorSubject<UserProfile>` drives auth state; `isAuthenticated()` and `hasRole()` helpers. `getAccessToken()` exposes the raw token (e.g. for future SignalR-style auth).
- **JWT claims mapping**:
  - `sub` → user id
  - `email` → email
  - `firstName` / `lastName` → custom claims
  - `tenantId` → tenant id (when caller is tenant-bound)
  - `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` → roles (string or string[])
- **Dark mode**: `ThemeService` — Angular signal + effect, toggles `.dark-mode` class on `<body>`, persisted in `localStorage`.
- **All routes lazy-loaded** via `loadComponent`.
- **Role-scoped API surfaces**: `admin.service.ts` calls `/api/superadmin/...`; `tenant-admin.service.ts` calls `/api/tenant/...`. Components import the one their feature belongs to.

## Routes & Guards

| Path | Guard | Roles |
|------|-------|-------|
| `/login`, `/login/verify-2fa`, `/forgot-password` | `noAuthGuard` | — |
| `/reset-password`, `/confirm-email`, `/accept-invite` | none | — |
| `/dashboard`, `/dashboard/:metric`, `/profile` | `authGuard` | — |
| `/superadmin/tenants`, `/superadmin/users`, `/superadmin/roles` | `authGuard` + `roleGuard` | `SuperAdmin` |
| `/tenant/users`, `/tenant/roles` | `authGuard` + `roleGuard` | `TenantAdmin` |
| `/unauthorized` | — | — |
| `**` | — | 404 |

## Login + 2FA Flow

1. `POST /api/auth/login` → either tokens (no 2FA) or `{ twoFactorToken, methods }`.
2. If 2FA required → app routes to `/login/verify-2fa`, user submits code.
3. `POST /api/auth/login/verify-2fa` → tokens, normal auth state resumes.

## JWT Interceptor Flow

1. Attaches `Authorization: Bearer <token>` to every request.
2. On `401` (not on refresh / login endpoints):
   - If refresh already in-flight → queue behind `refreshDone$` Subject, retry on complete.
   - Else → call `refreshToken()`, retry original request on success.
   - On refresh failure → logout.

## Environment Config

- **Dev** (`environment.ts`): `apiUrl: 'http://localhost:8080/api'`.
- **Prod** (`environment.prod.ts`): `apiUrl: '/api'` (Nginx proxies to backend).
- **Dev proxy** (`proxy.conf.kestrel.json`): `ng serve` → `http://localhost:8080`.
- **Docker dev proxy** (`proxy.conf.docker.json`): alternate config for docker network.

## Docker

- **Dockerfile**: multi-stage — `node:20-alpine` build → `nginx:alpine` serve.
- **nginx.conf**: proxies `/api/` → `http://api:8080/api/`, fallback to `index.html` for SPA routing.
- Served on port `4200:80` in docker-compose.

## Adding a New Feature

1. Add a service method in `core/services/api.service.ts`, or extend the role-appropriate service (`admin.service.ts` for SuperAdmin, `tenant-admin.service.ts` for TenantAdmin).
2. Create a standalone component under `features/<area>/`.
3. Register the route in `app.routes.ts` with the right guards (`authGuard` + `roleGuard` + `data: { roles: [...] }` when restricted).
4. Add a nav link in `shared/components/navbar/` if needed (use `*appHasRole` to gate by role).
5. Use `*appHasRole` directive for inline role-conditional template elements.

## Development Conventions

- **Change summary**: after completing a task, add a brief summary of what changed if the scope was non-trivial (new components, route changes, service updates).
