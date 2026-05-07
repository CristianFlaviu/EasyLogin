import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  template: `
    <section class="confirm-shell">
      @if (loading) {
        <mat-spinner diameter="32" />
        <h1>Confirming email</h1>
      } @else if (success) {
        <mat-icon class="state-icon success">verified</mat-icon>
        <h1>Email confirmed</h1>
        <p>You can now sign in to EasyLogin.</p>
        <a mat-flat-button color="primary" routerLink="/login">Sign in</a>
      } @else {
        <mat-icon class="state-icon error">error</mat-icon>
        <h1>Confirmation failed</h1>
        <p>The confirmation link is invalid or expired.</p>
        <a mat-stroked-button color="primary" routerLink="/login">Back to sign in</a>
      }
    </section>
  `,
  styles: [`
    .confirm-shell {
      min-height: 100vh;
      display: grid;
      place-items: center;
      align-content: center;
      gap: 12px;
      padding: 24px;
      text-align: center;
      background: #f6f8fb;
    }

    h1 {
      margin: 0;
      font-size: 1.5rem;
      letter-spacing: 0;
    }

    p {
      margin: 0 0 8px;
      color: rgba(0, 0, 0, 0.62);
    }

    .state-icon {
      width: 44px;
      height: 44px;
      font-size: 44px;
    }

    .success { color: #1b7f3a; }
    .error { color: #b3261e; }
  `],
})
export class ConfirmEmailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  loading = true;
  success = false;

  ngOnInit(): void {
    const email = this.route.snapshot.queryParamMap.get('email');
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!email || !token) {
      this.loading = false;
      this.success = false;
      return;
    }

    this.auth.confirmEmail({ email, token }).subscribe({
      next: () => {
        this.loading = false;
        this.success = true;
      },
      error: () => {
        this.loading = false;
        this.success = false;
      },
    });
  }
}
