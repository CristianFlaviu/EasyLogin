import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule,
  ],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss',
})
export class ForgotPasswordComponent {
  private readonly auth = inject(AuthService);
  private readonly snackBar = inject(MatSnackBar);

  form = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
  });

  loading = false;
  submitted = false;
  submittedEmail = '';

  submit(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const email = this.form.value.email!;

    this.auth.forgotPassword({ email }).subscribe({
      next: () => {
        this.loading = false;
        this.submittedEmail = email;
        this.submitted = true;
      },
      error: () => {
        this.loading = false;
        this.snackBar.open('Something went wrong. Please try again.', 'Close', { duration: 4000 });
      },
    });
  }

  resend(): void {
    this.submitted = false;
    this.form.reset();
  }
}
