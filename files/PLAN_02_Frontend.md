# Implementation plan — Frontend (Angular 18, Angular Material, Docker)

> **For the AI reading this:** Follow each phase in order. Do not skip ahead.
> Ask the developer a question any time you are unsure about a naming convention,
> a route path, a UI decision, or anything not explicitly stated here.
> Never guess at environment-specific values — ask first.
> The backend must be running and all checklist items in the backend plan must be
> verified before you begin Phase 4 of this plan.

---

## Context & decisions already made

| Topic | Decision |
|---|---|
| Framework | Angular 18 (standalone components throughout, use `@if` / `@for` control flow — not `*ngIf` / `*ngFor`) |
| UI library | Angular Material + Angular CDK |
| State management | `AuthService` using `BehaviorSubject` / Angular Signals — no NgRx |
| HTTP | Angular `HttpClient` with a functional `JwtInterceptor` |
| Role-based routing | Functional route guards: `authGuard`, `roleGuard` |
| Role-based UI elements | Custom structural directive `*appHasRole` |
| Token storage | Access token in memory (service property) — never in localStorage or sessionStorage |
| Refresh token transport | **To be confirmed with the developer** — either returned in JSON body (stored in memory) or set as `HttpOnly` cookie by the API (browser sends automatically). See question in Phase 2.2. |
| Styling | Angular Material theming (Material 3) + minimal custom SCSS |
| Containerisation | Docker + Nginx (serves the built Angular app, proxies API calls) |
| Tests | None in this plan |
| Backend API base URL (dev) | `http://localhost:8080/api` |

---

## Project structure

```
my-app/
  src/
    app/
      core/
        guards/
          auth.guard.ts
          role.guard.ts
        interceptors/
          jwt.interceptor.ts
        services/
          auth.service.ts
          api.service.ts
        models/
          user.model.ts
          auth.model.ts
      features/
        auth/
          login/
          register/
          forgot-password/
          reset-password/
        user/
          profile/
        admin/
          user-list/
      shared/
        directives/
          has-role.directive.ts
        components/
          navbar/
          unauthorized/
          page-not-found/
      app.routes.ts
      app.config.ts
      app.component.ts
      app.component.scss
    environments/
      environment.ts
      environment.prod.ts
    styles.scss
  Dockerfile
  nginx.conf
```

---

## Phase 1 — Project setup and theming

**Goal:** Create the project, install dependencies, configure the theme and the environment.

Theme setup is done in Phase 1 — not later — because every component built from
Phase 2 onward depends on Material styles being available.

### Tasks

1. Generate the Angular project with routing and SCSS:
   `ng new my-app --routing --style=scss --standalone`.

2. Install Angular Material: `ng add @angular/material`.
   Choose a pre-built theme or configure a custom one.

   > **Ask the developer:** Which Angular Material pre-built theme should be used?
   > Options: Indigo/Pink, Deep Purple/Amber, Pink/Blue Grey, Purple/Green —
   > or a custom Material 3 theme with specific brand colours.

3. Configure the Material 3 theme in `styles.scss` using `@use '@angular/material' as mat`
   and `mat.define-theme()`. Set a primary palette, a tertiary palette, and typography.
   Apply the theme to the `:root` selector so all Material components pick it up.

   > **Ask the developer:** Should the app support dark mode toggling, or light mode only?
   > If dark mode: add a toggle in the navbar and use `mat.define-theme()` with both
   > light and dark color schemes, switching via a CSS class on `<body>`.

4. Set up global styles in `styles.scss`:
   - Remove default browser margin/padding on `body` and `html`.
   - Set `box-sizing: border-box` globally.
   - Define a reusable `.auth-container` class for centred auth page layout:
     max-width 420px, centred horizontally, vertically centred with flexbox on the
     parent, with a `mat-card` inside.
   - Define SCSS variables for consistent spacing (`$spacing-sm: 8px`, `$spacing-md: 16px`,
     `$spacing-lg: 24px`).

5. Install `jwt-decode` (`npm install jwt-decode`). This is the only extra runtime dependency.

