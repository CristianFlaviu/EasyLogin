import { Component, inject, OnInit } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  return group.get('newPassword')?.value === group.get('confirmPassword')?.value
    ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatCardModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './reset-password.component.html',
  styleUrl: './reset-password.component.scss',
})
export class ResetPasswordComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  private token = '';
  private email = '';

  form = new FormGroup({
    newPassword: new FormControl('', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/(?=.*[A-Z])(?=.*\d)/),
    ]),
    confirmPassword: new FormControl('', Validators.required),
  }, { validators: passwordsMatch });

  loading = false;
  hidePassword = true;
  hideConfirm = true;

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.email = this.route.snapshot.queryParamMap.get('email') ?? '';
    if (!this.token || !this.email) {
      this.router.navigate(['/forgot-password']);
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const { newPassword, confirmPassword } = this.form.value;

    this.auth.resetPassword({
      email: this.email,
      token: this.token,
      newPassword: newPassword!,
      confirmPassword: confirmPassword!,
    }).subscribe({
      next: () => {
        this.snackBar.open('Password has been reset', 'Close', { duration: 4000 });
        this.router.navigate(['/login']);
      },
      error: () => {
        this.loading = false;
        this.snackBar.open(
          'This reset link has expired. Please request a new one.',
          'Request new link',
          { duration: 6000 }
        ).onAction().subscribe(() => this.router.navigate(['/forgot-password']));
      },
    });
  }
}
