import { Component, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CompanyAdminService } from '../../../core/services/company-admin.service';
import { CompanyRoleItem } from '../../../core/models/company.model';
import { CompanyRoleDialogComponent } from '../role-dialog/company-role-dialog.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-company-role-list',
  standalone: true,
  imports: [
    DatePipe,
    MatTableModule, MatCardModule, MatButtonModule, MatIconModule,
    MatTooltipModule, MatProgressBarModule,
  ],
  templateUrl: './company-role-list.component.html',
  styleUrl: './company-role-list.component.scss',
})
export class CompanyRoleListComponent implements OnInit {
  private readonly companyAdmin = inject(CompanyAdminService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  displayedColumns = ['name', 'description', 'createdAt', 'actions'];
  roles: CompanyRoleItem[] = [];
  loading = false;

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.loading = true;
    this.companyAdmin.getRoles().subscribe({
      next: roles => { this.roles = roles; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CompanyRoleDialogComponent, { width: '420px' });
    ref.afterClosed().subscribe(result => { if (result) this.loadRoles(); });
  }

  deleteRole(role: CompanyRoleItem): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Role',
        message: `Delete role "${role.name}"? Users assigned this role will lose it.`,
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.companyAdmin.deleteRole(role.id).subscribe({
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
