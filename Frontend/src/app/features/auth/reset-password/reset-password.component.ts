import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css']
})
export class ResetPasswordComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private notificationService = inject(NotificationService);

  isLoading = false;
  token: string = this.route.snapshot.queryParams['token'] ?? '';

  form = this.fb.group({
    newPassword: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]]
  });

  onSubmit(): void {
    if (this.form.invalid) return;
    this.isLoading = true;
    const { newPassword } = this.form.value;
    this.authService.resetPassword({ token: this.token, newPassword: newPassword! }).subscribe({
      next: () => {
        this.isLoading = false;
        this.notificationService.success('Password reset successfully. Please sign in.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.isLoading = false;
        const message = err?.error?.message || 'Password reset failed. The link may have expired.';
        this.notificationService.error(message);
      }
    });
  }
}
