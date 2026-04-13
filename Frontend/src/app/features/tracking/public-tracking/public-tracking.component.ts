import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TrackingService } from '../services/tracking.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-public-tracking',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './public-tracking.component.html',
  styleUrls: ['./public-tracking.component.css']
})
export class PublicTrackingComponent {
  private trackingService = inject(TrackingService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  form = this.fb.group({
    trackingNumber: ['', Validators.required]
  });

  trackingResult: any = null;
  isLoading = false;

  onSearch(): void {
    if (this.form.invalid) return;
    const tn = this.form.value.trackingNumber!;
    this.isLoading = true;
    this.trackingResult = null;

    this.trackingService.getByTrackingNumber(tn).subscribe({
      next: (data) => {
        this.trackingResult = data;
        this.isLoading = false;
      },
      error: () => {
        this.notificationService.error('Tracking number not found.');
        this.isLoading = false;
      }
    });
  }
}
