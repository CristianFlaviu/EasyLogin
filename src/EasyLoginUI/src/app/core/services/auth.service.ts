import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { ApiService } from './api.service';
import {
  AuthResponse, LoginRequest,
  ForgotPasswordRequest, ResetPasswordRequest, RefreshTokenRequest,
  AcceptInviteRequest, InviteValidationResponse,
} from '../models/auth.model';
import { UserProfile } from '../models/user.model';

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

interface JwtPayload {
  sub: string;
  email: string;
  firstName: string;
  lastName: string;
  company_id?: string;
  [key: string]: unknown;
}

const LS_ACCESS_TOKEN = 'access_token';
const LS_REFRESH_TOKEN = 'refresh_token';

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
      companyId: decoded.company_id ?? null,
      companyName: null,
      roles,
      companyRoles: [],
    };
  }

  private storeTokens(res: AuthResponse): void {
    this.accessToken = res.accessToken;
    this.refreshTokenValue = res.refreshToken;
    localStorage.setItem(LS_ACCESS_TOKEN, res.accessToken);
    localStorage.setItem(LS_REFRESH_TOKEN, res.refreshToken);
    this.currentUser$.next(this.decodeUser(res.accessToken));
  }

  login(req: LoginRequest): Observable<void> {
    return this.api.post<AuthResponse>('/auth/login', req).pipe(
      tap(res => this.storeTokens(res)),
      map(() => void 0),
    );
  }

  logout(): void {
    this.api.post<void>('/auth/revoke', { refreshToken: this.refreshTokenValue } as RefreshTokenRequest)
      .subscribe({ error: () => {} });
    this.accessToken = null;
    this.refreshTokenValue = null;
    localStorage.removeItem(LS_ACCESS_TOKEN);
    localStorage.removeItem(LS_REFRESH_TOKEN);
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
}
