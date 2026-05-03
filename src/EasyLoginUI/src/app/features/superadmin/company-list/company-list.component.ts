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
import { CompanyItem } from '../../../core/models/company.model';
import { CompanyDialogComponent } from '../company-dialog/company-dialog.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-company-list',
  standalone: true,
  imports: [
    DatePipe,
    MatTableModule, MatCardModule, MatButtonModule, MatIconModule,
    MatChipsModule, MatTooltipModule, MatProgressBarModule,
  ],
  templateUrl: './company-list.component.html',
  styleUrl: './company-list.component.scss',
})
export class CompanyListComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  displayedColumns = ['name', 'status', 'createdAt', 'actions'];
  companies: CompanyItem[] = [];
  loading = false;

  ngOnInit(): void {
    this.loadCompanies();
  }

  loadCompanies(): void {
    this.loading = true;
    this.admin.getCompanies().subscribe({
      next: data => { this.companies = data; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CompanyDialogComponent, {
      data: { company: null },
      width: '420px',
    });
    ref.afterClosed().subscribe(result => { if (result) this.loadCompanies(); });
  }

  openEditDialog(company: CompanyItem): void {
    const ref = this.dialog.open(CompanyDialogComponent, {
      data: { company },
      width: '420px',
    });
    ref.afterClosed().subscribe(result => { if (result) this.loadCompanies(); });
  }

  deleteCompany(company: CompanyItem): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Company',
        message: `Delete "${company.name}"? All associated users will lose their company assignment.`,
      },
    });
    ref.afterClosed().subscribe(confirmed => {
      if (!confirmed) return;
      this.admin.deleteCompany(company.id).subscribe({
        next: () => {
          this.snackBar.open('Company deleted.', 'Close', { duration: 3000 });
          this.loadCompanies();
        },
        error: err => {
          const msg = err.error?.detail ?? 'Delete failed.';
          this.snackBar.open(msg, 'Close', { duration: 4000 });
        },
      });
    });
  }
}