6. Set up `environment.ts` and `environment.prod.ts`:
   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'http://localhost:8080/api'
   };
   ```
   In `environment.prod.ts` set `production: true` and `apiUrl: '/api'` — in production
   the Nginx reverse proxy forwards `/api` requests to the backend container, so the
   frontend uses a relative path.

   > **Ask the developer:** What is the production API domain? If the API and frontend
   > are served from the same domain via Nginx proxy, `/api` is correct. If they are on
   > different domains, provide the full URL.

7. Configure `angular.json` to use `fileReplacements` so `environment.prod.ts` is swapped
   in on `ng build --configuration production`.

8. In `app.config.ts`, provide: `provideRouter(appRoutes)`,
   `provideHttpClient(withInterceptors([jwtInterceptor]))`, `provideAnimationsAsync()`.

---

## Phase 2 — Core: models, services, interceptor, guards, directive

**Goal:** Build all the plumbing before touching any pages.

### 2.1 — Models

`auth.model.ts`:
```typescript
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}
export interface LoginRequest { email: string; password: string; }
export interface RegisterRequest {
  firstName: string; lastName: string;
  email: string; password: string; confirmPassword: string;
}
export interface ForgotPasswordRequest { email: string; }
export interface ResetPasswordRequest {
  email: string; token: string;
  newPassword: string; confirmPassword: string;
}
export interface RefreshTokenRequest { refreshToken: string; }
```

`user.model.ts`:
```typescript
export interface UserProfile {
  id: string; email: string;
  firstName: string; lastName: string;
  roles: string[];
}
export interface UserListItem {
  id: string; email: string;
  firstName: string; lastName: string;
  roles: string[]; isActive: boolean;
}
export interface PaginatedList<T> {
  items: T[];
  pageNumber: number; totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean; hasNextPage: boolean;
}
```

### 2.2 — AuthService

`core/services/auth.service.ts` — singleton, provided in root.

> **Critical question — ask the developer before implementing this service:**
> Does the backend return the refresh token in the JSON body or set it as an
> `HttpOnly` cookie?
>
> **If JSON body (default per backend plan):** store the refresh token in a private
> in-memory property alongside the access token. Send it in the `POST /auth/refresh`
> request body. No `withCredentials` needed.
>
> **If HttpOnly cookie:** do not store the refresh token at all. The browser sends
> the cookie automatically. Set `withCredentials: true` on the `POST /auth/refresh`
> request. Remove `refreshToken` from the `AuthResponse` model.
>
> The rest of this section is written assuming **JSON body** (the simpler case).
> Adjust if the developer chooses cookies.

Responsibilities:
- Hold the current access token and refresh token as private `string | null` properties.
  Never persist either to localStorage or sessionStorage.
- Hold a `BehaviorSubject<UserProfile | null>` called `currentUser$`.
- On token storage: decode the JWT using `jwt-decode`, extract `sub`, `email`,
  `firstName`, `lastName`, and `role` claims, build a `UserProfile`, and emit it on
  `currentUser$`.
- Expose `isAuthenticated(): boolean` — returns true if the access token is not null.
- Expose `hasRole(role: string): boolean` — checks the current user's roles array.
- `login(req: LoginRequest): Observable<void>` — POST to `/auth/login`, store both tokens,
  decode and emit user.
- `register(req: RegisterRequest): Observable<void>` — POST to `/auth/register`,
  same post-processing as login.
- `logout(): void` — POST to `/auth/revoke` (fire-and-forget — catch and ignore errors),
  clear both in-memory tokens, emit `null` on `currentUser$`, navigate to `/login`.
- `refreshToken(): Observable<AuthResponse>` — POST to `/auth/refresh` with the stored
  refresh token, store the new tokens on success.
- `getAccessToken(): string | null` — used by the interceptor.
- `forgotPassword(req: ForgotPasswordRequest): Observable<void>` — POST, no token handling.
- `resetPassword(req: ResetPasswordRequest): Observable<void>` — POST, no token handling.

### 2.3 — ApiService

`core/services/api.service.ts` — thin typed wrapper around `HttpClient`.
Provides `get<T>`, `post<T>`, `put<T>`, `delete<T>` methods that prepend
`environment.apiUrl`. All feature-specific API calls go through this service.
Auth-specific calls go through `AuthService`, which uses `ApiService` internally.

### 2.4 — JwtInterceptor

`core/interceptors/jwt.interceptor.ts` — functional interceptor
(`HttpInterceptorFn`).

Logic:
1. If a token exists in `AuthService`, clone the request and attach
   `Authorization: Bearer <token>`.
2. Pass the request through.
3. If the response is a `401` error:
   - If the failing URL contains `/auth/refresh` or `/auth/login`: do not retry —
     propagate the error. This prevents infinite loops.
   - Otherwise: attempt a silent refresh.

**Silent refresh with queue (critical for correctness):**

Multiple HTTP requests can fail with 401 simultaneously (e.g. a dashboard loading
several resources). Without coordination, each would trigger its own refresh call,
causing race conditions and token rotation failures.

Implementation:
- Maintain a private `isRefreshing = false` flag and a `Subject<boolean>` called
  `refreshDone$`.
- On first 401: set `isRefreshing = true`, call `authService.refreshToken()`.
  On success: emit `true` on `refreshDone$`, set `isRefreshing = false`, retry the
  original request with the new token.
  On failure: emit `false`, set `isRefreshing = false`, call `authService.logout()`.
- On subsequent 401s while `isRefreshing === true`: do not call refresh again. Instead,
  return `refreshDone$.pipe(filter(v => v), take(1), switchMap(() => retryRequest))`.

### 2.5 — AuthGuard

`core/guards/auth.guard.ts` — functional guard (`CanActivateFn`).

If `authService.isAuthenticated()` is true: allow navigation.
Otherwise: redirect to `/login` with `{ queryParams: { returnUrl: state.url } }`.

### 2.6 — RoleGuard

`core/guards/role.guard.ts` — functional guard (`CanActivateFn`).

Precondition: `RoleGuard` must always be used together with `AuthGuard` (list `authGuard`
first in the route's `canActivate` array). By the time `RoleGuard` runs, the user is
guaranteed to be authenticated.

Read the required roles from `route.data['roles']` (string array).
If `authService.hasRole()` returns true for at least one required role: allow.
Otherwise: redirect to `/unauthorized`.

### 2.7 — HasRole directive

`shared/directives/has-role.directive.ts` — structural directive.

Usage in templates: `*appHasRole="'Admin'"` or `*appHasRole="['Admin', 'Manager']"`.

Behaviour: subscribe to `authService.currentUser$`. If the current user has at least
one of the required roles, render the host element using `ViewContainerRef.createEmbeddedView`.
Otherwise, clear the view using `ViewContainerRef.clear()`.

Re-evaluate whenever `currentUser$` emits. This ensures elements disappear immediately
on logout and appear immediately after login. Unsubscribe in `ngOnDestroy` (or use
`takeUntilDestroyed`).

---

## Phase 3 — Routing

**Goal:** Define all routes with the correct guards applied.

### app.routes.ts

```
/                       → redirect to /dashboard
/login                  → LoginComponent          (no guard — redirect away if already logged in)
/register               → RegisterComponent       (no guard — redirect away if already logged in)
/forgot-password        → ForgotPasswordComponent (no guard)
/reset-password         → ResetPasswordComponent  (no guard — token comes in query param)
/dashboard              → DashboardComponent      (canActivate: [authGuard])
/profile                → ProfileComponent        (canActivate: [authGuard])
/admin/users            → UserListComponent       (canActivate: [authGuard, roleGuard], data: { roles: ['Admin'] })
/unauthorized           → UnauthorizedComponent   (no guard)
**                      → PageNotFoundComponent   (no guard)
```

**Login/Register redirect:** If the user is already authenticated and navigates to
`/login` or `/register`, redirect them to `/dashboard` immediately. Implement this
as a separate functional guard (e.g. `noAuthGuard`) that checks
`!authService.isAuthenticated()`.

The `/reset-password` route must accept `token` and `email` as query parameters
(`?token=...&email=...`) — these are included in the link sent by the API's
forgot-password email.

> **Ask the developer:** Should the dashboard be a real page (e.g. welcome message +
> role-based cards) or a minimal placeholder for future content?

### Lazy loading

Each feature group (`auth`, `user`, `admin`) should be lazy-loaded using
`loadComponent` for individual components or `loadChildren` for route groups.
This keeps the initial bundle small — only the login page JS is loaded on first visit.

---

## Phase 4 — Auth pages

**Goal:** Build login, register, forgot password, and reset password pages.
All forms use Angular Material components and Angular Reactive Forms.

### General form patterns (apply to all auth pages)

- Use `FormGroup` with typed `FormControl` (Angular 14+ typed forms).
- Show validation errors inline below the field using `<mat-error>` — these appear
  automatically when the control is touched and invalid.
- Disable the submit button while the form is invalid or a request is in flight.
- Show a `MatProgressSpinner` (or `mat-spinner`) overlay or a loading state on the
  submit button during the API call.
- On API error: show a `MatSnackBar` with a user-friendly message. Do not display
  raw error objects.
- Wrap each page in the `.auth-container` class defined in `styles.scss` (Phase 1).

### 4.1 — Login page

Fields: Email (`mat-form-field`), Password (`mat-form-field` with show/hide toggle
via a `mat-icon-button` suffix that toggles the input type between `password` and `text`).

Buttons: "Log in" (primary), "Create an account" (link to `/register`),
"Forgot your password?" (link to `/forgot-password`).

On success: navigate to the `returnUrl` query param if present, otherwise `/dashboard`.
On error: display the error in a snackbar ("Invalid email or password" — do not
distinguish which one is wrong).

### 4.2 — Register page

Fields: First Name, Last Name, Email, Password, Confirm Password.

Validators:
- First Name, Last Name: required.
- Email: required, email format.
- Password: required, minimum length (match the backend's rules — ask if unknown).
- Confirm Password: required, must match Password (use a cross-field validator on the
  `FormGroup`).

On success: user is automatically logged in, navigate to `/dashboard`.
On `409 Conflict` error (duplicate email): show snackbar "An account with this email
already exists".

### 4.3 — Forgot password page

Field: Email.

On success: always show the same message — "If an account with that email exists,
a reset link has been sent." This is a security best practice: do not reveal whether
the email exists in the database.

On API error (network failure etc.): show a generic snackbar.

### 4.4 — Reset password page

On component init: read `token` and `email` from `ActivatedRoute.queryParams`.
If either is missing or empty, redirect to `/forgot-password` immediately.

Fields: New Password, Confirm Password.

Validators: same rules as the register page (minimum length, must match).

On success: show a snackbar "Password has been reset", navigate to `/login`.
On error (invalid or expired token): show a snackbar "This reset link has expired.
Please request a new one." with a link/button to `/forgot-password`.

---

## Phase 5 — Feature pages

### 5.1 — Dashboard

A simple authenticated landing page. Display:
- Welcome message using the user's first name from `currentUser$`:
  "Welcome back, {firstName}".
- A `mat-card` showing the user's assigned roles as `mat-chip` elements.
- Quick-action links as `mat-card` elements, visible based on role:
  - All users see: "My Profile" card (links to `/profile`).
  - Admin users additionally see: "User Management" card (links to `/admin/users`),
    rendered via `*appHasRole="'Admin'"`.

### 5.2 — Profile page

Display the current user's details in a read-only `mat-card`:
First name, last name, email, roles (as `mat-chip` elements).

Data source: `authService.currentUser$`. No additional API call needed because
the `firstName`, `lastName`, `email`, and `role` claims are included in the JWT
(see backend plan Phase 3.4).

### 5.3 — Admin user list page

Visible only to Admin role (protected by both `roleGuard` on the route and
`*appHasRole` on the nav link).

Display a `mat-table` with columns: Name (first + last), Email, Roles, Status
(active/inactive — show as a coloured `mat-chip`: green for active, grey for inactive).

Implement server-side pagination using `mat-paginator`:
- On paginator change: call `GET /api/admin/users?pageNumber=X&pageSize=Y`.
- Display `totalCount` from the `PaginatedList` response.
- Default page size: 20. Page size options: `[10, 20, 50]`.

Show a `mat-progress-bar` (indeterminate mode) above the table while loading.
Show an empty state message if no users are returned.

### 5.4 — Unauthorized page

Simple page centred on screen with:
- A `mat-icon` (e.g. `lock`) and a heading: "Access denied".
- Body text: "You do not have permission to view this page."
- A `mat-button` linking to `/dashboard`: "Go to Dashboard".

### 5.5 — Page not found page

Simple page centred on screen with:
- Heading: "Page not found".
- Body: "The page you are looking for does not exist."
- A `mat-button` linking to `/dashboard` (if authenticated) or `/login` (if not).

---

## Phase 6 — Navbar

### NavbarComponent

A `mat-toolbar` with `color="primary"` rendered at the top of every authenticated page.

Implementation: in `app.component.ts`, use Angular 18 control flow:
```html
@if (authService.isAuthenticated()) {
  <app-navbar />
}
<router-outlet />
```

Do **not** use `*ngIf` — the project uses standalone components with the new
control flow syntax throughout.

Navbar content (left to right):
- App name (plain text or small logo) — links to `/dashboard`.
- `mat-button` links: "Dashboard" (`/dashboard`), "Profile" (`/profile`).
- Admin link visible only to Admin role:
  `<a mat-button routerLink="/admin/users" *appHasRole="'Admin'">Users</a>`
  (the `*appHasRole` directive is the one exception where structural directive
  syntax is used inside the new control flow).
- Spacer (`<span class="spacer"></span>` with `flex: 1 1 auto`).
- Current user's email as plain text.
- "Logout" button (`mat-icon-button` with `logout` icon) that calls
  `authService.logout()`.

Highlight the active route using `routerLinkActive="active-link"` on each nav link.

---

## Phase 7 — Docker

### 7.1 — Dockerfile

Multi-stage build:

1. **build stage** (`node:20-alpine`): copy `package.json` and `package-lock.json` first,
   run `npm ci` (cached unless dependencies change). Copy remaining source,
   run `npm run build -- --configuration production`.
2. **runtime stage** (`nginx:alpine`): copy the built output from
   `dist/my-app/browser/` (Angular 17+ output path) into `/usr/share/nginx/html`.
   Copy the custom `nginx.conf` to `/etc/nginx/conf.d/default.conf`.

### 7.2 — nginx.conf

Two responsibilities: serve the Angular SPA and reverse-proxy API calls to the backend.

```nginx
server {
  listen 80;
  root /usr/share/nginx/html;
  index index.html;

  # Reverse proxy API calls to the backend container
  location /api/ {
    proxy_pass http://api:8080/api/;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
  }

  # Serve Angular SPA — fallback to index.html for client-side routing
  location / {
    try_files $uri $uri/ /index.html;
  }
}
```

This means in production, the frontend and API are accessed through the same domain
(via the Nginx container), and `environment.prod.ts` uses `apiUrl: '/api'` — no
CORS issues in production.

### 7.3 — docker-compose addition

Add the `frontend` service to the root `docker-compose.yml`:

```yaml
frontend:
  build: ./my-app
  ports:
    - "4200:80"
  depends_on:
    - api
