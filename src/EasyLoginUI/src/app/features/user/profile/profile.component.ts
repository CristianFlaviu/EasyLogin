import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import * as QRCode from 'qrcode';
import { AuthService } from '../../../core/services/auth.service';
import { UserProfile } from '../../../core/models/user.model';
import { TwoFactorSetupResponse } from '../../../core/models/auth.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule, MatCardModule, MatChipsModule,
    MatFormFieldModule, MatIconModule, MatInputModule,
    MatProgressBarModule, MatProgressSpinnerModule,
  ],
  template: `
    <div class="page-container">
      @if (loading) {
        <mat-progress-bar mode="indeterminate" />
      }

      @if (profile) {
        <section class="profile-hero">
          <div class="profile-avatar">{{ initials }}</div>
          <div class="profile-heading">
            <h1>{{ profile.firstName }} {{ profile.lastName }}</h1>
            <p>{{ profile.email }}</p>
          </div>
        </section>

        <div class="profile-grid">
          <mat-card class="profile-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>badge</mat-icon>
              <mat-card-title>Account</mat-card-title>
              <mat-card-subtitle>Identity details</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <dl class="detail-list">
                <div><dt>First Name</dt><dd>{{ profile.firstName }}</dd></div>
                <div><dt>Last Name</dt><dd>{{ profile.lastName }}</dd></div>
                <div><dt>Email</dt><dd>{{ profile.email }}</dd></div>
                <div><dt>User ID</dt><dd class="mono">{{ profile.id }}</dd></div>
              </dl>
            </mat-card-content>
          </mat-card>

          <mat-card class="profile-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>domain</mat-icon>
              <mat-card-title>Tenant</mat-card-title>
              <mat-card-subtitle>Organization context</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <dl class="detail-list">
                <div><dt>Name</dt><dd>{{ profile.tenantName ?? 'No tenant assigned' }}</dd></div>
                <div><dt>Tenant ID</dt><dd class="mono">{{ profile.tenantId ?? '-' }}</dd></div>
              </dl>
            </mat-card-content>
          </mat-card>

          <mat-card class="profile-card security-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>security</mat-icon>
              <mat-card-title>Security</mat-card-title>
              <mat-card-subtitle>
                <mat-chip-set>
                  <mat-chip>{{ profile.twoFactorEnabled ? '2FA enabled' : '2FA disabled' }}</mat-chip>
                  @if (profile.twoFactorMethod) {
                    <mat-chip>{{ profile.twoFactorMethod }}</mat-chip>
                  }
                </mat-chip-set>
              </mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              @if (!profile.twoFactorEnabled) {
                <form [formGroup]="setupPasswordForm" (ngSubmit)="startTwoFactorSetup()" class="security-form">
                  <mat-form-field appearance="outline">
                    <mat-label>Current password</mat-label>
                    <input matInput formControlName="password" type="password" autocomplete="current-password">
                    @if (setupPasswordForm.get('password')?.errors?.['required']) {
                      <mat-error>Password is required</mat-error>
                    }
                  </mat-form-field>
                  <button mat-flat-button color="primary" type="submit"
                          [disabled]="setupPasswordForm.invalid || actionLoading === 'enable'">
                    @if (actionLoading === 'enable') {
                      <mat-spinner diameter="18" />
                    }
                    <mat-icon [class.is-hidden]="actionLoading === 'enable'">qr_code_2</mat-icon>
                    <span [class.is-hidden]="actionLoading === 'enable'">Set up authenticator</span>
                  </button>
                </form>

                <form [formGroup]="emailPasswordForm" (ngSubmit)="enableEmailTwoFactor()" class="security-form">
                  <mat-form-field appearance="outline">
                    <mat-label>Current password</mat-label>
                    <input matInput formControlName="password" type="password" autocomplete="current-password">
                    @if (emailPasswordForm.get('password')?.errors?.['required']) {
                      <mat-error>Password is required</mat-error>
                    }
                  </mat-form-field>
                  <button mat-stroked-button color="primary" type="submit"
                          [disabled]="emailPasswordForm.invalid || actionLoading === 'enableEmail' || !profile.emailConfirmed">
                    @if (actionLoading === 'enableEmail') {
                      <mat-spinner diameter="18" />
                    }
                    <mat-icon [class.is-hidden]="actionLoading === 'enableEmail'">mail</mat-icon>
                    <span [class.is-hidden]="actionLoading === 'enableEmail'">Use email codes</span>
                  </button>
                </form>

                @if (setup) {
                  <div class="setup-grid">
                    <div class="qr-box">
                      @if (qrCodeDataUrl) {
                        <img [src]="qrCodeDataUrl" alt="Authenticator QR code">
                      }
                    </div>
                    <div class="setup-fields">
                      <dl class="detail-list">
                        <div><dt>Manual secret</dt><dd class="mono">{{ setup.sharedSecret }}</dd></div>
                      </dl>
                      <form [formGroup]="confirmForm" (ngSubmit)="confirmTwoFactor()" class="security-form compact">
                        <mat-form-field appearance="outline">
                          <mat-label>Authenticator code</mat-label>
                          <input matInput formControlName="code" inputmode="numeric" autocomplete="one-time-code">
                          @if (confirmForm.get('code')?.errors?.['required']) {
                            <mat-error>Code is required</mat-error>
                          }
                        </mat-form-field>
                        <button mat-flat-button color="primary" type="submit"
                                [disabled]="confirmForm.invalid || actionLoading === 'confirm'">
                          @if (actionLoading === 'confirm') {
                            <mat-spinner diameter="18" />
                          }
                          <mat-icon [class.is-hidden]="actionLoading === 'confirm'">verified_user</mat-icon>
                          <span [class.is-hidden]="actionLoading === 'confirm'">Enable 2FA</span>
                        </button>
                      </form>
                    </div>
                  </div>
                }
              } @else {
                @if (profile.twoFactorMethod === 'Email') {
                  <div class="email-code-row">
                    <button mat-stroked-button color="primary" type="button"
                            [disabled]="actionLoading === 'sendEmailCode'"
                            (click)="sendEmailCode()">
                      @if (actionLoading === 'sendEmailCode') {
                        <mat-spinner diameter="18" />
                      }
                      <mat-icon [class.is-hidden]="actionLoading === 'sendEmailCode'">mail</mat-icon>
                      <span [class.is-hidden]="actionLoading === 'sendEmailCode'">Send email code</span>
                    </button>
                  </div>
                }

                <div class="security-actions">
                  <form [formGroup]="disableForm" (ngSubmit)="disableTwoFactor()" class="sensitive-form danger">
                    <h2>Disable 2FA</h2>
                    <mat-form-field appearance="outline">
                      <mat-label>Current password</mat-label>
                      <input matInput formControlName="password" type="password" autocomplete="current-password">
                    </mat-form-field>
                    <mat-form-field appearance="outline">
                      <mat-label>{{ profile.twoFactorMethod === 'Email' ? 'Email code' : 'Authenticator code' }}</mat-label>
                      <input matInput formControlName="code" inputmode="numeric" autocomplete="one-time-code">
                    </mat-form-field>
                    <button mat-stroked-button color="warn" type="submit"
                            [disabled]="disableForm.invalid || actionLoading === 'disable'">
                      @if (actionLoading === 'disable') {
                        <mat-spinner diameter="18" />
                      }
                      <mat-icon [class.is-hidden]="actionLoading === 'disable'">lock_open</mat-icon>
                      <span [class.is-hidden]="actionLoading === 'disable'">Disable 2FA</span>
                    </button>
                  </form>
                </div>
              }
            </mat-card-content>
          </mat-card>

          <mat-card class="profile-card access-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>admin_panel_settings</mat-icon>
              <mat-card-title>Access</mat-card-title>
              <mat-card-subtitle>System and tenant roles</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <div class="role-section">
                <h2>System Roles</h2>
                <mat-chip-set>
                  @for (role of profile.roles; track role) {
                    <mat-chip>{{ role }}</mat-chip>
                  }
                  @if (profile.roles.length === 0) {
                    <span class="muted">None</span>
                  }
                </mat-chip-set>
              </div>

              <div class="role-section">
                <h2>Tenant Roles</h2>
                <mat-chip-set>
                  @for (role of profile.tenantRoles; track role) {
                    <mat-chip>{{ role }}</mat-chip>
                  }
                  @if (profile.tenantRoles.length === 0) {
                    <span class="muted">None</span>
                  }
                </mat-chip-set>
              </div>
            </mat-card-content>
          </mat-card>
        </div>
      } @else if (!loading) {
        <mat-card>
          <mat-card-content>
            <p class="error-state">Unable to load profile.</p>
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .page-container {
      padding: 32px 24px;
      max-width: 1040px;
      margin: 0 auto;
    }

    .profile-hero {
      min-height: 132px;
      display: flex;
      align-items: center;
      gap: 18px;
      padding: 24px;
      border: 1px solid #d9e2ef;
      border-radius: 8px;
      background: #ffffff;
      margin-bottom: 18px;
    }

    .profile-avatar {
      width: 72px;
      height: 72px;
      border-radius: 50%;
      background: #e8f1ff;
      color: #1565c0;
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 700;
      font-size: 1.15rem;
      text-transform: uppercase;
      flex: 0 0 auto;
    }

    .profile-heading { min-width: 0; }
    .profile-heading h1 {
      margin: 0;
      font-size: 1.7rem;
      line-height: 1.2;
      letter-spacing: 0;
    }
    .profile-heading p {
      margin: 5px 0 0;
      color: rgba(0, 0, 0, 0.62);
      overflow-wrap: anywhere;
    }

    .profile-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 16px;
    }

    .profile-card { border-radius: 8px; }
    .profile-card mat-card-content { padding: 16px !important; }
    .security-card,
    .access-card { grid-column: 1 / -1; }

    .detail-list {
      margin: 0;
      display: grid;
      gap: 14px;
    }
    .detail-list div { min-width: 0; }
    .detail-list dt {
      margin-bottom: 3px;
      color: rgba(0, 0, 0, 0.58);
      font-size: 0.78rem;
      text-transform: uppercase;
    }
    .detail-list dd {
      margin: 0;
      font-size: 0.96rem;
      overflow-wrap: anywhere;
    }

    .mono {
      font-family: Consolas, 'Courier New', monospace;
      font-size: 0.84rem !important;
    }

    .security-form,
    .sensitive-form {
      display: grid;
      grid-template-columns: minmax(180px, 1fr) auto;
      gap: 12px;
      align-items: start;
    }

    .security-form + .security-form {
      margin-top: 12px;
    }

    .security-form.compact {
      grid-template-columns: minmax(160px, 1fr) auto;
      margin-top: 16px;
    }

    .security-form button,
    .sensitive-form button {
      min-height: 56px;
      border-radius: 8px;
    }

    .is-hidden {
      display: none !important;
    }

    .setup-grid {
      display: grid;
      grid-template-columns: 180px minmax(0, 1fr);
      gap: 18px;
      margin-top: 16px;
      padding-top: 16px;
      border-top: 1px solid #e6ebf2;
    }

    .qr-box {
      width: 180px;
      height: 180px;
      display: grid;
      place-items: center;
      border: 1px solid #d9e2ef;
      border-radius: 8px;
      background: #ffffff;
    }
    .qr-box img {
      width: 160px;
      height: 160px;
    }

    .security-actions {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 16px;
    }

    .email-code-row {
      display: flex;
      justify-content: flex-end;
      margin-bottom: 12px;
    }

    .sensitive-form {
      grid-template-columns: 1fr;
      padding: 14px;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
    }
    .sensitive-form h2,
    .role-section h2 {
      margin: 0 0 10px;
      font-size: 0.95rem;
      font-weight: 600;
      letter-spacing: 0;
    }
    .sensitive-form.danger {
      border-color: #f1c7c7;
    }

    .role-section + .role-section {
      margin-top: 18px;
      padding-top: 18px;
      border-top: 1px solid #e6ebf2;
    }

    .muted {
      color: rgba(0, 0, 0, 0.56);
      font-size: 0.9rem;
      line-height: 32px;
    }

    .error-state {
      text-align: center;
      color: #b3261e;
      margin: 0;
      padding: 24px;
    }

    @media (max-width: 760px) {
      .page-container {
        padding: 20px 14px;
      }

      .profile-hero {
        align-items: flex-start;
        padding: 18px;
      }

      .profile-avatar {
        width: 56px;
        height: 56px;
        font-size: 0.95rem;
      }

      .profile-grid,
      .setup-grid,
      .security-actions,
      .security-form,
      .security-form.compact,
      .recovery-grid {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);

  profile: UserProfile | null = null;
  loading = true;
  actionLoading: 'enable' | 'enableEmail' | 'confirm' | 'disable' | 'sendEmailCode' | null = null;
  setup: TwoFactorSetupResponse | null = null;
  qrCodeDataUrl: string | null = null;

  setupPasswordForm = new FormGroup({
    password: new FormControl('', [Validators.required]),
  });

  emailPasswordForm = new FormGroup({
    password: new FormControl('', [Validators.required]),
  });

  confirmForm = new FormGroup({
    code: new FormControl('', [Validators.required]),
  });

  disableForm = new FormGroup({
    password: new FormControl('', [Validators.required]),
    code: new FormControl('', [Validators.required]),
  });

  get initials(): string {
    if (!this.profile)
      return '';

    return `${this.profile.firstName.charAt(0)}${this.profile.lastName.charAt(0)}`;
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  startTwoFactorSetup(): void {
    if (this.setupPasswordForm.invalid || this.actionLoading) return;

    this.actionLoading = 'enable';
    this.auth.enableTwoFactor({ password: this.setupPasswordForm.value.password ?? '' }).subscribe({
      next: async setup => {
        this.setup = setup;
        this.qrCodeDataUrl = await QRCode.toDataURL(setup.otpAuthUri, { margin: 1, width: 180 });
        this.actionLoading = null;
        this.setupPasswordForm.reset();
      },
      error: () => this.handleActionError('Unable to start 2FA setup'),
    });
  }

  confirmTwoFactor(): void {
    if (this.confirmForm.invalid || this.actionLoading) return;

    this.actionLoading = 'confirm';
    this.auth.confirmTwoFactor({ code: this.confirmForm.value.code ?? '' }).subscribe({
      next: () => {
        this.setup = null;
        this.qrCodeDataUrl = null;
        this.confirmForm.reset();
        this.actionLoading = null;
        this.loadProfile(false);
      },
      error: () => this.handleActionError('Unable to confirm 2FA'),
    });
  }

  enableEmailTwoFactor(): void {
    if (this.emailPasswordForm.invalid || this.actionLoading) return;

    this.actionLoading = 'enableEmail';
    this.auth.enableEmailTwoFactor({ password: this.emailPasswordForm.value.password ?? '' }).subscribe({
      next: () => {
        this.emailPasswordForm.reset();
        this.actionLoading = null;
        this.loadProfile(false);
      },
      error: () => this.handleActionError('Unable to enable email 2FA'),
    });
  }

  sendEmailCode(): void {
    if (this.actionLoading) return;

    this.actionLoading = 'sendEmailCode';
    this.auth.sendEmailTwoFactorCode().subscribe({
      next: () => {
        this.actionLoading = null;
        this.snackBar.open('Verification code sent', 'Close', { duration: 3000 });
      },
      error: () => this.handleActionError('Unable to send verification code'),
    });
  }

  disableTwoFactor(): void {
    if (this.disableForm.invalid || this.actionLoading) return;

    this.actionLoading = 'disable';
    this.auth.disableTwoFactor({
      password: this.disableForm.value.password ?? '',
      code: this.disableForm.value.code ?? '',
    }).subscribe({
      next: () => {
        this.disableForm.reset();
        this.actionLoading = null;
        this.loadProfile(false);
      },
      error: () => this.handleActionError('Unable to disable 2FA'),
    });
  }

  private loadProfile(showLoading = true): void {
    if (showLoading)
      this.loading = true;

    this.auth.getProfile().subscribe({
      next: profile => {
        this.profile = profile;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  private handleActionError(message: string): void {
    this.actionLoading = null;
    this.snackBar.open(message, 'Close', { duration: 4000 });
  }
}
