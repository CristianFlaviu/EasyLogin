import { Component, OnInit, inject } from '@angular/core';
import { AsyncPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { Observable } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AdminService } from '../../core/services/admin.service';
import { TenantAdminService } from '../../core/services/tenant-admin.service';
import { OverviewResponse } from '../../core/models/user.model';
import { HasRoleDirective } from '../../shared/directives/has-role.directive';

interface DashboardStat {
  icon: string;
  label: string;
  value: string;
  color: string;
  route: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [AsyncPipe, DatePipe, RouterLink, MatCardModule, MatIconModule, MatButtonModule, HasRoleDirective],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly admin = inject(AdminService);
  private readonly tenantAdmin = inject(TenantAdminService);

  readonly stats: DashboardStat[] = [
    { icon: 'people', label: 'Total Users', value: '-', color: '#4f46e5', route: '/dashboard/users' },
    { icon: 'login', label: 'Logins Last 24 Hours', value: '-', color: '#059669', route: '/dashboard/logins' },
    { icon: 'verified_user', label: 'Active Sessions', value: '-', color: '#0284c7', route: '/dashboard/sessions' },
  ];

  overviewScope = 'Dashboard';
  overviewError: string | null = null;
  showOverview = false;
  loadingOverview = false;
  updatedAt: Date | null = null;

  ngOnInit(): void {
    if (this.auth.hasRole('SuperAdmin')) {
      this.showOverview = true;
      this.overviewScope = 'Global overview';
      this.loadOverview(this.admin.getOverview());
      return;
    }

    if (this.auth.hasRole('TenantAdmin')) {
      this.showOverview = true;
      this.overviewScope = 'Tenant overview';
      this.loadOverview(this.tenantAdmin.getOverview());
    }
  }

  private loadOverview(source: Observable<OverviewResponse>): void {
    this.loadingOverview = true;
    source.subscribe({
      next: overview => {
        this.applyOverview(overview);
        this.loadingOverview = false;
      },
      error: () => {
        this.loadingOverview = false;
        this.overviewError = 'Unable to load overview.';
      },
    });
  }

  private applyOverview(overview: OverviewResponse): void {
    this.overviewError = null;
    this.updatedAt = new Date();
    this.stats[0].value = this.formatCount(overview.totalUsers);
    this.stats[1].value = this.formatCount(overview.loginsLast24Hours);
    this.stats[2].value = this.formatCount(overview.activeSessions);
  }

  private formatCount(value: number): string {
    return new Intl.NumberFormat().format(value);
  }
}
