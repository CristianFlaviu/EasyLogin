import { Component, inject } from '@angular/core';
import { AsyncPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../core/services/auth.service';
import { HasRoleDirective } from '../../shared/directives/has-role.directive';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [AsyncPipe, RouterLink, MatCardModule, MatIconModule, MatButtonModule, HasRoleDirective],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {
  readonly auth = inject(AuthService);

  readonly stats = [
    { icon: 'people', label: 'Total Users', value: '—', color: '#4f46e5' },
    { icon: 'verified_user', label: 'Active Sessions', value: '—', color: '#0284c7' },
    { icon: 'login', label: 'Logins (24h)', value: '—', color: '#059669' },
    { icon: 'cloud_done', label: 'API Status', value: 'Online', color: '#d97706' },
  ];
}
