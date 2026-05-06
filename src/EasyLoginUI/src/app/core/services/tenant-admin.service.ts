import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { PaginatedList, UserListItem, UserDetail } from '../models/user.model';
import {
  TenantRoleItem, CreateTenantRoleRequest,
  CreateTenantUserRequest, UpdateTenantUserRequest,
} from '../models/tenant.model';

@Injectable({ providedIn: 'root' })
export class TenantAdminService {
  private readonly api = inject(ApiService);

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

  createRole(request: CreateTenantRoleRequest): Observable<TenantRoleItem> {
    return this.api.post<TenantRoleItem>('/tenant/roles', request);
  }

  deleteRole(id: string): Observable<void> {
    return this.api.delete<void>(`/tenant/roles/${id}`);
  }
}
