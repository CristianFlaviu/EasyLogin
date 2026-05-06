import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';
import { noAuthGuard } from './core/guards/no-auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  {
    path: 'login',
    canActivate: [noAuthGuard],
    loadComponent: () => import('./features/auth/auth-layout/auth-layout.component').then(m => m.AuthLayoutComponent),
    children: [{ path: '', pathMatch: 'full', loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent) }],
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/auth-layout/auth-layout.component').then(m => m.AuthLayoutComponent),
    children: [{ path: '', pathMatch: 'full', loadComponent: () => import('./features/auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) }],
  },
  {
    path: 'reset-password',
    loadComponent: () => import('./features/auth/reset-password/reset-password.component').then(m => m.ResetPasswordComponent),
  },
  {
    path: 'accept-invite',
    loadComponent: () => import('./features/auth/accept-invite/accept-invite.component').then(m => m.AcceptInviteComponent),
  },

  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'profile', canActivate: [authGuard], loadComponent: () => import('./features/user/profile/profile.component').then(m => m.ProfileComponent) },

  // SuperAdmin routes
  {
    path: 'superadmin/tenants',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SuperAdmin'] },
    loadComponent: () => import('./features/superadmin/tenant-list/tenant-list.component').then(m => m.TenantListComponent),
  },
  {
    path: 'superadmin/users',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SuperAdmin'] },
    loadComponent: () => import('./features/admin/user-list/user-list.component').then(m => m.UserListComponent),
  },
  {
    path: 'superadmin/roles',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['SuperAdmin'] },
    loadComponent: () => import('./features/admin/role-list/role-list.component').then(m => m.RoleListComponent),
  },

  // TenantAdmin routes
  {
    path: 'tenant/users',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['TenantAdmin'] },
    loadComponent: () => import('./features/tenant/user-list/tenant-user-list.component').then(m => m.TenantUserListComponent),
  },
  {
    path: 'tenant/roles',
    canActivate: [authGuard, roleGuard],
    data: { roles: ['TenantAdmin'] },
    loadComponent: () => import('./features/tenant/role-list/tenant-role-list.component').then(m => m.TenantRoleListComponent),
  },

  { path: 'unauthorized', loadComponent: () => import('./shared/components/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent) },
  { path: '**', loadComponent: () => import('./shared/components/page-not-found/page-not-found.component').then(m => m.PageNotFoundComponent) },
];
