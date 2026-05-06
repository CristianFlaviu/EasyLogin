import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  PaginatedList, UserListItem, UserDetail, RoleItem,
  AdminCreateUserRequest, UpdateUserRequest, CreateRoleRequest, InviteUserRequest,
} from '../models/user.model';
import { TenantItem, TenantRoleItem, CreateTenantRequest, UpdateTenantRequest } from '../models/tenant.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly api = inject(ApiService);

  // ── Users ────────────────────────────────────────────────────────────────

  getUsers(pageNumber: number, pageSize: number): Observable<PaginatedList<UserListItem>> {
    return this.api.get<PaginatedList<UserListItem>>(`/superadmin/users?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getUser(id: string): Observable<UserDetail> {
    return this.api.get<UserDetail>(`/superadmin/users/${id}`);
  }

  createUser(request: AdminCreateUserRequest): Observable<UserDetail> {
    return this.api.post<UserDetail>('/superadmin/users', request);
  }

  inviteUser(request: InviteUserRequest): Observable<UserDetail> {
    return this.api.post<UserDetail>('/superadmin/users/invite', request);
  }

  resendInvite(userId: string): Observable<void> {
    return this.api.post<void>(`/superadmin/users/${userId}/resend-invite`, {});
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<UserDetail> {
    return this.api.put<UserDetail>(`/superadmin/users/${id}`, request);
  }

  deleteUser(id: string): Observable<void> {
    return this.api.delete<void>(`/superadmin/users/${id}`);
  }

  // ── System Roles ─────────────────────────────────────────────────────────

  getRoles(): Observable<RoleItem[]> {
    return this.api.get<RoleItem[]>('/superadmin/roles');
  }

  createRole(request: CreateRoleRequest): Observable<RoleItem> {
    return this.api.post<RoleItem>('/superadmin/roles', request);
  }

  deleteRole(id: string): Observable<void> {
    return this.api.delete<void>(`/superadmin/roles/${id}`);
  }

  // ── Tenants ─────────────────────────────────────────────────────────────

  getTenants(): Observable<TenantItem[]> {
    return this.api.get<TenantItem[]>('/superadmin/tenants');
  }

  getTenant(id: string): Observable<TenantItem> {
    return this.api.get<TenantItem>(`/superadmin/tenants/${id}`);
  }

  createTenant(request: CreateTenantRequest): Observable<TenantItem> {
    return this.api.post<TenantItem>('/superadmin/tenants', request);
  }

  updateTenant(id: string, request: UpdateTenantRequest): Observable<TenantItem> {
    return this.api.put<TenantItem>(`/superadmin/tenants/${id}`, request);
  }

  deleteTenant(id: string): Observable<void> {
    return this.api.delete<void>(`/superadmin/tenants/${id}`);
  }

  getTenantUsers(id: string, pageNumber: number, pageSize: number): Observable<PaginatedList<UserListItem>> {
    return this.api.get<PaginatedList<UserListItem>>(
      `/superadmin/tenants/${id}/users?pageNumber=${pageNumber}&pageSize=${pageSize}`
    );
  }

  getTenantRoles(id: string): Observable<TenantRoleItem[]> {
    return this.api.get<TenantRoleItem[]>(`/superadmin/tenants/${id}/roles`);
  }
}
