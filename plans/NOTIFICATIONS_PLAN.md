# Notification System Implementation Plan (Backend + Frontend)

Audience: another AI / engineer picking this up to implement end-to-end. All decisions confirmed by the user.

## Context

EasyLogin currently has no notification feature. Goal: classic bell-icon UX with unread-count badge, dropdown list, mark-as-read on click, **real-time push** via SignalR. Backend has no SignalR yet; frontend has no `@microsoft/signalr` dependency. A test controller seeds notifications since no real producers exist yet.

### Confirmed decisions
- **Transport:** SignalR
- **Storage:** SQL Server — new `Notifications` table, persisted
- **Schema:** minimal — `Id, UserId, Title, Message, Type, IsRead, CreatedAt`
- **Test endpoints:** `/self`, `/user/{userId}`, `/broadcast` — **AllowAnonymous** (test-only convenience)
- **UX:** mark-as-read on click; list inside MatMenu dropdown; mark-all-read button
- **Migration:** standard `AddNotifications` EF migration (auto-applied via existing `DataSeeder.SeedAsync`)

---

## Backend — `src/EasyLoginAPI/`

Stack reminder: .NET 10, Clean Architecture (Domain / Application / Infrastructure / API), MediatR 14 CQRS, FluentValidation, Mapster, Identity + JWT (HS256), Scalar for docs. See `src/EasyLoginAPI/CLAUDE.md` for full conventions. Use **explicit types** (no `var`) per project style rule.

### 1. Domain — new entity

**File:** `EasyLogin.Domain/Entities/Notification.cs` (new)

```csharp
public class Notification
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;   // FK → AppIdentityUser.Id
    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string Type { get; set; } = null!;     // "info" | "success" | "warning" | "error" | domain key
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

POCO, zero NuGet deps. Model on `EasyLogin.Domain/Entities/AuditLog.cs`.

### 2. Infrastructure — EF wiring + migration

- **New:** `EasyLogin.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs` — implements `IEntityTypeConfiguration<Notification>`. Required: PK on `Id`; index `(UserId, IsRead, CreatedAt DESC)`; FK `UserId → AspNetUsers.Id` with cascade delete; max-length constraints on string columns (Title 200, Message 2000, Type 64).
- **Modify:** `EasyLogin.Infrastructure/Persistence/AppDbContext.cs` — add `public DbSet<Notification> Notifications => Set<Notification>();`. `ApplyConfigurationsFromAssembly` already auto-picks the configuration class.
- **Migration:** `dotnet ef migrations add AddNotifications -p EasyLogin.Infrastructure -s EasyLoginAPI` (run from `src/EasyLoginAPI/`).
- `DataSeeder.SeedAsync` already calls `Database.MigrateAsync()` at startup — no extra wiring.

### 3. Application — interfaces + DTOs + CQRS

**Interfaces (new):**
- `EasyLogin.Application/Interfaces/INotificationService.cs`
  - `Task<Notification> CreateAsync(string userId, string title, string message, string type, CancellationToken ct = default);`
  - `Task<List<Notification>> CreateForAllUsersAsync(string title, string message, string type, CancellationToken ct = default);` — iterates `AspNetUsers`, inserts one row per user (per-user read state).
  - `Task MarkReadAsync(Guid id, string userId, CancellationToken ct = default);`
  - `Task MarkAllReadAsync(string userId, CancellationToken ct = default);`
  - `Task<List<Notification>> GetForUserAsync(string userId, bool unreadOnly, int skip, int take, CancellationToken ct = default);`
  - `Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);`
- `EasyLogin.Application/Interfaces/INotificationPusher.cs` — abstraction so Application doesn't depend on SignalR.
  - `Task PushToUserAsync(string userId, NotificationResponse n, CancellationToken ct = default);`
  - `Task PushToAllAsync(NotificationResponse n, CancellationToken ct = default);`

**DTO (new):** `EasyLogin.Application/Notifications/Dtos/NotificationResponse.cs`

```csharp
public record NotificationResponse(
    Guid Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTime CreatedAt);
