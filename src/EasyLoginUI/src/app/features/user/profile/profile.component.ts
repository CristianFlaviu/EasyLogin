import { Component, inject, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AuthService } from '../../../core/services/auth.service';
import { UserProfile } from '../../../core/models/user.model';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [MatCardModule, MatChipsModule, MatProgressBarModule],
  template: `
    <div class="page-container">
      <mat-card>
        <mat-card-header>
          <mat-card-title>My Profile</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          @if (loading) {
            <mat-progress-bar mode="indeterminate" />
          } @else if (profile) {
            <p><strong>First name:</strong> {{ profile.firstName }}</p>
            <p><strong>Last name:</strong> {{ profile.lastName }}</p>
            <p><strong>Email:</strong> {{ profile.email }}</p>
            @if (profile.companyName) {
              <p><strong>Company:</strong> {{ profile.companyName }}</p>
            }
            <p><strong>System roles:</strong></p>
            <mat-chip-set>
              @for (role of profile.roles; track role) {
                <mat-chip>{{ role }}</mat-chip>
              }
              @if (profile.roles.length === 0) {
                <span class="muted">None</span>
              }
            </mat-chip-set>
            @if (profile.companyRoles.length > 0) {
              <p style="margin-top:12px"><strong>Company roles:</strong></p>
              <mat-chip-set>
                @for (role of profile.companyRoles; track role) {
                  <mat-chip>{{ role }}</mat-chip>
                }
              </mat-chip-set>
            }
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .page-container { padding: 24px; max-width: 600px; margin: 0 auto; }
    p { margin: 8px 0; }
    .muted { color: var(--mat-sys-on-surface-variant); font-size: 14px; }
  `],
})
export class ProfileComponent implements OnInit {
  private readonly auth = inject(AuthService);

  profile: UserProfile | null = null;
  loading = true;

  ngOnInit(): void {
    this.auth.getProfile().subscribe({
      next: p => { this.profile = p; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }
}
