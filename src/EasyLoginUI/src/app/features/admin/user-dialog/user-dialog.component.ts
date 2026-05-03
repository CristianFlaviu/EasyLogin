import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminService } from '../../../core/services/admin.service';
import { CompanyAdminService } from '../../../core/services/company-admin.service';
import { RoleItem, UserDetail } from '../../../core/models/user.model';
import { CompanyItem, CompanyRoleItem } from '../../../core/models/company.model';

export interface UserDialogData {
  user: UserDetail | null;
  mode: 'superadmin' | 'companyadmin';
  companies?: CompanyItem[];
  companyRoles?: CompanyRoleItem[];
}

@Component({
  selector: 'app-user-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule,
    MatSlideToggleModule, MatProgressSpinnerModule,
  ],
  templateUrl: './user-dialog.component.html',
})
export class UserDialogComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly companyAdminService = inject(CompanyAdminService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef = inject(MatDialogRef<UserDialogComponent>);
  readonly data = inject<UserDialogData>(MAT_DIALOG_DATA);

  readonly isEdit = this.data.user !== null;
  readonly isSuperAdmin = this.data.mode === 'superadmin';

  systemRoles: RoleItem[] = [];
  companyRoles: CompanyRoleItem[] = this.data.companyRoles ?? [];
  companies: CompanyItem[] = this.data.companies ?? [];

  loading = false;
  hidePassword = true;

  form = new FormGroup({
    firstName: new FormControl('', Validators.required),
    lastName: new FormControl('', Validators.required),
    email: new FormControl('', [Validators.required, Validators.email]),
    isActive: new FormControl(true),
    companyId: new FormControl<string | null>(null),
    systemRoles: new FormControl<string[]>([]),
    companyRoleIds: new FormControl<string[]>([]),
    password: new FormControl(''),
    confirmPassword: new FormControl(''),
  });

  ngOnInit(): void {
    if (this.isSuperAdmin) {
      this.adminService.getRoles().subscribe({ next: r => (this.systemRoles = r) });
    }

    if (this.isEdit && this.data.user) {
      const u = this.data.user;
      const selectedCompanyRoleIds = this.companyRoles
        .filter(r => u.companyRoles.includes(r.name))
        .map(r => r.id);

      this.form.patchValue({
        firstName: u.firstName,
        lastName: u.lastName,
        email: u.email,
        isActive: u.isActive,
        companyId: u.companyId,
        systemRoles: u.roles,
        companyRoleIds: selectedCompanyRoleIds,
      });
    } else {
      this.form.get('password')!.setValidators([
        Validators.required,
        Validators.minLength(8),
        Validators.pattern(/(?=.*[A-Z])(?=.*\d)/),
      ]);
      this.form.get('confirmPassword')!.setValidators(Validators.required);
      this.form.get('password')!.updateValueAndValidity();
      this.form.get('confirmPassword')!.updateValueAndValidity();
    }
  }

  get passwordMismatch(): boolean {
    const pw = this.form.get('password')?.value;
    const confirm = this.form.get('confirmPassword')?.value;
    return !!(pw && confirm && pw !== confirm);
  }

  submit(): void {
    if (this.form.invalid || this.passwordMismatch) return;

    const { firstName, lastName, email, isActive, companyId, systemRoles, companyRoleIds, password } = this.form.value;
    this.loading = true;

    let obs;

    if (this.isSuperAdmin) {
      obs = this.isEdit
        ? this.adminService.updateUser(this.data.user!.id, {
            firstName: firstName!,
            lastName: lastName!,
            email: email!,
            isActive: isActive!,
            systemRoles: systemRoles ?? [],
            newPassword: password || null,
          })
        : this.adminService.createUser({
            firstName: firstName!,
            lastName: lastName!,
            email: email!,
            password: password!,
            systemRoles: systemRoles ?? [],
            companyId: companyId ?? null,
          });
    } else {
      obs = this.isEdit
        ? this.companyAdminService.updateUser(this.data.user!.id, {
            firstName: firstName!,
            lastName: lastName!,
            email: email!,
            isActive: isActive!,
            companyRoleIds: companyRoleIds ?? [],
            newPassword: password || null,
          })
        : this.companyAdminService.createUser({
            firstName: firstName!,
            lastName: lastName!,
            email: email!,
            password: password!,
            companyRoleIds: companyRoleIds ?? [],
          });
    }

    obs.subscribe({
      next: result => {
        this.loading = false;
        this.dialogRef.close(result);
      },
      error: err => {
        this.loading = false;
        const msg = err.error?.detail ?? err.error?.message ?? 'Operation failed. Please try again.';
        this.snackBar.open(msg, 'Close', { duration: 5000 });
      },
    });
  }
}