```

**Commands / Queries (new) under `EasyLogin.Application/Notifications/`:**

| Type | Name | Inputs | Returns |
|------|------|--------|---------|
| Query | `GetMyNotificationsQuery` | `bool UnreadOnly`, `int Skip`, `int Take` | `List<NotificationResponse>` |
| Query | `GetUnreadCountQuery` | (none — uses caller) | `int` |
| Command | `MarkNotificationReadCommand` | `Guid Id` | `Unit` |
| Command | `MarkAllReadCommand` | (none) | `Unit` |
| Command | `SendTestNotificationCommand` | `string? TargetUserId`, `bool Broadcast`, `string Title`, `string Message`, `string Type` | `Unit` |

- Use existing `ICurrentUserService` for the caller's `UserId` in non-test handlers.
- `SendTestNotificationCommand` handler logic:
  - If `Broadcast` → `CreateForAllUsersAsync` then `PushToAllAsync`.
  - Else require `TargetUserId` → `CreateAsync` then `PushToUserAsync`.
- All handlers project entity → `NotificationResponse` via Mapster.
- Add FluentValidation validators in `EasyLogin.Application/Auth/Validators/` (or a new `Notifications/Validators/` folder following existing layout) — `Title` required ≤200, `Message` required ≤2000, `Type` required ≤64, `Take` between 1 and 100.

**Mapster mapping:** register `Notification → NotificationResponse` either in existing `IdentityMappingConfig` (`TypeAdapterConfig<Notification, NotificationResponse>.NewConfig();`) or a new `NotificationMappingConfig : IRegister` in `EasyLogin.Infrastructure/Identity/MappingProfiles/` (gets auto-discovered the same way).

### 4. Infrastructure — implementations

- **New:** `EasyLogin.Infrastructure/Services/NotificationService.cs` — implements `INotificationService` using `AppDbContext`. Read queries should use `AsNoTracking()`. Order list `CreatedAt DESC`.
- **New:** `EasyLogin.Infrastructure/Realtime/NotificationHub.cs` — `[Authorize]` SignalR hub. Empty server-callable surface (clients only receive); `Context.UserIdentifier` is populated by the custom `IUserIdProvider` below so `Clients.User(...)` works.
- **New:** `EasyLogin.Infrastructure/Realtime/JwtUserIdProvider.cs` — `IUserIdProvider` returning the `sub` claim (matches the JWT layout in `EasyLogin.Infrastructure/InfrastructureServiceExtensions.cs`).
- **New:** `EasyLogin.Infrastructure/Realtime/SignalRNotificationPusher.cs` — implements `INotificationPusher`. Injects `IHubContext<NotificationHub>`.
  - `Clients.User(userId).SendAsync("notification", dto, ct)`
  - `Clients.All.SendAsync("notification", dto, ct)`
- **Modify:** `EasyLogin.Infrastructure/InfrastructureServiceExtensions.cs`
  - `services.AddSignalR();`
  - `services.AddSingleton<IUserIdProvider, JwtUserIdProvider>();`
  - `services.AddScoped<INotificationService, NotificationService>();`
  - `services.AddScoped<INotificationPusher, SignalRNotificationPusher>();`
  - **JWT bearer event** (browsers can't send `Authorization` headers over WebSocket). In the existing `AddJwtBearer(...)` call, add `Events`:
    ```csharp
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            string? accessToken = context.Request.Query["access_token"];
            PathString path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
    ```

### 5. API — `Program.cs`

- `app.MapHub<NotificationHub>("/hubs/notifications");`
- CORS — SignalR with auth requires `.AllowCredentials()`. Update the existing CORS policy builder so the frontend origin is allowed AND credentials are permitted (cannot combine with `AllowAnyOrigin()` — use explicit `WithOrigins(...)`).

### 6. API — controller

**File:** `EasyLoginAPI/Controllers/NotificationsController.cs` (new). Pattern follows `EasyLoginAPI/Controllers/AuthController.cs`: `[ApiController]`, `[Route("api/notifications")]`, inject `IMediator`.

| Method | Route | Auth | Handler |
|--------|-------|------|---------|
| GET | `/api/notifications?unreadOnly=&skip=&take=` | `[Authorize]` | `GetMyNotificationsQuery` |
| GET | `/api/notifications/unread-count` | `[Authorize]` | `GetUnreadCountQuery` |
| PUT | `/api/notifications/{id}/read` | `[Authorize]` | `MarkNotificationReadCommand` |
| PUT | `/api/notifications/read-all` | `[Authorize]` | `MarkAllReadCommand` |
| POST | `/api/notifications/test/self` | `[AllowAnonymous]` | body `{ userId, title, message, type }` → `SendTestNotificationCommand` with `TargetUserId=userId` |
| POST | `/api/notifications/test/user/{userId}` | `[AllowAnonymous]` | route `userId`, body `{ title, message, type }` → `SendTestNotificationCommand` with `TargetUserId=userId` |
| POST | `/api/notifications/test/broadcast` | `[AllowAnonymous]` | body `{ title, message, type }` → `SendTestNotificationCommand` with `Broadcast=true` |

Note: with `[AllowAnonymous]`, `/test/self` is identical in shape to `/test/user/{userId}` since there is no JWT to derive "self". Both are kept for API symmetry — `/self` reads `userId` from the body, `/user/{userId}` from the route.

### 7. Packages

`Microsoft.AspNetCore.SignalR` is part of the `Microsoft.AspNetCore.App` framework reference — **no extra NuGet should be needed** in `EasyLoginAPI.csproj` or `EasyLogin.Infrastructure.csproj`. If you reference SignalR types from the Application layer (you shouldn't — that's why `INotificationPusher` exists), install `Microsoft.AspNetCore.SignalR.Common` there.

---

## Frontend — `src/EasyLoginUI/`

Stack reminder: Angular 19 standalone, Angular Material 19 (azure-blue), JWT in localStorage, `@if`/`@for` control flow, lazy-loaded routes, dark-mode via `ThemeService`. See `src/EasyLoginUI/CLAUDE.md`.

### 1. Dependency

```
npm install @microsoft/signalr
```

### 2. Model

**File:** `src/app/core/models/notification.model.ts` (new)

```ts
export interface NotificationItem {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  createdAt: string;
}
```

### 3. Service

**File:** `src/app/core/services/notification.service.ts` (new). `@Injectable({ providedIn: 'root' })`.

Responsibilities:
- `list$: BehaviorSubject<NotificationItem[]>` — last N (default 50), newest first.
- `unreadCount$: BehaviorSubject<number>`.
- Lazy `HubConnection`. `init()` is idempotent — called from `AppComponent` on boot if already authenticated and from `AuthService` after a successful login. `stop()` called on logout.
- Hub URL: `${environment.apiUrl.replace(/\/api$/, '')}/hubs/notifications`. Use:
  ```ts
  this.connection = new HubConnectionBuilder()
    .withUrl(hubUrl, { accessTokenFactory: () => this.auth.getAccessToken() ?? '' })
    .withAutomaticReconnect()
    .build();
  this.connection.on('notification', (n: NotificationItem) => {
    this.list$.next([n, ...this.list$.value].slice(0, 50));
    if (!n.isRead) this.unreadCount$.next(this.unreadCount$.value + 1);
  });
  ```
- HTTP via existing `ApiService`:
  - `loadInitial()` → `GET /notifications?unreadOnly=false&skip=0&take=50` → populate `list$`.
  - `loadUnreadCount()` → `GET /notifications/unread-count` → populate `unreadCount$`.
  - `markRead(id)` → `PUT /notifications/{id}/read`, then patch local list (`isRead = true`) and decrement count.
  - `markAllRead()` → `PUT /notifications/read-all`, then map list with `isRead = true`, count = 0.

Call `loadUnreadCount()` immediately after `connection.start()` resolves so the badge is correct on page load.

### 4. Navbar — bell UI

**Files:**
- `src/app/shared/components/navbar/navbar.component.ts`
- `src/app/shared/components/navbar/navbar.component.html`
- `src/app/shared/components/navbar/navbar.component.scss`

Changes:
- Add to component imports: `MatBadgeModule`, `MatListModule`, `MatDividerModule`, `AsyncPipe`, `DatePipe`.
- Inject `NotificationService` (public, so it's accessible from template via `notifications.list$ | async`).
- Insert the following block after `<span class="spacer"></span>` and before the user avatar `<button>`:

```html
<button mat-icon-button [matMenuTriggerFor]="notifMenu"
        (menuOpened)="notifications.loadInitial()"
        aria-label="Notifications">
  <mat-icon [matBadge]="(notifications.unreadCount$ | async) || null"
            matBadgeColor="warn"
            matBadgeSize="small">notifications</mat-icon>
