import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap, map } from 'rxjs';
import { Router } from '@angular/router';
import { jwtDecode } from 'jwt-decode';
import { ApiService } from './api.service';
import {
  AuthResponse, LoginRequest, RegisterRequest,
  ForgotPasswordRequest, ResetPasswordRequest, RefreshTokenRequest
} from '../models/auth.model';
import { UserProfile } from '../models/user.model';

// .NET ClaimTypes.Role maps to this URI in the JWT
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

interface JwtPayload {
  sub: string;
  email: string;
  firstName: string;
  lastName: string;
  [key: string]: unknown;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  private accessToken: string | null = null;
  private refreshTokenValue: string | null = null;

  readonly currentUser$ = new BehaviorSubject<UserProfile | null>(null);

  isAuthenticated(): boolean {
    return this.accessToken !== null;
  }

  hasRole(role: string): boolean {
    return this.currentUser$.value?.roles.includes(role) ?? false;
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  private storeTokens(res: AuthResponse): void {
    this.accessToken = res.accessToken;
    this.refreshTokenValue = res.refreshToken;

    const decoded = jwtDecode<JwtPayload>(res.accessToken);
    const raw = decoded[ROLE_CLAIM];
    const roles = Array.isArray(raw) ? raw as string[] : raw ? [raw as string] : [];

    this.currentUser$.next({
      id: decoded.sub,
      email: decoded.email,
      firstName: decoded.firstName,
      lastName: decoded.lastName,
      roles,
    });
  }

  login(req: LoginRequest): Observable<void> {
    return this.api.post<AuthResponse>('/auth/login', req).pipe(
      tap(res => this.storeTokens(res)),
      map(() => void 0),
    );
  }

  register(req: RegisterRequest): Observable<void> {
    return this.api.post<AuthResponse>('/auth/register', req).pipe(
      tap(res => this.storeTokens(res)),
      map(() => void 0),
    );
  }

  logout(): void {
    this.api.post<void>('/auth/revoke', { refreshToken: this.refreshTokenValue } as RefreshTokenRequest)
      .subscribe({ error: () => {} });
    this.accessToken = null;
    this.refreshTokenValue = null;
    this.currentUser$.next(null);
    this.router.navigate(['/login']);
  }

  refreshToken(): Observable<AuthResponse> {
    return this.api.post<AuthResponse>('/auth/refresh', { refreshToken: this.refreshTokenValue } as RefreshTokenRequest).pipe(
      tap(res => this.storeTokens(res)),
    );
  }

  forgotPassword(req: ForgotPasswordRequest): Observable<void> {
    return this.api.post<void>('/auth/forgot-password', req);
  }

  resetPassword(req: ResetPasswordRequest): Observable<void> {
    return this.api.post<void>('/auth/reset-password', req);
  }
}
