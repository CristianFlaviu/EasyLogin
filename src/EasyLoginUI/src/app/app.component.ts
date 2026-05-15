import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';

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
export class AppComponent implements OnInit {
  readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
    this.authService.currentUser$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(user => {
        if (user) {
          this.notifications.init();
        } else {
          this.notifications.stop();
        }
      });
  }
}
