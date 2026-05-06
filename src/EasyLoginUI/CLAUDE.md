# EasyLogin Frontend

Angular 19 SPA — auth template with JWT, role-based access, Material 3, dark mode.

## Stack

- **Angular 19.2.x** — standalone components throughout, `@if`/`@for` control flow (no NgModules)
- **Angular Material 19** — Material 3, azure-blue palette custom theme
- **jwt-decode** — parse JWT claims client-side
- **@angular/animations** — required separately (`@angular/animations@19.2.21`)

## Project Structure

```
src/app/
├── core/
│   ├── guards/         → auth.guard, no-auth.guard, role.guard (all functional)
│   ├── interceptors/   → jwt.interceptor.ts
│   ├── models/         → auth.model.ts, user.model.ts
│   └── services/       → auth.service, api.service, admin.service, theme.service
├── features/
│   ├── auth/           → login, register, forgot-password, reset-password + auth-layout
│   ├── admin/          → user-list, user-dialog, role-list, role-dialog
│   ├── dashboard/      → placeholder ("Coming soon")
│   └── user/profile/   → current user profile view
└── shared/
    ├── components/     → navbar, confirm-dialog, page-not-found, unauthorized
    └── directives/     → has-role.directive.ts (*appHasRole)
```

## Key Decisions

- **Token storage**: access + refresh tokens stored in **localStorage** (not in-memory)
- **auth.service.ts**: `BehaviorSubject<UserProfile>` drives auth state; `isAuthenticated()` and `hasRole()` helpers
- **JWT claims mapping**:
  - `sub` → user id
  - `email` → email
  - `firstName` / `lastName` → custom claims
  - `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` → roles (string or string[])
- **Dark mode**: `ThemeService` — signal + effect, toggles `.dark-mode` class on `<body>`, persisted in `localStorage`
- **All routes lazy-loaded** via `loadComponent`

## Routes & Guards

| Path | Guard | Roles |
|------|-------|-------|
| `/login`, `/register`, `/forgot-password` | noAuthGuard | — |
| `/reset-password` | none | — |
| `/dashboard`, `/profile` | authGuard | — |
| `/admin/users`, `/admin/roles` | authGuard + roleGuard | Admin |
| `**` | — | 404 |

## JWT Interceptor Flow

1. Attaches `Authorization: Bearer <token>` to every request
2. On 401 (not on refresh/login endpoints):
   - If refresh already in-flight → queue behind `refreshDone$` Subject, retry on complete
   - Else → call `refreshToken()`, retry original request on success
   - On refresh failure → logout

## Environment Config

- **Dev** (`environment.ts`): `apiUrl: http://localhost:8080/api`
- **Prod** (`environment.prod.ts`): `apiUrl: /api` (Nginx proxies to backend)
- **Dev proxy** (`proxy.conf.kestrel.json`): `ng serve` → `http://localhost:8080`
- **Docker dev proxy** (`proxy.conf.docker.json`): alternate config for docker network

## Docker

- **Dockerfile**: multi-stage — `node:20-alpine` build → `nginx:alpine` serve
- **nginx.conf**: proxies `/api/` → `http://api:8080/api/`, fallback to `index.html` for SPA routing
- Served on port `4200:80` in docker-compose

## Development Conventions

- **Change summary**: after completing a task, add a brief summary of what changed if the scope was non-trivial (new components, route changes, service updates)

## Adding a New Feature

1. Add service method in `core/services/api.service.ts` or a feature-specific service
2. Create standalone component in `features/<feature>/`
3. Register route in `app.routes.ts` with appropriate guard
4. Add nav link to `shared/components/navbar/` if needed
5. Use `*appHasRole` directive for role-conditional template elements
