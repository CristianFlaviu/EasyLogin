import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-unauthorized',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="center-page">
      <mat-icon class="big-icon">lock</mat-icon>
      <h1>Access denied</h1>
      <p>You do not have permission to view this page.</p>
      <a mat-flat-button routerLink="/dashboard">Go to Dashboard</a>
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
    .big-icon { font-size: 64px; width: 64px; height: 64px; opacity: 0.4; }
    h1 { margin: 0; }
    p { margin: 0; opacity: 0.7; }
  `],
})
export class UnauthorizedComponent {}
