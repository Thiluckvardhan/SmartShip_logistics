import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TrackingService } from '../services/tracking.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-tracking',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './tracking.component.html',
  styleUrls: ['./tracking.component.css']
})
export class TrackingComponent implements OnInit {
  private trackingService = inject(TrackingService);
  private route = inject(ActivatedRoute);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  form = this.fb.group({
    trackingNumber: ['', Validators.required]
  });

  trackingData: any = null;
  timeline: any[] = [];
  currentLocation: any = null;
  isLoading = false;

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const tn = params['trackingNumber'];
      if (tn) {
        this.form.patchValue({ trackingNumber: tn });
        this.onSearch();
      }
    });
  }

  onSearch(): void {
    if (this.form.invalid) return;
    const tn = this.form.value.trackingNumber!;
    this.isLoading = true;
    this.trackingData = null;
    this.timeline = [];
    this.currentLocation = null;

    this.trackingService.getByTrackingNumber(tn).subscribe({
      next: (data) => {
        this.trackingData = data;
        this.loadTimeline(tn);
        this.loadCurrentLocation(tn);
      },
      error: () => {
        this.notificationService.error('Tracking number not found.');
        this.isLoading = false;
      }
    });
  }

  private loadTimeline(tn: string): void {
    this.trackingService.getTimeline(tn).subscribe({
      next: (events) => {
        this.timeline = events;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  private loadCurrentLocation(tn: string): void {
    this.trackingService.getCurrentLocation(tn).subscribe({
      next: (loc) => { this.currentLocation = loc; },
      error: () => { /* location may not be available */ }
    });
  }
}
