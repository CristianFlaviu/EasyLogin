import { Component, inject, OnInit } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';
import { InviteValidationResponse } from '../../../core/models/auth.model';

interface InviteErrorBody {
  code?: string;
  message?: string;
}

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  return group.get('password')?.value === group.get('confirmPassword')?.value
    ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-accept-invite',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatProgressSpinnerModule,
  ],
  templateUrl: './accept-invite.component.html',
  styleUrl: './accept-invite.component.scss',
})
export class AcceptInviteComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  private token = '';

  state: 'loading' | 'ready' | 'error' = 'loading';
  invite: InviteValidationResponse | null = null;
  errorTitle = 'Invite unavailable';
  errorMessage = 'This invite link cannot be used.';
  hidePassword = true;
  hideConfirm = true;
  submitting = false;

  form = new FormGroup({
    password: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/(?=.*[A-Z])(?=.*\d)/),
    ]),
    confirmPassword: new FormControl('', Validators.required),
  }, { validators: passwordsMatch });

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.token) {
      this.router.navigate(['/login']);
      return;
    }

    this.auth.validateInviteToken(this.token).subscribe({
      next: invite => {
        this.invite = invite;
        this.state = 'ready';
      },
      error: (error: HttpErrorResponse) => this.showInviteError(error),
    });
  }

  submit(): void {
    if (this.form.invalid || !this.invite) return;

    this.submitting = true;
    const password = this.form.controls.password.value!;
    const confirmPassword = this.form.controls.confirmPassword.value!;

    this.auth.acceptInvite({
      token: this.token,
      password,
      confirmPassword,
    }).subscribe({
      next: () => {
        this.snackBar.open('Invite accepted. You can sign in now.', 'Close', { duration: 4000 });
        this.router.navigate(['/login']);
      },
      error: (error: HttpErrorResponse) => {
        this.submitting = false;
        this.showInviteError(error);
      },
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }

  private showInviteError(error: HttpErrorResponse): void {
    const body = error.error as InviteErrorBody | null;
    this.errorTitle = body?.code === 'InviteExpired'
      ? 'Invite expired'
      : body?.code === 'InviteAlreadyUsed'
        ? 'Invite already used'
        : 'Invite unavailable';
    this.errorMessage = body?.message ?? 'Ask an administrator to send a new invite.';
    this.state = 'error';
  }
}
