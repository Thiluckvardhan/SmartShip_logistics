import { Component, OnInit } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { nameValidator, phoneValidator } from '../../shared/validators/custom-validators';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, AsyncPipe],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  currentUser$ = this.authService.currentUser$;
  form: FormGroup;
  isLoading = false;

  constructor(
    private authService: AuthService,
    private router: Router,
    private notificationService: NotificationService,
    private fb: FormBuilder
  ) {
    this.form = this.fb.group({
      name: ['', [nameValidator()]],
      phone: ['', [phoneValidator()]]
    });
  }

  ngOnInit(): void {
    this.currentUser$.subscribe(user => {
      if (user) {
        this.form.patchValue({
          name: user.name ?? '',
          phone: user.phone ?? ''
        });
      }
    });
  }

  onUpdate(): void {
    if (this.form.invalid || this.isLoading) return;
    this.isLoading = true;
    const { name, phone } = this.form.value;
    this.authService.updateMe({ name, phone }).subscribe({
      next: updatedUser => {
        localStorage.setItem('user_profile', JSON.stringify(updatedUser));
        this.authService.currentUserSubject.next(updatedUser);
        this.notificationService.success('Profile updated successfully.');
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  onDeleteAccount(): void {
    const confirmed = confirm('Are you sure? This cannot be undone.');
    if (!confirmed) return;
    this.authService.deleteMe().subscribe({
      next: () => {
        this.authService.clearStorage();
        this.router.navigate(['/login']);
      }
    });
  }
}
