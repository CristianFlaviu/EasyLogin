export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  tenantId: string | null;
  tenantName: string | null;
  roles: string[];
  tenantRoles: string[];
  twoFactorEnabled: boolean;
  twoFactorMethod: 'Authenticator' | 'Email' | null;
  emailConfirmed: boolean;
}

export interface OverviewResponse {
  totalUsers: number;
  loginsLast24Hours: number;
  activeSessions: number;
}

export interface OverviewLoginItem {
  id: string;
  timestamp: string;
  actorUserId: string | null;
  actorEmail: string | null;
  ipAddress: string | null;
  browserName: string | null;
  osName: string | null;
  deviceFamily: string | null;
}

export interface OverviewActiveSessionItem {
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  tenantId: string | null;
  tenantName: string | null;
  refreshTokenExpiry: string | null;
}

export interface UserListItem {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
  tenantId: string | null;
  tenantName: string | null;
  roles: string[];
  tenantRoles: string[];
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
  tenantId: string | null;
  tenantName: string | null;
  roles: string[];
  tenantRoles: string[];
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
  tenantId: string | null;
}

export interface InviteUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  systemRoles: string[];
  tenantId: string | null;
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