```

In `docker-compose.override.yml` for development: instead of building the Nginx image,
override the command to run `ng serve --host 0.0.0.0 --port 4200 --proxy-config proxy.conf.json`
from a Node image, with the source directory mounted as a volume for hot-reload.

### 7.4 — Angular proxy config (dev only)

Create `proxy.conf.json` for Angular dev server — proxies `/api` requests to the backend
container so there are no CORS issues during development either:

```json
{
  "/api": {
    "target": "http://localhost:8080",
    "secure": false,
    "changeOrigin": true
  }
}
```

Register it in `angular.json` under `serve.options.proxyConfig` so it applies
automatically when running `ng serve`.

---

## Phase 8 — Final checklist

Work through each item manually. Do not mark complete until verified.

### Build & deploy
- [ ] `ng build --configuration production` completes with zero errors and zero warnings
- [ ] `docker-compose up` starts frontend, backend, and database cleanly
- [ ] Frontend is accessible at `http://localhost:4200`

### Auth flow
- [ ] Unauthenticated visit to `/dashboard` redirects to `/login`
- [ ] Authenticated visit to `/login` redirects to `/dashboard`
- [ ] Login with valid credentials redirects to `/dashboard`
- [ ] Login with invalid credentials shows snackbar error, stays on `/login`
- [ ] Login preserves and redirects to `returnUrl` query param after success
- [ ] Register form creates a new user and redirects to `/dashboard`
- [ ] Register with duplicate email shows "account already exists" error
- [ ] Register form shows inline validation errors for empty or invalid fields
- [ ] Forgot password shows confirmation message regardless of email existence
- [ ] Reset password with valid token changes password and redirects to `/login`
- [ ] Reset password with expired token shows expiry message
- [ ] Logout clears the session and redirects to `/login`
- [ ] After logout, manually navigating to `/dashboard` redirects to `/login`

