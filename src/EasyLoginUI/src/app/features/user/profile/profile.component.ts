import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AuthService } from '../../../core/services/auth.service';
import { UserProfile } from '../../../core/models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [MatCardModule, MatChipsModule, MatIconModule, MatProgressBarModule],
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
                <div>
                  <dt>First Name</dt>
                  <dd>{{ profile.firstName }}</dd>
                </div>
                <div>
                  <dt>Last Name</dt>
                  <dd>{{ profile.lastName }}</dd>
                </div>
                <div>
                  <dt>Email</dt>
                  <dd>{{ profile.email }}</dd>
                </div>
                <div>
                  <dt>User ID</dt>
                  <dd class="mono">{{ profile.id }}</dd>
                </div>
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
                <div>
                  <dt>Name</dt>
                  <dd>{{ profile.tenantName ?? 'No tenant assigned' }}</dd>
                </div>
                <div>
                  <dt>Tenant ID</dt>
                  <dd class="mono">{{ profile.tenantId ?? '-' }}</dd>
                </div>
              </dl>
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
      max-width: 980px;
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

    .profile-heading {
      min-width: 0;

      h1 {
        margin: 0;
        font-size: 1.7rem;
        line-height: 1.2;
        letter-spacing: 0;
      }

      p {
        margin: 5px 0 0;
        color: rgba(0, 0, 0, 0.62);
        overflow-wrap: anywhere;
      }
    }

    .profile-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 16px;
    }

    .profile-card {
      border-radius: 8px;

      mat-card-content {
        padding: 16px !important;
      }
    }

    .access-card {
      grid-column: 1 / -1;
    }

    .detail-list {
      margin: 0;
      display: grid;
      gap: 14px;

      div {
        min-width: 0;
      }

      dt {
        margin-bottom: 3px;
        color: rgba(0, 0, 0, 0.58);
        font-size: 0.78rem;
        text-transform: uppercase;
      }

      dd {
        margin: 0;
        font-size: 0.96rem;
        overflow-wrap: anywhere;
      }
    }

    .mono {
      font-family: Consolas, 'Courier New', monospace;
      font-size: 0.84rem !important;
    }

    .role-section + .role-section {
      margin-top: 18px;
      padding-top: 18px;
      border-top: 1px solid #e6ebf2;
    }

    .role-section h2 {
      margin: 0 0 10px;
      font-size: 0.95rem;
      font-weight: 600;
      letter-spacing: 0;
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

    @media (max-width: 720px) {
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

      .profile-grid {
        grid-template-columns: 1fr;
      }
    }
  `],
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);

  profile: UserProfile | null = null;
  loading = true;

  get initials(): string {
    if (!this.profile)
      return '';

    return `${this.profile.firstName.charAt(0)}${this.profile.lastName.charAt(0)}`;
  }

  ngOnInit(): void {
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
}
