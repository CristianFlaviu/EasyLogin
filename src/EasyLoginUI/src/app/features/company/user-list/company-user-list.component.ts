import { Component, inject, OnInit } from '@angular/core';
import { PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CompanyAdminService } from '../../../core/services/company-admin.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserListItem } from '../../../core/models/user.model';
import { CompanyRoleItem } from '../../../core/models/company.model';
import { UserDialogComponent } from '../../admin/user-dialog/user-dialog.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-company-user-list',
  standalone: true,
  imports: [
    MatTableModule, MatPaginatorModule, MatProgressBarModule,
    MatChipsModule, MatCardModule, MatButtonModule, MatIconModule, MatTooltipModule,
  ],
  templateUrl: './company-user-list.component.html',
  styleUrl: './company-user-list.component.scss',
})
export class CompanyUserListComponent implements OnInit {
  private readonly companyAdmin = inject(CompanyAdminService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  displayedColumns = ['name', 'email', 'companyRoles', 'status', 'actions'];
  users: UserListItem[] = [];
  companyRoles: CompanyRoleItem[] = [];
  totalCount = 0;
  pageSize = 20;
  pageIndex = 0;
  loading = false;

  get currentUserId(): string | undefined {
    return this.auth.currentUser$.value?.id;
  }

  ngOnInit(): void {
    this.companyAdmin.getRoles().subscribe({ next: r => (this.companyRoles = r) });
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.companyAdmin.getUsers(this.pageIndex + 1, this.pageSize).subscribe({
      next: data => { this.users = data.items; this.totalCount = data.totalCount; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(UserDialogComponent, {
      data: { user: null, mode: 'companyadmin', companyRoles: this.companyRoles },
      width: '540px',
    });
    ref.afterClosed().subscribe(result => { if (result) this.loadUsers(); });
  }

  openEditDialog(user: UserListItem): void {
    this.companyAdmin.getUser(user.id).subscribe({
      next: detail => {
        const ref = this.dialog.open(UserDialogComponent, {
          data: { user: detail, mode: 'companyadmin', companyRoles: this.companyRoles },
          width: '540px',
        });
        ref.afterClosed().subscribe(result => { if (result) this.loadUsers(); });
      },
      error: () => this.snackBar.open('Failed to load user details.', 'Close', { duration: 3000 }),
    });
  }

  deleteUser(user: UserListItem): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete User',
        message: `Delete ${user.firstName} ${user.lastName} (${user.email})? This cannot be undone.`,
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.companyAdmin.deleteUser(user.id).subscribe({
        next: () => {
          this.snackBar.open('User deleted.', 'Close', { duration: 3000 });
          this.loadUsers();
        },
        error: err => {
          const msg = err.error?.detail ?? 'Delete failed.';
          this.snackBar.open(msg, 'Close', { duration: 4000 });
        },
      });
    });
  }
}