</button>

<mat-menu #notifMenu="matMenu" class="notif-menu">
  <div class="notif-header" (click)="$event.stopPropagation()">
    <span>Notifications</span>
    <button mat-button (click)="notifications.markAllRead()">Mark all read</button>
  </div>
  <mat-divider></mat-divider>
  @for (n of (notifications.list$ | async); track n.id) {
    <button mat-menu-item (click)="onNotifClick(n)" [class.unread]="!n.isRead">
      <div class="notif-row">
        <strong>{{ n.title }}</strong>
        <small>{{ n.message }}</small>
        <small class="ts">{{ n.createdAt | date:'short' }}</small>
      </div>
    </button>
  } @empty {
    <div class="notif-empty">No notifications</div>
  }
</mat-menu>
```

Component method:
```ts
onNotifClick(n: NotificationItem) {
  if (!n.isRead) this.notifications.markRead(n.id);
}
```

SCSS additions (`navbar.component.scss`):
- `.notif-menu` wrapper: width ~340px, max-height ~480px, scroll on overflow.
- `.notif-header`: flex row, space-between, padding 8–12px.
- `.notif-row`: flex column; `strong` for title, `small` for body, `.ts` timestamp dim color.
- `.unread`: 3px left border (use the accent / azure-blue tone) and slightly heavier weight.
- Respect existing `.dark-mode` body class — re-use existing token / variable patterns from the file.

### 5. App bootstrap

**File:** `src/app/app.component.ts`
- `constructor` (or `ngOnInit`): if `auth.isAuthenticated()` → `notifications.init()`.
- Subscribe to `auth.user$` (or whatever the existing observable is): when value transitions truthy → `notifications.init()`; when null → `notifications.stop()`.

### 6. Environment

No new file needed. `apiUrl` already encodes dev (`http://localhost:8080/api`) and prod (`/api`). The hub URL is derived in the service by stripping `/api` (see §3). Nginx config in prod will need to forward `/hubs/` to the API container — **out of scope for this plan** unless the user requests it; flag it during implementation if Docker prod testing is needed.

