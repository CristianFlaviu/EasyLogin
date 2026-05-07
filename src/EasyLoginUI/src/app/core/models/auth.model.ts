export interface AuthResponse {
  accessToken: string | null;
  refreshToken: string | null;
  expiresIn: number;
  requiresTwoFactor: boolean;
  twoFactorToken: string | null;
  twoFactorMethod: 'Authenticator' | 'Email' | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface AcceptInviteRequest {
  token: string;
  password: string;
  confirmPassword: string;
}

export interface InviteValidationResponse {
  email: string;
  firstName: string;
  lastName: string;
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}

export interface TwoFactorSetupResponse {
  otpAuthUri: string;
  sharedSecret: string;
}

export interface VerifyTwoFactorRequest {
  twoFactorToken: string;
  code: string;
}

export interface EnableTwoFactorRequest {
  password: string;
}

export interface ConfirmTwoFactorRequest {
  code: string;
}

export interface SensitiveTwoFactorRequest {
  password: string;
  code: string;
}

export interface ConfirmEmailRequest {
  email: string;
  token: string;
}

export interface ResendEmailConfirmationRequest {
  email: string;
}
