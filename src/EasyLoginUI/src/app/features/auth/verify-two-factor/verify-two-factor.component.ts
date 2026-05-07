import { Component, inject } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-verify-two-factor',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatButtonModule, MatFormFieldModule, MatIconModule,
    MatInputModule, MatProgressSpinnerModule,
  ],
  templateUrl: './verify-two-factor.component.html',
  styleUrl: './verify-two-factor.component.scss',
})
export class VerifyTwoFactorComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  form = new FormGroup({
    code: new FormControl('', [Validators.required]),
  });

  loading = false;
  readonly method = this.auth.getTwoFactorMethod();

  constructor() {
    if (!this.auth.getTwoFactorChallengeToken())
      this.router.navigate(['/login']);
  }

  get codeLabel(): string {
    return this.method === 'Email' ? 'Email code' : 'Authenticator code';
  }

  submit(): void {
    if (this.form.invalid || this.loading) return;

    this.loading = true;
    const value = this.form.value.code?.trim() ?? '';
    const returnUrl = this.auth.getTwoFactorReturnUrl();

    this.auth.verifyTwoFactor({
      code: value,
    }).subscribe({
      next: () => {
        this.router.navigateByUrl(returnUrl);
      },
      error: () => {
        this.loading = false;
        this.snackBar.open('Invalid verification code', 'Close', { duration: 4000 });
      },
    });
  }
}
