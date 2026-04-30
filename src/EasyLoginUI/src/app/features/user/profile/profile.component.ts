import { Component, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [AsyncPipe, MatCardModule, MatChipsModule],
  template: `
    @if (auth.currentUser$ | async; as user) {
      <div class="page-container">
        <mat-card>
          <mat-card-header>
            <mat-card-title>My Profile</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <p><strong>First name:</strong> {{ user.firstName }}</p>
            <p><strong>Last name:</strong> {{ user.lastName }}</p>
            <p><strong>Email:</strong> {{ user.email }}</p>
            <p><strong>Roles:</strong></p>
            <mat-chip-set>
              @for (role of user.roles; track role) {
                <mat-chip>{{ role }}</mat-chip>
              }
            </mat-chip-set>
          </mat-card-content>
        </mat-card>
      </div>
    }
  `,
  styles: [`
    .page-container {
      padding: 24px;
      max-width: 600px;
      margin: 0 auto;
    }
    p { margin: 8px 0; }
  `],
})
export class ProfileComponent {
  readonly auth = inject(AuthService);
}
