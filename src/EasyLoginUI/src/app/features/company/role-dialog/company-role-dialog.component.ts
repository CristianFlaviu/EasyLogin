import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CompanyAdminService } from '../../../core/services/company-admin.service';

@Component({
  selector: 'app-company-role-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatProgressSpinnerModule,
  ],
  template: `
    <h2 mat-dialog-title>Create Role</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:4px;min-width:360px;padding-top:8px">
        <mat-form-field class="full-width">
          <mat-label>Role Name</mat-label>
          <input matInput formControlName="name" />
          @if (form.get('name')?.hasError('required') && form.get('name')?.touched) {
            <mat-error>Required</mat-error>
          }
        </mat-form-field>
        <mat-form-field class="full-width">
          <mat-label>Description (optional)</mat-label>
          <input matInput formControlName="description" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="form.invalid || loading" (click)="submit()">
        @if (loading) { <mat-spinner diameter="20" /> } @else { Create }
      </button>
    </mat-dialog-actions>
  `,
  styles: ['.full-width { width: 100%; }'],
})
export class CompanyRoleDialogComponent {
  private readonly companyAdmin = inject(CompanyAdminService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef = inject(MatDialogRef<CompanyRoleDialogComponent>);

  loading = false;

  form = new FormGroup({
    name: new FormControl('', Validators.required),
    description: new FormControl(''),
  });

  submit(): void {
    if (this.form.invalid) return;
    const { name, description } = this.form.value;
    this.loading = true;
    this.companyAdmin.createRole({ name: name!, description: description || null }).subscribe({
      next: result => { this.loading = false; this.dialogRef.close(result); },
      error: err => {
        this.loading = false;
        const msg = err.error?.detail ?? err.error?.message ?? 'Failed to create role.';
        this.snackBar.open(msg, 'Close', { duration: 5000 });
      },
    });
  }
}
