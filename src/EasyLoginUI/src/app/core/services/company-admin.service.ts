import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { PaginatedList, UserListItem, UserDetail } from '../models/user.model';
import {
  CompanyRoleItem, CreateCompanyRoleRequest,
  CreateCompanyUserRequest, UpdateCompanyUserRequest,
} from '../models/company.model';

@Injectable({ providedIn: 'root' })
export class CompanyAdminService {
  private readonly api = inject(ApiService);

  // ── Users ────────────────────────────────────────────────────────────────

  getUsers(pageNumber: number, pageSize: number): Observable<PaginatedList<UserListItem>> {
    return this.api.get<PaginatedList<UserListItem>>(`/company/users?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getUser(id: string): Observable<UserDetail> {
    return this.api.get<UserDetail>(`/company/users/${id}`);
  }

  createUser(request: CreateCompanyUserRequest): Observable<UserDetail> {
    return this.api.post<UserDetail>('/company/users', request);
  }

  updateUser(id: string, request: UpdateCompanyUserRequest): Observable<UserDetail> {
    return this.api.put<UserDetail>(`/company/users/${id}`, request);
  }

  deleteUser(id: string): Observable<void> {
    return this.api.delete<void>(`/company/users/${id}`);
  }

  // ── Company Roles ─────────────────────────────────────────────────────────

  getRoles(): Observable<CompanyRoleItem[]> {
    return this.api.get<CompanyRoleItem[]>('/company/roles');
  }

  createRole(request: CreateCompanyRoleRequest): Observable<CompanyRoleItem> {
    return this.api.post<CompanyRoleItem>('/company/roles', request);
  }

  deleteRole(id: string): Observable<void> {
    return this.api.delete<void>(`/company/roles/${id}`);
  }
}
