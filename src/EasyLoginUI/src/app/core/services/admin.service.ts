import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  PaginatedList, UserListItem, UserDetail, RoleItem,
  AdminCreateUserRequest, UpdateUserRequest, CreateRoleRequest,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly api = inject(ApiService);

  getUsers(pageNumber: number, pageSize: number): Observable<PaginatedList<UserListItem>> {
    return this.api.get<PaginatedList<UserListItem>>(`/admin/users?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  getUser(id: string): Observable<UserDetail> {
    return this.api.get<UserDetail>(`/admin/users/${id}`);
  }

  createUser(request: AdminCreateUserRequest): Observable<UserDetail> {
    return this.api.post<UserDetail>('/admin/users', request);
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<UserDetail> {
    return this.api.put<UserDetail>(`/admin/users/${id}`, request);
  }

  deleteUser(id: string): Observable<void> {
    return this.api.delete<void>(`/admin/users/${id}`);
  }

  getRoles(): Observable<RoleItem[]> {
    return this.api.get<RoleItem[]>('/admin/roles');
  }

  createRole(request: CreateRoleRequest): Observable<RoleItem> {
    return this.api.post<RoleItem>('/admin/roles', request);
  }

  deleteRole(id: string): Observable<void> {
    return this.api.delete<void>(`/admin/roles/${id}`);
  }
}
