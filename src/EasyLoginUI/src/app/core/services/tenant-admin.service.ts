import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  PaginatedList, UserListItem, UserDetail, OverviewResponse,
  OverviewLoginItem, OverviewActiveSessionItem,
} from '../models/user.model';
import {
  TenantRoleItem, CreateTenantRoleRequest,
  CreateTenantUserRequest, UpdateTenantUserRequest,
  InviteTenantUserRequest, TenantItem,
} from '../models/tenant.model';

@Injectable({ providedIn: 'root' })
export class TenantAdminService {
  private readonly api = inject(ApiService);

  getOverview(): Observable<OverviewResponse> {
    return this.api.get<OverviewResponse>('/tenant/overview');
  }

  getOverviewLogins(pageNumber: number, pageSize: number): Observable<PaginatedList<OverviewLoginItem>> {
    return this.api.get<PaginatedList<OverviewLoginItem>>(
      `/tenant/overview/logins?pageNumber=${pageNumber}&pageSize=${pageSize}`
    );
  }

  getOverviewActiveSessions(
    pageNumber: number,
    pageSize: number
  ): Observable<PaginatedList<OverviewActiveSessionItem>> {
    return this.api.get<PaginatedList<OverviewActiveSessionItem>>(
      `/tenant/overview/sessions?pageNumber=${pageNumber}&pageSize=${pageSize}`
    );
  }

  // ── Users ────────────────────────────────────────────────────────────────

  getUsers(pageNumber: number, pageSize: number): Observable<PaginatedList<UserListItem>> {
    return this.api.get<PaginatedList<UserListItem>>(`/tenant/users?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getUser(id: string): Observable<UserDetail> {
    return this.api.get<UserDetail>(`/tenant/users/${id}`);
  }

  createUser(request: CreateTenantUserRequest): Observable<UserDetail> {
    return this.api.post<UserDetail>('/tenant/users', request);
  }

  inviteUser(request: InviteTenantUserRequest): Observable<UserDetail> {
    return this.api.post<UserDetail>('/tenant/users/invite', request);
  }

  resendInvite(id: string): Observable<void> {
    return this.api.post<void>(`/tenant/users/${id}/resend-invite`, {});
  }

  revokeInvite(id: string): Observable<void> {
    return this.api.post<void>(`/tenant/users/${id}/revoke-invite`, {});
  }

  suspendUser(id: string): Observable<void> {
    return this.api.post<void>(`/tenant/users/${id}/suspend`, {});
  }

  updateUser(id: string, request: UpdateTenantUserRequest): Observable<UserDetail> {
    return this.api.put<UserDetail>(`/tenant/users/${id}`, request);
  }

  deleteUser(id: string): Observable<void> {
    return this.api.delete<void>(`/tenant/users/${id}`);
  }

  // ── Tenant Roles ─────────────────────────────────────────────────────────

  getRoles(): Observable<TenantRoleItem[]> {
    return this.api.get<TenantRoleItem[]>('/tenant/roles');
  }

  getContext(): Observable<TenantItem> {
    return this.api.get<TenantItem>('/tenant/context');
  }

  createRole(request: CreateTenantRoleRequest): Observable<TenantRoleItem> {
    return this.api.post<TenantRoleItem>('/tenant/roles', request);
  }

  deleteRole(id: string): Observable<void> {
    return this.api.delete<void>(`/tenant/roles/${id}`);
  }
}
