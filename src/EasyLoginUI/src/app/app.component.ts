import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavbarComponent],
  template: `
    @if (authService.isAuthenticated()) {
      <app-navbar />
    }
    <router-outlet />
  `,
  styleUrl: './app.component.scss',
})
export class AppComponent {
  readonly authService = inject(AuthService);
}
