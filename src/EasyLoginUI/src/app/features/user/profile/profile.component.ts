import { Component, inject, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
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
              <mat-card-subtitle>Multi-factor authentication</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <div class="security-summary" [class.enabled]="profile.twoFactorEnabled">
                <div class="security-state-icon">
                  <mat-icon>{{ profile.twoFactorEnabled ? 'verified_user' : 'gpp_maybe' }}</mat-icon>
                </div>
                <div class="security-state-copy">
                  <span>Two-factor authentication </span>
                  <strong>{{ profile.twoFactorEnabled ? 'Enabled' : 'Disabled' }}</strong>
                </div>
                <mat-chip-set class="security-badges">
                  <mat-chip>{{ profile.twoFactorEnabled ? 'Active' : 'Inactive' }}</mat-chip>
                  <mat-chip>{{ twoFactorMethodLabel }}</mat-chip>
                </mat-chip-set>
              </div>

              @if (!profile.twoFactorEnabled) {
                <div class="security-option-grid">
                  <form [formGroup]="setupPasswordForm" (ngSubmit)="startTwoFactorSetup()" class="security-option primary-option">
                    <div class="option-heading">
                      <div class="option-icon"><mat-icon>qr_code_2</mat-icon></div>
                      <div>
                        <h2>Authenticator app</h2>
                        <span>Recommended</span>
                      </div>
                    </div>
                    <div class="option-controls">
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
                        <mat-icon [class.is-hidden]="actionLoading === 'enable'">qr_code_scanner</mat-icon>
                        <span [class.is-hidden]="actionLoading === 'enable'">Set up</span>
                      </button>
                    </div>
                  </form>

                  <form [formGroup]="emailPasswordForm" (ngSubmit)="enableEmailTwoFactor()" class="security-option">
                    <div class="option-heading">
                      <div class="option-icon"><mat-icon>mail_lock</mat-icon></div>
                      <div>
                        <h2>Email codes</h2>
                        <span>{{ profile.emailConfirmed ? 'Available' : 'Email unconfirmed' }}</span>
                      </div>
                    </div>
                    <div class="option-controls">
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
                        <span [class.is-hidden]="actionLoading === 'enableEmail'">Enable</span>
                      </button>
                    </div>
                  </form>
                </div>

                @if (setup) {
                  <div class="setup-panel">
                    <div class="setup-preview">
                      <div class="qr-box">
                        @if (qrCodeDataUrl) {
                          <img [src]="qrCodeDataUrl" alt="Authenticator QR code">
                        }
                      </div>
                      <div>
                        <span class="field-label">Manual secret</span>
                        <code>{{ setup.sharedSecret }}</code>
                      </div>
                    </div>
                    <form [formGroup]="confirmForm" (ngSubmit)="confirmTwoFactor()" class="confirm-panel">
                      <div class="option-heading">
                        <div class="option-icon success"><mat-icon>verified_user</mat-icon></div>
                        <div>
                          <h2>Confirm setup</h2>
                          <span>Authenticator code</span>
                        </div>
                      </div>
                      <div class="option-controls">
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
                          <mat-icon [class.is-hidden]="actionLoading === 'confirm'">check_circle</mat-icon>
                          <span [class.is-hidden]="actionLoading === 'confirm'">Confirm</span>
                        </button>
                      </div>
                    </form>
                  </div>
                }
              } @else {
                <div class="security-enabled-layout">
                  <div class="active-method-panel">
                    <div class="option-heading">
                      <div class="option-icon success">
                        <mat-icon>{{ profile.twoFactorMethod === 'Email' ? 'mail_lock' : 'app_registration' }}</mat-icon>
                      </div>
                      <div>
                        <h2>{{ twoFactorMethodLabel }}</h2>
                        <span>Verification method</span>
                      </div>
                    </div>
                    @if (profile.twoFactorMethod === 'Email') {
                      <button mat-stroked-button color="primary" type="button"
                              [disabled]="actionLoading === 'sendEmailCode'"
                              (click)="sendEmailCode()">
                        @if (actionLoading === 'sendEmailCode') {
                          <mat-spinner diameter="18" />
                        }
                        <mat-icon [class.is-hidden]="actionLoading === 'sendEmailCode'">mail</mat-icon>
                        <span [class.is-hidden]="actionLoading === 'sendEmailCode'">Send code</span>
                      </button>
                    }
                  </div>

                  <form [formGroup]="disableForm" (ngSubmit)="disableTwoFactor()" class="danger-zone">
                    <div class="danger-heading">
                      <div class="option-icon danger"><mat-icon>lock_open</mat-icon></div>
                      <div>
                        <h2>Disable 2FA</h2>
                        <span>{{ disableFailureCount > 0 ? disableAttemptsRemaining + ' attempts remaining' : 'Password and code required' }}</span>
                      </div>
                    </div>
                    @if (disableFailureCount > 0) {
                      <div class="retry-banner">
                        <mat-icon>error</mat-icon>
                        <span>Invalid password or code. {{ disableAttemptsRemaining }} attempt{{ disableAttemptsRemaining === 1 ? '' : 's' }} remaining.</span>
                      </div>
                    }
                    <div class="sensitive-grid">
                      <mat-form-field appearance="outline">
                        <mat-label>Current password</mat-label>
                        <input matInput formControlName="password" type="password" autocomplete="current-password">
                      </mat-form-field>
                      <mat-form-field appearance="outline">
                        <mat-label>{{ profile.twoFactorMethod === 'Email' ? 'Email code' : 'Authenticator code' }}</mat-label>
                        <input matInput formControlName="code" inputmode="numeric" autocomplete="one-time-code">
                      </mat-form-field>
                    </div>
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

    .is-hidden {
      display: none !important;
    }

    .security-summary {
      display: grid;
      grid-template-columns: auto minmax(0, 1fr) auto;
      align-items: center;
      gap: 14px;
      padding: 14px;
      border: 1px solid #dbe4f0;
      border-radius: 8px;
      background: #f8fafc;
      margin-bottom: 16px;
    }

    .security-state-icon,
    .option-icon {
      color: #1565c0;
      flex: 0 0 auto;
    }

    .option-icon.success { color: #137333; }

    .security-badges {
      display: flex;
      flex-wrap: wrap;
      justify-content: flex-end;
      gap: 8px;
    }

    .security-option-grid,
    .security-enabled-layout {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 14px;
    }

    .security-option,
    .active-method-panel,
    .danger-zone,
    .confirm-panel {
      display: grid;
      gap: 14px;
      padding: 16px;
      border: 1px solid #dde5ef;
      border-radius: 8px;
      background: #ffffff;
    }

    .security-option.primary-option {
      border-color: #b8cdeb;
      background: #f8fbff;
    }

    .danger-zone {
      border-color: #f0c9c9;
      background: #fffafa;
    }

    .option-icon.danger {
      color: #b3261e;
    }

    .option-heading,
    .danger-heading {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .option-heading h2,
    .danger-heading h2,
    .role-section h2 {
      margin: 0 0 3px;
      font-size: 0.98rem;
      font-weight: 650;
      line-height: 1.25;
      letter-spacing: 0;
    }

    .option-controls {
      display: grid;
      grid-template-columns: minmax(0, 1fr) auto;
      gap: 12px;
      align-items: start;
    }

    .option-controls button,
    .active-method-panel button,
    .danger-zone button {
      min-height: 56px;
      border-radius: 8px;
      white-space: nowrap;
    }

    .setup-panel {
      display: grid;
      grid-template-columns: 220px minmax(0, 1fr);
      gap: 18px;
      margin-top: 16px;
      padding: 16px;
      border: 1px solid #dbe4f0;
      border-radius: 8px;
      background: #fbfcfe;
    }

    .setup-preview {
      display: grid;
      gap: 12px;
    }

    .qr-box {
      width: 100%;
      aspect-ratio: 1;
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

    .active-method-panel {
      grid-template-columns: minmax(0, 1fr) auto;
      align-items: center;
    }

    .sensitive-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 12px;
    }

    .retry-banner {
      display: flex;
      align-items: center;
      gap: 8px;
      min-height: 36px;
      padding: 8px 10px;
      border: 1px solid #f0c9c9;
      border-radius: 8px;
      background: #fff4f4;
      color: #9b1c16;
      font-size: 0.86rem;
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
      .security-summary,
      .security-option-grid,
      .security-enabled-layout,
      .setup-panel,
      .option-controls,
      .active-method-panel,
      .sensitive-grid {
        grid-template-columns: 1fr;
      }

      .security-badges {
        justify-content: flex-start;
      }

      .qr-box {
        max-width: 180px;
      }

      .option-controls button,
      .active-method-panel button,
      .danger-zone button {
        width: 100%;
      }
    }
  `],
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly maxDisableFailures = 3;

  profile: UserProfile | null = null;
  loading = true;
  actionLoading: 'enable' | 'enableEmail' | 'confirm' | 'disable' | 'sendEmailCode' | null = null;
  setup: TwoFactorSetupResponse | null = null;
  qrCodeDataUrl: string | null = null;
  disableFailureCount = 0;

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

  get twoFactorMethodLabel(): string {
    if (!this.profile?.twoFactorEnabled)
      return 'No method';

    if (!this.profile.twoFactorMethod)
      return 'Authenticator app';

    return this.profile.twoFactorMethod === 'Email' ? 'Email codes' : 'Authenticator app';
  }

  get disableAttemptsRemaining(): number {
    return Math.max(0, this.maxDisableFailures - this.disableFailureCount);
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
        this.disableFailureCount = 0;
        this.actionLoading = null;
        this.loadProfile(false);
      },
      error: error => this.handleDisableError(error),
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

  private handleDisableError(error: unknown): void {
    this.actionLoading = null;

    if (error instanceof HttpErrorResponse && error.error?.code === 'TwoFactorVerificationFailed') {
      this.disableFailureCount += 1;
      const attemptsRemaining = this.maxDisableFailures - this.disableFailureCount;

      if (attemptsRemaining <= 0) {
        this.snackBar.open('Too many failed attempts. Please sign in again.', 'Close', { duration: 4000 });
        this.auth.logout();
        return;
      }

      this.snackBar.open(`Invalid password or code. ${attemptsRemaining} attempt${attemptsRemaining === 1 ? '' : 's'} remaining.`, 'Close', { duration: 4000 });
      return;
    }

    if (error instanceof HttpErrorResponse && error.error?.code === 'TwoFactorLockedOut') {
      this.snackBar.open('Too many failed attempts. Please sign in again.', 'Close', { duration: 4000 });
      this.auth.logout();
      return;
    }

    this.snackBar.open('Unable to disable 2FA', 'Close', { duration: 4000 });
  }
}
