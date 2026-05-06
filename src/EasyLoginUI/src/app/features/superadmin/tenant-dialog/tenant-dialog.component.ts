import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminService } from '../../../core/services/admin.service';
import { TenantItem } from '../../../core/models/tenant.model';

export interface TenantDialogData {
  tenant: TenantItem | null;
}

@Component({
  selector: 'app-tenant-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatSlideToggleModule, MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>{{ isEdit ? 'Edit Tenant' : 'Create Tenant' }}</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:8px;min-width:360px;padding-top:8px">
        <mat-form-field class="full-width">
          <mat-label>Tenant Name</mat-label>
          <input matInput formControlName="name" />
          @if (form.get('name')?.hasError('required') && form.get('name')?.touched) {
            <mat-error>Required</mat-error>
          }
        </mat-form-field>
        @if (isEdit) {
          <mat-slide-toggle formControlName="isActive" color="primary">Active</mat-slide-toggle>
        }
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || loading" (click)="submit()">
        @if (loading) { <mat-spinner diameter="20" /> } @else { {{ isEdit ? 'Save' : 'Create' }} }
      </button>
    </mat-dialog-actions>
  `,
  styles: ['.full-width { width: 100%; }'],
})
export class TenantDialogComponent implements OnInit {
  private readonly admin = inject(AdminService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef = inject(MatDialogRef<TenantDialogComponent>);
  readonly data = inject<TenantDialogData>(MAT_DIALOG_DATA);

  readonly isEdit = this.data.tenant !== null;
  loading = false;

  form = new FormGroup({
    name: new FormControl('', Validators.required),
    isActive: new FormControl(true),
  });

  ngOnInit(): void {
    if (this.isEdit && this.data.tenant) {
      this.form.patchValue({ name: this.data.tenant.name, isActive: this.data.tenant.isActive });
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    const { name, isActive } = this.form.value;
    this.loading = true;

    const obs = this.isEdit
      ? this.admin.updateTenant(this.data.tenant!.id, { name: name!, isActive: isActive! })
      : this.admin.createTenant({ name: name! });

    obs.subscribe({
      next: result => { this.loading = false; this.dialogRef.close(result); },
      error: err => {
        this.loading = false;
        const msg = err.error?.detail ?? err.error?.message ?? 'Operation failed.';
        this.snackBar.open(msg, 'Close', { duration: 5000 });
      },
    });
  }
}