---

## Critical Files

### Backend — new
- `EasyLogin.Domain/Entities/Notification.cs`
- `EasyLogin.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs`
- `EasyLogin.Infrastructure/Services/NotificationService.cs`
- `EasyLogin.Infrastructure/Realtime/NotificationHub.cs`
- `EasyLogin.Infrastructure/Realtime/JwtUserIdProvider.cs`
- `EasyLogin.Infrastructure/Realtime/SignalRNotificationPusher.cs`
- `EasyLogin.Application/Interfaces/INotificationService.cs`
- `EasyLogin.Application/Interfaces/INotificationPusher.cs`
- `EasyLogin.Application/Notifications/Dtos/NotificationResponse.cs`
- `EasyLogin.Application/Notifications/Commands/MarkNotificationReadCommand.cs`
- `EasyLogin.Application/Notifications/Commands/MarkAllReadCommand.cs`
- `EasyLogin.Application/Notifications/Commands/SendTestNotificationCommand.cs`
- `EasyLogin.Application/Notifications/Queries/GetMyNotificationsQuery.cs`
- `EasyLogin.Application/Notifications/Queries/GetUnreadCountQuery.cs`
- `EasyLoginAPI/Controllers/NotificationsController.cs`

### Backend — modified
- `EasyLogin.Infrastructure/Persistence/AppDbContext.cs` — add `DbSet<Notification>`
- `EasyLogin.Infrastructure/InfrastructureServiceExtensions.cs` — `AddSignalR`, DI, JWT `OnMessageReceived` query-string handler
- `EasyLoginAPI/Program.cs` — `MapHub`, CORS `.AllowCredentials()`
- `EasyLogin.Infrastructure/Identity/MappingProfiles/IdentityMappingConfig.cs` *or* new `NotificationMappingConfig.cs` — Mapster registration

