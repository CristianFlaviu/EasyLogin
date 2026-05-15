import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AsyncPipe, DatePipe } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationItem } from '../../../core/models/notification.model';
import { NotificationService } from '../../../core/services/notification.service';
import { HasRoleDirective } from '../../directives/has-role.directive';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    AsyncPipe, DatePipe, RouterLink, RouterLinkActive,
    MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule, MatTooltipModule,
    MatBadgeModule, MatDividerModule, MatListModule,
    HasRoleDirective,
  ],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.scss',
})
export class NavbarComponent {
  readonly auth = inject(AuthService);
  readonly notifications = inject(NotificationService);

  onNotifClick(notification: NotificationItem): void {
    if (!notification.isRead) {
      this.notifications.markRead(notification.id);
    }
  }
}
