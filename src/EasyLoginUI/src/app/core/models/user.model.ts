export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  companyId: string | null;
  companyName: string | null;
  roles: string[];
  companyRoles: string[];
}

export interface UserListItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  companyId: string | null;
  companyName: string | null;
  roles: string[];
  companyRoles: string[];
  status: string;
}

export interface UserDetail {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  companyId: string | null;
  companyName: string | null;
  roles: string[];
  companyRoles: string[];
  status: string;
}

export interface RoleItem {
  id: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface PaginatedList<T> {
  items: T[];
  pageNumber: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface AdminCreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  systemRoles: string[];
  companyId: string | null;
}

export interface InviteUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  systemRoles: string[];
  companyId: string | null;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  systemRoles: string[];
  newPassword: string | null;
}

export interface CreateRoleRequest {
  name: string;
  description: string | null;
}
