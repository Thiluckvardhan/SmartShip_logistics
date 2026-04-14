import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css']
})
export class ForgotPasswordComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);

  isLoading = false;
  isResetting = false;
  otpSent = false;
  resetCompleted = false;
  emailForReset = '';
  showNewPassword = false;
  showConfirmPassword = false;

  emailForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  resetForm = this.fb.group(
    {
      token: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required]]
    },
    { validators: this.passwordsMatchValidator }
  );

  onSubmit(): void {
    if (this.emailForm.invalid) return;

    this.isLoading = true;
    const { email } = this.emailForm.value;
    this.authService.forgotPassword({ email: email! }).subscribe({
      next: () => {
        this.isLoading = false;
        this.otpSent = true;
        this.emailForReset = email!;
        this.notificationService.success('OTP sent to your email.');
      },
      error: (err) => {
        this.isLoading = false;
        const message = err?.error?.message || 'Something went wrong. Please try again.';
        this.notificationService.error(message);
      }
    });
  }

  onResetPassword(): void {
    if (this.resetForm.invalid) return;

    this.isResetting = true;
    const { token, newPassword } = this.resetForm.value;

    this.authService.resetPassword({ token: token!, newPassword: newPassword! }).subscribe({
      next: () => {
        this.isResetting = false;
        this.resetCompleted = true;
        this.notificationService.success('Password reset successful. You can sign in now.');
      },
      error: (err) => {
        this.isResetting = false;
        const message = err?.error?.message || 'Password reset failed. Please verify your OTP and try again.';
        this.notificationService.error(message);
      }
    });
  }

  backToEmailStep(): void {
    this.otpSent = false;
    this.resetForm.reset();
    this.showNewPassword = false;
    this.showConfirmPassword = false;
  }

  toggleNewPassword(): void {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmPassword(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  private passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
    const newPassword = control.get('newPassword')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;

    if (!newPassword || !confirmPassword) {
      return null;
    }

    return newPassword === confirmPassword ? null : { passwordMismatch: true };
  }
}
