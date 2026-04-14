import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnDestroy, OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private notificationService = inject(NotificationService);
  private googleInitialized = false;

  isLoading = false;
  isGoogleLoading = false;
  isVerifyingOtp = false;
  isResendingOtp = false;
  showPassword = false;
  otpChallengeId: string | null = null;
  pendingEmail = '';
  googleClientId = environment.googleClientId;
  resendSecondsLeft = 0;
  private resendCooldownTimer: ReturnType<typeof setInterval> | null = null;

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]]
  });

  otpForm = this.fb.group({
    otp: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  onSubmit(): void {
    if (this.form.invalid) return;

    this.isLoading = true;
    const { email, password } = this.form.value;
    this.authService.login({ email: email!, password: password! }).subscribe({
      next: (response) => {
        this.isLoading = false;

        if (response.requiresOtp && response.challengeId) {
          this.otpChallengeId = response.challengeId;
          this.pendingEmail = email!;
          this.otpForm.reset();
          this.startResendCooldown(response.cooldownSeconds ?? 0);
          this.notificationService.success(response.message ?? 'OTP sent to your email.');
          return;
        }

        this.navigateByRole();
      },
      error: (err) => {
        this.isLoading = false;
        const message = err?.error?.message || 'Login failed. Please check your credentials.';
        this.notificationService.error(message);
      }
    });
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onVerifyOtp(): void {
    if (!this.otpChallengeId || this.otpForm.invalid) return;

    this.isVerifyingOtp = true;
    const otp = this.otpForm.value.otp!;

    this.authService.verifyLoginOtp({ challengeId: this.otpChallengeId, otp }).subscribe({
      next: () => {
        this.isVerifyingOtp = false;
        this.navigateByRole();
      },
      error: (err) => {
        this.isVerifyingOtp = false;
        const message = err?.error?.message || 'OTP verification failed. Please try again.';
        this.notificationService.error(message);
      }
    });
  }

  backToCredentials(): void {
    this.otpChallengeId = null;
    this.pendingEmail = '';
    this.otpForm.reset();
    this.isResendingOtp = false;
    this.resendSecondsLeft = 0;
    this.clearResendCooldownTimer();
  }

  onResendOtp(): void {
    if (!this.otpChallengeId || this.isResendingOtp || this.resendSecondsLeft > 0 || this.isVerifyingOtp) {
      return;
    }

    this.isResendingOtp = true;
    this.authService.resendLoginOtp({ challengeId: this.otpChallengeId }).subscribe({
      next: (response) => {
        this.isResendingOtp = false;
        if (response.challengeId) {
          this.otpChallengeId = response.challengeId;
        }

        this.startResendCooldown(response.cooldownSeconds ?? 0);
        this.notificationService.success(response.message ?? 'A new OTP has been sent to your email.');
      },
      error: (err) => {
        this.isResendingOtp = false;
        const cooldownSeconds = Number(err?.error?.cooldownSeconds ?? 0);
        if (cooldownSeconds > 0) {
          this.startResendCooldown(cooldownSeconds);
        }

        const message = err?.error?.message || 'Unable to resend OTP right now. Please try again.';
        this.notificationService.error(message);
      }
    });
  }

  get resendButtonText(): string {
    if (this.isResendingOtp) {
      return 'Sending...';
    }

    if (this.resendSecondsLeft > 0) {
      return `Resend OTP in ${this.resendSecondsLeft}s`;
    }

    return 'Resend OTP';
  }

  ngOnInit(): void {
    if (this.googleClientId) {
      return;
    }

    this.authService.getGoogleConfig().subscribe({
      next: (config) => {
        this.googleClientId = config.clientId?.trim() ?? '';
      },
      error: () => {
        // Keep existing behavior; button handler will show a clear message.
      }
    });
  }

  ngOnDestroy(): void {
    this.clearResendCooldownTimer();
  }

  onGoogleSignIn(): void {
    if (!this.googleClientId) {
      this.notificationService.error('Google sign-in is not configured. Set GoogleAuth__ClientId in Backend/.env and restart the Identity service.');
      return;
    }

    if (!window.google?.accounts?.id) {
      this.notificationService.error('Google sign-in is currently unavailable. Please refresh and try again.');
      return;
    }

    if (!this.googleInitialized) {
      window.google.accounts.id.initialize({
        client_id: this.googleClientId,
        callback: (credentialResponse: GoogleCredentialResponse) => {
          this.handleGoogleCredential(credentialResponse);
        }
      });
      this.googleInitialized = true;
    }

    this.isGoogleLoading = true;
    window.google.accounts.id.prompt((notification: GooglePromptNotification) => {
      const hidden = notification?.isNotDisplayed?.() || notification?.isSkippedMoment?.();
      if (hidden) {
        this.isGoogleLoading = false;
      }
    });
  }

  private handleGoogleCredential(response: GoogleCredentialResponse): void {
    const idToken = response?.credential;
    if (!idToken) {
      this.isGoogleLoading = false;
      this.notificationService.error('Google sign-in did not return a valid token.');
      return;
    }

    this.authService.googleLogin({ idToken }).subscribe({
      next: () => {
        this.isGoogleLoading = false;
        this.navigateByRole();
      },
      error: (err) => {
        this.isGoogleLoading = false;
        const message = err?.error?.message || 'Google login failed. Please try again.';
        this.notificationService.error(message);
      }
    });
  }

  private navigateByRole(): void {
    const role = this.authService.getRole();
    this.router.navigate([role === 'Admin' ? '/admin' : '/dashboard']);
  }

  private startResendCooldown(seconds: number): void {
    const safeSeconds = Math.max(0, Math.floor(seconds));
    this.clearResendCooldownTimer();
    this.resendSecondsLeft = safeSeconds;

    if (safeSeconds <= 0) {
      return;
    }

    this.resendCooldownTimer = setInterval(() => {
      if (this.resendSecondsLeft <= 1) {
        this.resendSecondsLeft = 0;
        this.clearResendCooldownTimer();
        return;
      }

      this.resendSecondsLeft -= 1;
    }, 1000);
  }

  private clearResendCooldownTimer(): void {
    if (!this.resendCooldownTimer) {
      return;
    }

    clearInterval(this.resendCooldownTimer);
    this.resendCooldownTimer = null;
  }
}

interface GoogleCredentialResponse {
  credential?: string;
}

interface GooglePromptNotification {
  isNotDisplayed?: () => boolean;
  isSkippedMoment?: () => boolean;
}

declare global {
  interface Window {
    google?: {
      accounts?: {
        id?: {
          initialize: (options: { client_id: string; callback: (response: GoogleCredentialResponse) => void }) => void;
          prompt: (listener?: (notification: GooglePromptNotification) => void) => void;
        };
      };
    };
  }
}
