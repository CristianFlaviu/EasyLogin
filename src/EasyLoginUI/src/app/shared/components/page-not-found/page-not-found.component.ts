import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-page-not-found',
  standalone: true,
  imports: [RouterLink, MatButtonModule],
  template: `
    <div class="center-page">
      <h1>Page not found</h1>
      <p>The page you are looking for does not exist.</p>
      @if (auth.isAuthenticated()) {
        <a mat-flat-button routerLink="/dashboard">Go to Dashboard</a>
      } @else {
        <a mat-flat-button routerLink="/login">Go to Login</a>
      }
    </div>
  `,
  styles: [`
    .center-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 80vh;
      gap: 12px;
      text-align: center;
    }
    h1 { margin: 0; }
    p { margin: 0; opacity: 0.7; }
  `],
})
export class PageNotFoundComponent {
  readonly auth = inject(AuthService);
}
