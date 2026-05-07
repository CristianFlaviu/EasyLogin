import { DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TenantItem } from '../../../core/models/tenant.model';
import { UserListItem } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';
import { TenantAdminService } from '../../../core/services/tenant-admin.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { TenantInviteUserDialogComponent } from '../invite-user-dialog/tenant-invite-user-dialog.component';

@Component({
  selector: 'app-tenant-user-list',
  standalone: true,
  imports: [
    DatePipe,
    MatTableModule, MatPaginatorModule, MatProgressBarModule,
    MatChipsModule, MatCardModule, MatButtonModule, MatIconModule, MatTooltipModule,
  ],
  templateUrl: './tenant-user-list.component.html',
  styleUrl: './tenant-user-list.component.scss',
})
export class TenantUserListComponent implements OnInit {
  private readonly tenantAdmin = inject(TenantAdminService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  displayedColumns = ['email', 'role', 'status', 'invitedAt', 'actions'];
  users: UserListItem[] = [];
  tenantContext: TenantItem | null = null;
  totalCount = 0;
  pageSize = 20;
  pageIndex = 0;
  loading = false;
  contextLoading = true;

  get currentUserId(): string | undefined {
    return this.auth.currentUser$.value?.id;
  }

  get invitesEnabled(): boolean {
    return this.tenantContext?.isActive ?? true;
  }

  get showSuspendedWarning(): boolean {
    return !this.contextLoading && !this.invitesEnabled;
  }

  ngOnInit(): void {
    this.tenantAdmin.getContext().subscribe({
      next: context => {
        this.tenantContext = context;
        this.contextLoading = false;
      },
      error: () => {
        this.contextLoading = false;
      },
    });
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.tenantAdmin.getUsers(this.pageIndex + 1, this.pageSize).subscribe({
      next: data => {
        this.users = data.items;
        this.totalCount = data.totalCount;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  openInviteDialog(): void {
    if (!this.invitesEnabled) {
      this.snackBar.open('Your organization is suspended. Invites are disabled.', 'Close', { duration: 5000 });
      return;
    }

    const ref = this.dialog.open(TenantInviteUserDialogComponent, { width: '460px' });
    ref.afterClosed().subscribe(result => {
      if (!result) return;
      this.snackBar.open('Invite sent.', 'Close', { duration: 3000 });
      this.loadUsers();
    });
  }

  resendInvite(user: UserListItem): void {
    if (!this.invitesEnabled) {
      this.snackBar.open('Your organization is suspended. Invites are disabled.', 'Close', { duration: 5000 });
      return;
    }

    this.tenantAdmin.resendInvite(user.id).subscribe({
      next: () => this.snackBar.open('Invite resent.', 'Close', { duration: 3000 }),
      error: err => {
        const msg = err.error?.message ?? 'Could not resend invite.';
        this.snackBar.open(msg, 'Close', { duration: 4000 });
      },
    });
  }

  revokeInvite(user: UserListItem): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Revoke Invite',
        message: `Revoke active invite links for ${user.email}?`,
      },
    });

    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.tenantAdmin.revokeInvite(user.id).subscribe({
        next: () => this.snackBar.open('Invite revoked.', 'Close', { duration: 3000 }),
        error: err => {
          const msg = err.error?.message ?? 'Could not revoke invite.';
          this.snackBar.open(msg, 'Close', { duration: 4000 });
        },
      });
    });
  }

  suspendUser(user: UserListItem): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Suspend User',
        message: `Suspend ${user.email}? They will no longer be able to sign in.`,
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.tenantAdmin.suspendUser(user.id).subscribe({
        next: () => {
          this.snackBar.open('User suspended.', 'Close', { duration: 3000 });
          this.loadUsers();
        },
        error: err => {
          const msg = err.error?.message ?? 'Suspend failed.';
          this.snackBar.open(msg, 'Close', { duration: 4000 });
        },
      });
    });
  }

  roleText(user: UserListItem): string {
    return user.tenantRoles[0] ?? '-';
  }

  statusClass(user: UserListItem): string {
    switch (user.status) {
      case 'Active':
        return 'chip-active';
      case 'Pending':
        return 'chip-pending';
      case 'Suspended':
        return 'chip-suspended';
      case 'Expired':
        return 'chip-expired';
      default:
        return 'chip-expired';
    }
  }

  statusText(user: UserListItem): string {
    return user.status;
  }
}
