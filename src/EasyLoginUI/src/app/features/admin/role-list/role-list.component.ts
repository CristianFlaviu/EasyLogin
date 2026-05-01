import { Component, inject, OnInit } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DatePipe } from '@angular/common';
import { AdminService } from '../../../core/services/admin.service';
import { RoleItem } from '../../../core/models/user.model';
import { RoleDialogComponent } from '../role-dialog/role-dialog.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-role-list',
  standalone: true,
  imports: [
    MatTableModule, MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatTooltipModule, MatProgressBarModule, DatePipe,
  ],
  templateUrl: './role-list.component.html',
  styleUrl: './role-list.component.scss',
})
export class RoleListComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  displayedColumns = ['name', 'description', 'type', 'createdAt', 'actions'];
  roles: RoleItem[] = [];
  loading = false;

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.loading = true;
    this.admin.getRoles().subscribe({
      next: roles => {
        this.roles = roles;
        this.loading = false;
      },
      error: () => { this.loading = false; },
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(RoleDialogComponent, { width: '420px' });
    ref.afterClosed().subscribe(result => {
      if (result) this.loadRoles();
    });
  }

  deleteRole(role: RoleItem): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Role',
        message: `Delete role "${role.name}"? Users currently assigned this role will lose it.`,
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.admin.deleteRole(role.id).subscribe({
        next: () => {
          this.snackBar.open('Role deleted.', 'Close', { duration: 3000 });
          this.loadRoles();
        },
        error: err => {
          const msg = err.error?.detail ?? 'Delete failed.';
          this.snackBar.open(msg, 'Close', { duration: 4000 });
        },
      });
    });
  }
}
