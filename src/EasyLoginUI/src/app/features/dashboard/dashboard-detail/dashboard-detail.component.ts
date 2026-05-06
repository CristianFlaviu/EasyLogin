import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { Observable } from 'rxjs';
import {
  OverviewActiveSessionItem,
  OverviewLoginItem,
  PaginatedList,
  UserListItem,
} from '../../../core/models/user.model';
import { AdminService } from '../../../core/services/admin.service';
import { AuthService } from '../../../core/services/auth.service';
import { TenantAdminService } from '../../../core/services/tenant-admin.service';

type DashboardMetric = 'users' | 'logins' | 'sessions';

@Component({
  selector: 'app-dashboard-detail',
  standalone: true,
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTableModule,
  ],
  templateUrl: './dashboard-detail.component.html',
  styleUrl: './dashboard-detail.component.scss',
})
export class DashboardDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);
  private readonly admin = inject(AdminService);
  private readonly tenantAdmin = inject(TenantAdminService);

  metric: DashboardMetric = 'users';
  title = 'Total Users';
  subtitle = 'Global overview';
  error: string | null = null;
  loading = false;

  users: UserListItem[] = [];
  logins: OverviewLoginItem[] = [];
  sessions: OverviewActiveSessionItem[] = [];

  displayedColumns: string[] = [];
  totalCount = 0;
  pageSize = 20;
  pageIndex = 0;

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.metric = this.parseMetric(params.get('metric'));
      this.pageIndex = 0;
      this.configureView();
      this.loadRows();
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadRows();
  }

  roleText(user: UserListItem): string {
    const roles = [...user.roles, ...user.tenantRoles];
    return roles.length > 0 ? roles.join(', ') : '-';
  }

  sessionName(session: OverviewActiveSessionItem): string {
    return `${session.firstName} ${session.lastName}`.trim();
  }

  private parseMetric(metric: string | null): DashboardMetric {
    if (metric === 'logins' || metric === 'sessions')
      return metric;

    return 'users';
  }

  private configureView(): void {
    this.subtitle = this.auth.hasRole('SuperAdmin') ? 'Global overview' : 'Tenant overview';

    switch (this.metric) {
      case 'logins':
        this.title = 'Logins Last 24 Hours';
        this.displayedColumns = ['timestamp', 'email', 'ipAddress', 'browser', 'device'];
        break;
      case 'sessions':
        this.title = 'Active Sessions';
        this.displayedColumns = ['name', 'email', 'tenant', 'expiresAt'];
        break;
      default:
        this.title = 'Total Users';
        this.displayedColumns = ['name', 'email', 'tenant', 'roles', 'status'];
        break;
    }
  }

  private loadRows(): void {
    if (!this.auth.hasRole('SuperAdmin') && !this.auth.hasRole('TenantAdmin')) {
      this.error = 'Overview details are available to administrators only.';
      this.clearRows();
      return;
    }

    this.loading = true;
    this.error = null;

    switch (this.metric) {
      case 'logins':
        this.loadLogins();
        break;
      case 'sessions':
        this.loadSessions();
        break;
      default:
        this.loadUsers();
        break;
    }
  }

  private loadUsers(): void {
    this.getUserSource().subscribe({
      next: data => {
        this.users = data.items;
        this.totalCount = data.totalCount;
        this.loading = false;
      },
      error: () => this.handleLoadError(),
    });
  }

  private loadLogins(): void {
    this.getLoginSource().subscribe({
      next: data => {
        this.logins = data.items;
        this.totalCount = data.totalCount;
        this.loading = false;
      },
      error: () => this.handleLoadError(),
    });
  }

  private loadSessions(): void {
    this.getSessionSource().subscribe({
      next: data => {
        this.sessions = data.items;
        this.totalCount = data.totalCount;
        this.loading = false;
      },
      error: () => this.handleLoadError(),
    });
  }

  private getUserSource(): Observable<PaginatedList<UserListItem>> {
    if (this.auth.hasRole('SuperAdmin'))
      return this.admin.getUsers(this.pageIndex + 1, this.pageSize);

    return this.tenantAdmin.getUsers(this.pageIndex + 1, this.pageSize);
  }

  private getLoginSource(): Observable<PaginatedList<OverviewLoginItem>> {
    if (this.auth.hasRole('SuperAdmin'))
      return this.admin.getOverviewLogins(this.pageIndex + 1, this.pageSize);

    return this.tenantAdmin.getOverviewLogins(this.pageIndex + 1, this.pageSize);
  }

  private getSessionSource(): Observable<PaginatedList<OverviewActiveSessionItem>> {
    if (this.auth.hasRole('SuperAdmin'))
      return this.admin.getOverviewActiveSessions(this.pageIndex + 1, this.pageSize);

    return this.tenantAdmin.getOverviewActiveSessions(this.pageIndex + 1, this.pageSize);
  }

  private handleLoadError(): void {
    this.loading = false;
    this.error = 'Unable to load details.';
    this.clearRows();
  }

  private clearRows(): void {
    this.users = [];
    this.logins = [];
    this.sessions = [];
    this.totalCount = 0;
  }
}
