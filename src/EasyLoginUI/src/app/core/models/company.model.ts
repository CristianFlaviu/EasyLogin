export interface CompanyItem {
  id: string;
  name: string;
  isActive: boolean;
  createdAt: string;
}

export interface CompanyRoleItem {
  id: string;
  name: string;
  description: string | null;
  companyId: string;
  createdAt: string;
}

export interface CreateCompanyRequest {
  name: string;
}

export interface UpdateCompanyRequest {
  name: string;
  isActive: boolean;
}

export interface CreateCompanyRoleRequest {
  name: string;
  description: string | null;
}

export interface CreateCompanyUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  companyRoleIds: string[];
}

export interface UpdateCompanyUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  companyRoleIds: string[];
  newPassword: string | null;
}
