import { Component, inject, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminService } from '../../../core/services/admin.service';
import { TenantItem } from '../../../core/models/tenant.model';
import { TenantDialogComponent } from '../tenant-dialog/tenant-dialog.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-tenant-list',
  standalone: true,
  imports: [
    DatePipe,
    MatTableModule, MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatTooltipModule, MatProgressBarModule,
  ],
  templateUrl: './tenant-list.component.html',
  styleUrl: './tenant-list.component.scss',
})
export class TenantListComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  displayedColumns = ['name', 'status', 'createdAt', 'actions'];
  tenants: TenantItem[] = [];
  loading = false;

  ngOnInit(): void {
    this.loadTenants();
  }

  loadTenants(): void {
    this.loading = true;
    this.admin.getTenants().subscribe({
      next: data => { this.tenants = data; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(TenantDialogComponent, {
      data: { tenant: null },
      width: '420px',
    });
    ref.afterClosed().subscribe(result => { if (result) this.loadTenants(); });
  }

  openEditDialog(tenant: TenantItem): void {
    const ref = this.dialog.open(TenantDialogComponent, {
      data: { tenant },
      width: '420px',
    });
    ref.afterClosed().subscribe(result => { if (result) this.loadTenants(); });
  }

  deleteTenant(tenant: TenantItem): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Tenant',
        message: `Delete "${tenant.name}"? All associated users will lose their tenant assignment.`,
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.admin.deleteTenant(tenant.id).subscribe({
        next: () => {
          this.snackBar.open('Tenant deleted.', 'Close', { duration: 3000 });
          this.loadTenants();
        },
        error: err => {
          const msg = err.error?.detail ?? 'Delete failed.';
          this.snackBar.open(msg, 'Close', { duration: 4000 });
        },
      });
    });
  }
}
