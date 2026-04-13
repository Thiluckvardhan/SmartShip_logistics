import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { nameValidator, phoneValidator } from '../../../shared/validators/custom-validators';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private notificationService = inject(NotificationService);

  isLoading = false;

  form = this.fb.group({
    name: ['', [Validators.required, nameValidator()]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]],
    phone: ['', [phoneValidator()]]
  });

  onSubmit(): void {
    if (this.form.invalid) return;
    this.isLoading = true;
    const { name, email, password, phone } = this.form.value;
    const dto: any = { name: name!, email: email!, password: password! };
    if (phone) dto.phone = phone;

    this.authService.register(dto).subscribe({
      next: () => {
        this.isLoading = false;
        this.notificationService.success('Account created successfully! Please sign in.');
        this.router.navigate(['/login']);
      },
      error: (err) => {
        this.isLoading = false;
        const message = err?.error?.message || 'Registration failed. Please try again.';
        this.notificationService.error(message);
      }
    });
  }
}
