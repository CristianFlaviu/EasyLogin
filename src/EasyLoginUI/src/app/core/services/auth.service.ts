import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { ApiService } from './api.service';
import {
  AuthResponse, LoginRequest,
  ForgotPasswordRequest, ResetPasswordRequest, RefreshTokenRequest,
  AcceptInviteRequest, InviteValidationResponse,
  VerifyTwoFactorRequest, TwoFactorSetupResponse,
  EnableTwoFactorRequest, ConfirmTwoFactorRequest, SensitiveTwoFactorRequest,
  RegisterRequest, RegisterResponse, ConfirmEmailRequest, ResendEmailConfirmationRequest,
} from '../models/auth.model';
import { UserProfile } from '../models/user.model';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

interface JwtPayload {
  sub: string;
  email: string;
  firstName: string;
  lastName: string;
  tenant_id?: string;
  [key: string]: unknown;
}

const LS_ACCESS_TOKEN = 'access_token';
const LS_REFRESH_TOKEN = 'refresh_token';
const SS_TWO_FACTOR_TOKEN = 'two_factor_token';
const SS_TWO_FACTOR_RETURN_URL = 'two_factor_return_url';
const SS_TWO_FACTOR_METHOD = 'two_factor_method';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  private accessToken: string | null = localStorage.getItem(LS_ACCESS_TOKEN);
  private refreshTokenValue: string | null = localStorage.getItem(LS_REFRESH_TOKEN);

  readonly currentUser$ = new BehaviorSubject<UserProfile | null>(
    this.accessToken ? this.decodeUser(this.accessToken) : null
  );

  isAuthenticated(): boolean {
    return this.accessToken !== null;
  }

  hasRole(role: string): boolean {
    return this.currentUser$.value?.roles.includes(role) ?? false;
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  getProfile(): Observable<UserProfile> {
    return this.api.get<UserProfile>('/user/profile');
  }

  private decodeUser(token: string): UserProfile {
    const decoded = jwtDecode<JwtPayload>(token);
    const raw = decoded[ROLE_CLAIM];
    const roles = Array.isArray(raw) ? raw as string[] : raw ? [raw as string] : [];
    return {
      id: decoded.sub,
      email: decoded.email,
      firstName: decoded.firstName,
      lastName: decoded.lastName,
      tenantId: decoded.tenant_id ?? null,
      tenantName: null,
      roles,
      tenantRoles: [],
      twoFactorEnabled: false,
      twoFactorMethod: null,
      emailConfirmed: true,
    };
  }

  private storeTokens(res: AuthResponse): void {
    if (!res.accessToken || !res.refreshToken) {
      throw new Error('Authentication response did not include tokens.');
    }

    this.accessToken = res.accessToken;
    this.refreshTokenValue = res.refreshToken;
    localStorage.setItem(LS_ACCESS_TOKEN, res.accessToken);
    localStorage.setItem(LS_REFRESH_TOKEN, res.refreshToken);
    this.currentUser$.next(this.decodeUser(res.accessToken));
  }

  login(req: LoginRequest, returnUrl: string): Observable<AuthResponse> {
    return this.api.post<AuthResponse>('/auth/login', req).pipe(
      tap(res => {
        if (res.requiresTwoFactor && res.twoFactorToken) {
          sessionStorage.setItem(SS_TWO_FACTOR_TOKEN, res.twoFactorToken);
          sessionStorage.setItem(SS_TWO_FACTOR_RETURN_URL, returnUrl);
          if (res.twoFactorMethod)
            sessionStorage.setItem(SS_TWO_FACTOR_METHOD, res.twoFactorMethod);
          return;
        }

        this.clearTwoFactorChallenge();
        this.storeTokens(res);
      }),
    );
  }

  getTwoFactorChallengeToken(): string | null {
    return sessionStorage.getItem(SS_TWO_FACTOR_TOKEN);
  }

  getTwoFactorReturnUrl(): string {
    return sessionStorage.getItem(SS_TWO_FACTOR_RETURN_URL) ?? '/dashboard';
  }

  getTwoFactorMethod(): 'Authenticator' | 'Email' | null {
    const value = sessionStorage.getItem(SS_TWO_FACTOR_METHOD);
    return value === 'Authenticator' || value === 'Email' ? value : null;
  }

  clearTwoFactorChallenge(): void {
    sessionStorage.removeItem(SS_TWO_FACTOR_TOKEN);
    sessionStorage.removeItem(SS_TWO_FACTOR_RETURN_URL);
    sessionStorage.removeItem(SS_TWO_FACTOR_METHOD);
  }

  verifyTwoFactor(req: Omit<VerifyTwoFactorRequest, 'twoFactorToken'>): Observable<AuthResponse> {
    const twoFactorToken = this.getTwoFactorChallengeToken();
    if (!twoFactorToken) {
      throw new Error('Missing two-factor challenge token.');
    }

    return this.api.post<AuthResponse>('/auth/login/verify-2fa', {
      twoFactorToken,
      code: req.code,
    } satisfies VerifyTwoFactorRequest).pipe(
      tap(res => {
        this.storeTokens(res);
        this.clearTwoFactorChallenge();
      }),
    );
  }

  logout(): void {
    this.api.post<void>('/auth/revoke', { refreshToken: this.refreshTokenValue } as RefreshTokenRequest)
      .subscribe({ error: () => {} });
    this.accessToken = null;
    this.refreshTokenValue = null;
    localStorage.removeItem(LS_ACCESS_TOKEN);
    localStorage.removeItem(LS_REFRESH_TOKEN);
    this.clearTwoFactorChallenge();
    this.currentUser$.next(null);
    this.router.navigate(['/login']);
  }

  refreshToken(): Observable<AuthResponse> {
    const body: RefreshTokenRequest = {
      accessToken: this.accessToken ?? '',
      refreshToken: this.refreshTokenValue ?? '',
    };
    return this.api.post<AuthResponse>('/auth/refresh', body).pipe(
      tap(res => this.storeTokens(res)),
    );
  }

  forgotPassword(req: ForgotPasswordRequest): Observable<void> {
    return this.api.post<void>('/auth/forgot-password', req);
  }

  resetPassword(req: ResetPasswordRequest): Observable<void> {
    return this.api.post<void>('/auth/reset-password', req);
  }

  validateInviteToken(token: string): Observable<InviteValidationResponse> {
    return this.api.get<InviteValidationResponse>(`/auth/invite/validate?token=${encodeURIComponent(token)}`);
  }

  acceptInvite(req: AcceptInviteRequest): Observable<void> {
    return this.api.post<void>('/auth/accept-invite', req);
  }

  register(req: RegisterRequest): Observable<RegisterResponse> {
    return this.api.post<RegisterResponse>('/auth/register', req);
  }

  confirmEmail(req: ConfirmEmailRequest): Observable<void> {
    return this.api.post<void>('/auth/confirm-email', req);
  }

  resendEmailConfirmation(req: ResendEmailConfirmationRequest): Observable<void> {
    return this.api.post<void>('/auth/resend-confirmation', req);
  }

  enableTwoFactor(req: EnableTwoFactorRequest): Observable<TwoFactorSetupResponse> {
    return this.api.post<TwoFactorSetupResponse>('/auth/2fa/enable', req);
  }

  confirmTwoFactor(req: ConfirmTwoFactorRequest): Observable<void> {
    return this.api.post<void>('/auth/2fa/confirm', req);
  }

  enableEmailTwoFactor(req: EnableTwoFactorRequest): Observable<void> {
    return this.api.post<void>('/auth/2fa/email/enable', req);
  }

  sendEmailTwoFactorCode(): Observable<void> {
    return this.api.post<void>('/auth/2fa/email/send-code', {});
  }

  disableTwoFactor(req: SensitiveTwoFactorRequest): Observable<void> {
    return this.api.post<void>('/auth/2fa/disable', req);
  }
}
