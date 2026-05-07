import { UserStatus } from './user.model';

export interface TenantItem {
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface TenantRoleItem {
  id: string;
  name: string;
  description: string | null;
  tenantId: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateTenantRequest {
  name: string;
}

export interface UpdateTenantRequest {
  name: string;
  isActive: boolean;
}

export interface CreateTenantRoleRequest {
  name: string;
  description: string | null;
}

export interface CreateTenantUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  tenantRoleIds: string[];
}

export interface InviteTenantUserRequest {
  email: string;
  tenantRoleId: string;
}

export interface UpdateTenantUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  status: UserStatus;
  tenantRoleIds: string[];
  newPassword: string | null;
}
