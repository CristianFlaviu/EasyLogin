import { Component, inject, OnInit } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AdminService } from '../../../core/services/admin.service';
import { CompanyItem } from '../../../core/models/company.model';
import { RoleItem, UserListItem } from '../../../core/models/user.model';

interface InviteDialogData {
  companies: CompanyItem[];
  pendingUsers: UserListItem[];
}

interface ApiErrorBody {
  code?: string;
  message?: string;
}

@Component({
  selector: 'app-invite-user-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule, MatDialogModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatProgressSpinnerModule, MatSelectModule,
  ],
  templateUrl: './invite-user-dialog.component.html',
})
export class InviteUserDialogComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly snackBar = inject(MatSnackBar);
  readonly dialogRef = inject(MatDialogRef<InviteUserDialogComponent>);
  readonly data = inject<InviteDialogData>(MAT_DIALOG_DATA);

  companies: CompanyItem[] = this.data.companies;
  systemRoles: RoleItem[] = [];
  loading = false;

  form = new FormGroup({
    firstName: new FormControl('', Validators.required),
    lastName: new FormControl('', Validators.required),
    email: new FormControl('', [Validators.required, Validators.email]),
    companyId: new FormControl<string | null>(null),
    systemRoles: new FormControl<string[]>([]),
  });

  ngOnInit(): void {
    this.adminService.getRoles().subscribe({ next: roles => (this.systemRoles = roles) });
  }

  submit(): void {
    if (this.form.invalid) return;

    this.loading = true;
    const value = this.form.getRawValue();

    this.adminService.inviteUser({
      firstName: value.firstName!,
      lastName: value.lastName!,
      email: value.email!,
      companyId: value.companyId,
      systemRoles: value.systemRoles ?? [],
    }).subscribe({
      next: result => {
        this.loading = false;
        this.dialogRef.close(result);
      },
      error: (error: HttpErrorResponse) => {
        this.loading = false;
        const body = error.error as ApiErrorBody | null;
        if (body?.code === 'InvitePending') {
          const pendingUser = this.data.pendingUsers.find(user =>
            user.email.toLowerCase() === (value.email ?? '').toLowerCase());
          const snackRef = this.snackBar.open(
            body.message ?? 'An invite is already pending.',
            pendingUser ? 'Resend' : 'Close',
            { duration: 6000 });
          if (pendingUser) {
            snackRef.onAction().subscribe(() => {
              this.adminService.resendInvite(pendingUser.id).subscribe({
                next: () => this.snackBar.open('Invite resent.', 'Close', { duration: 3000 }),
                error: () => this.snackBar.open('Could not resend invite.', 'Close', { duration: 4000 }),
              });
            });
          }
          return;
        }

        this.snackBar.open(body?.message ?? 'Invite failed. Please try again.', 'Close', { duration: 5000 });
      },
    });
  }
}