### Frontend — new
- `src/app/core/models/notification.model.ts`
- `src/app/core/services/notification.service.ts`

### Frontend — modified
- `src/app/shared/components/navbar/navbar.component.{ts,html,scss}` — bell + menu
- `src/app/app.component.ts` — init / teardown hub on auth changes
- `package.json` — add `@microsoft/signalr`

---

## Verification

1. `dotnet build src/EasyLoginAPI/EasyLoginAPI.sln` → no errors.
2. From `src/EasyLoginAPI/`: `dotnet ef migrations add AddNotifications -p EasyLogin.Infrastructure -s EasyLoginAPI` → migration file generated.
3. Run API: `dotnet run --project src/EasyLoginAPI/EasyLoginAPI` → startup logs show migration applied, `Notifications` table exists in SQL Server.
4. Run frontend: `cd src/EasyLoginUI && ng serve` → log in as admin user.
5. Browser DevTools → Network → WS tab: confirm `/hubs/notifications` connection returns `101 Switching Protocols` with `access_token` in the query string.
6. From a second terminal (or Scalar/Postman):
   - `POST http://localhost:8080/api/notifications/test/user/{adminUserId}` with body `{ "title": "Hi", "message": "test", "type": "info" }` → bell badge increments **without page refresh**, item appears in the dropdown.
7. Click an unread item → `PUT /notifications/{id}/read` fires, badge decrements, `.unread` style clears.
8. `POST /api/notifications/test/broadcast` with the same body shape (no `userId`) → every connected user receives it.
9. Refresh page → bell badge reflects unread count (loaded via `GET /notifications/unread-count` on hub start), dropdown reloads via `GET /notifications` on menu open.
10. Log out → hub stops; further `POST .../test/...` from other clients do not impact the logged-out browser.

### Smoke matrix
| Scenario | Expected |
|----------|----------|
| Unauthenticated user, hub connect attempt | 401 / closed |
| Token expires mid-session | `withAutomaticReconnect` retries; on reconnect with fresh token (jwt interceptor refresh) hub continues |
| Two browser tabs same user | Both receive `notification` event (SignalR sends to all connections per user) |
| Broadcast with 0 users | 200 OK, list empty server-side, no event listeners receive it (no error) |

---

## Out of scope (flag if user asks later)
- Nginx config forwarding `/hubs/` in prod docker setup.
- Real producers wired into existing handlers (UserCreated, RoleAssigned, LoginSuccess) — pattern is ready (`INotificationService` + `INotificationPusher` injected into handler), but no handler edits are part of this plan.
- Notification preferences / channels / per-type opt-out.
- Pagination UI beyond initial 50.
- Localization of title/message.
- Audit log entries for notification CRUD (the existing `AuditLog` system covers auth + user/role mutations; notifications are intentionally not audited).