### Token management
- [ ] Access token is not visible in localStorage or sessionStorage (check dev tools)
- [ ] After token expires, the next API call triggers a silent refresh transparently
- [ ] Multiple simultaneous 401s trigger only one refresh call (verify in network tab)
- [ ] If refresh fails, user is logged out and redirected to `/login`

### Role-based UI
- [ ] Admin nav link is visible when logged in as Admin
- [ ] Admin nav link is hidden when logged in as User
- [ ] `/admin/users` route is accessible to Admin, redirects to `/unauthorized` for User
- [ ] Admin user list displays correctly with working pagination
- [ ] `*appHasRole` elements appear/disappear immediately on login/logout (no page refresh needed)

### Navigation
- [ ] Hard refresh on `/dashboard` (or any route) does NOT return a 404
- [ ] `/nonexistent-path` shows the "Page not found" component
- [ ] Active route is highlighted in the navbar

---

## Questions the AI must ask the developer before starting

1. Which Angular Material theme (pre-built or custom colours)?
2. Production API URL — same domain via Nginx proxy (`/api`) or different domain?
3. Is the refresh token returned in the JSON body or set as an `HttpOnly` cookie?
4. Should the dashboard be a real page or a minimal placeholder?
5. Light mode only, or dark mode toggle support?
6. What is the minimum password length (must match backend validation rules)?
