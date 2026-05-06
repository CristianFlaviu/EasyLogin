import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TenantRoleItem } from '../../../core/models/tenant.model';
import { TenantAdminService } from '../../../core/services/tenant-admin.service';

interface ApiErrorBody {
  message?: string;
}

@Component({
  selector: 'app-tenant-invite-user-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatButtonModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatProgressSpinnerModule,
  ],
  templateUrl: './tenant-invite-user-dialog.component.html',
})
export class TenantInviteUserDialogComponent implements OnInit {
  private readonly tenantAdmin = inject(TenantAdminService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef = inject(MatDialogRef<TenantInviteUserDialogComponent>);

  loading = false;
  rolesLoading = false;
  roles: TenantRoleItem[] = [];

  form = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    tenantRoleId: new FormControl<string | null>(null, Validators.required),
  });

  ngOnInit(): void {
    this.rolesLoading = true;
    this.tenantAdmin.getRoles().subscribe({
      next: roles => {
        this.roles = roles
          .filter(role => !['TenantAdmin', 'SuperAdmin', 'OrgAdmin'].includes(role.name))
          .sort((left, right) => left.name.localeCompare(right.name));
        this.rolesLoading = false;
      },
      error: () => {
        this.rolesLoading = false;
        this.snackBar.open('Failed to load tenant roles.', 'Close', { duration: 5000 });
      },
    });
  }

  submit(): void {
    if (this.form.invalid) return;

    const value = this.form.getRawValue();
    this.loading = true;
    this.tenantAdmin.inviteUser({
      email: value.email!,
      tenantRoleId: value.tenantRoleId!,
    }).subscribe({
      next: result => {
        this.loading = false;
        this.dialogRef.close(result);
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        const body = error.error as ApiErrorBody | null;
        this.snackBar.open(body?.message ?? 'Invite failed. Please try again.', 'Close', { duration: 5000 });
      },
    });
  }
}
