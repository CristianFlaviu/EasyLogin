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
import { RoleItem, UserDetail } from '../../../core/models/user.model';

export interface UserDialogData {
  user: UserDetail | null;
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
  private readonly admin = inject(AdminService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef = inject(MatDialogRef<UserDialogComponent>);
  readonly data = inject<UserDialogData>(MAT_DIALOG_DATA);

  readonly isEdit = this.data.user !== null;
  availableRoles: RoleItem[] = [];
  loading = false;
  hidePassword = true;

  form = new FormGroup({
    firstName: new FormControl('', Validators.required),
    lastName: new FormControl('', Validators.required),
    email: new FormControl('', [Validators.required, Validators.email]),
    isActive: new FormControl(true),
    roles: new FormControl<string[]>([], Validators.required),
    password: new FormControl(''),
    confirmPassword: new FormControl(''),
  });

  ngOnInit(): void {
    this.admin.getRoles().subscribe({ next: roles => (this.availableRoles = roles) });

    if (this.isEdit && this.data.user) {
      const u = this.data.user;
      this.form.patchValue({
        firstName: u.firstName,
        lastName: u.lastName,
        email: u.email,
        isActive: u.isActive,
        roles: u.roles,
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
    if (this.form.invalid) return;
    if (this.passwordMismatch) return;

    const { firstName, lastName, email, isActive, roles, password } = this.form.value;
    this.loading = true;

    const obs = this.isEdit
      ? this.admin.updateUser(this.data.user!.id, {
          firstName: firstName!,
          lastName: lastName!,
          email: email!,
          isActive: isActive!,
          roles: roles!,
          newPassword: password || null,
        })
      : this.admin.createUser({
          firstName: firstName!,
          lastName: lastName!,
          email: email!,
          password: password!,
          roles: roles!,
        });

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
