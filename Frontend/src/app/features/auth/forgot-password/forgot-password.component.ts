import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
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
  submitted = false;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  onSubmit(): void {
    if (this.form.invalid) return;
    this.isLoading = true;
    const { email } = this.form.value;
    this.authService.forgotPassword({ email: email! }).subscribe({
      next: () => {
        this.isLoading = false;
        this.submitted = true;
      },
      error: (err) => {
        this.isLoading = false;
        const message = err?.error?.message || 'Something went wrong. Please try again.';
        this.notificationService.error(message);
      }
    });
  }
}
