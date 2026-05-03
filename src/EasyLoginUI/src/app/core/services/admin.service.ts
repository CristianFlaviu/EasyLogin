import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  PaginatedList, UserListItem, UserDetail, RoleItem,
  AdminCreateUserRequest, UpdateUserRequest, CreateRoleRequest,
} from '../models/user.model';
import { CompanyItem, CompanyRoleItem, CreateCompanyRequest, UpdateCompanyRequest } from '../models/company.model';

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

  // ── Companies ─────────────────────────────────────────────────────────────

  getCompanies(): Observable<CompanyItem[]> {
    return this.api.get<CompanyItem[]>('/superadmin/companies');
  }

  getCompany(id: string): Observable<CompanyItem> {
    return this.api.get<CompanyItem>(`/superadmin/companies/${id}`);
  }

  createCompany(request: CreateCompanyRequest): Observable<CompanyItem> {
    return this.api.post<CompanyItem>('/superadmin/companies', request);
  }

  updateCompany(id: string, request: UpdateCompanyRequest): Observable<CompanyItem> {
    return this.api.put<CompanyItem>(`/superadmin/companies/${id}`, request);
  }

  deleteCompany(id: string): Observable<void> {
    return this.api.delete<void>(`/superadmin/companies/${id}`);
  }

  getCompanyUsers(id: string, pageNumber: number, pageSize: number): Observable<PaginatedList<UserListItem>> {
    return this.api.get<PaginatedList<UserListItem>>(
      `/superadmin/companies/${id}/users?pageNumber=${pageNumber}&pageSize=${pageSize}`
    );
  }

  getCompanyRoles(id: string): Observable<CompanyRoleItem[]> {
    return this.api.get<CompanyRoleItem[]>(`/superadmin/companies/${id}/roles`);
  }
}
