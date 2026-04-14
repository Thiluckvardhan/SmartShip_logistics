import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { TrackingService } from '../services/tracking.service';
import { ShipmentService } from '../../shipments/services/shipment.service';
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
  private shipmentService = inject(ShipmentService);
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

  readonly journeySteps = [
    { status: 'Booked',          label: 'Booked' },
    { status: 'PickedUp',        label: 'Picked Up' },
    { status: 'InTransit',       label: 'In Transit' },
    { status: 'OutForDelivery',  label: 'Out for Delivery' },
    { status: 'Delivered',       label: 'Delivered' },
    { status: 'Delayed',         label: 'Delayed' },
    { status: 'Returned',        label: 'Returned' },
    { status: 'Failed',          label: 'Failed' },
  ];

  private readonly progressOrder = ['Draft','Booked','PickedUp','InTransit','OutForDelivery','Delivered'];

  isStepCompleted(stepStatus: string): boolean {
    const current = this.getJourneyProgressStatus();
    if (!current) return false;

    if (!this.progressOrder.includes(stepStatus)) return false;

    const step = this.progressOrder.indexOf(stepStatus);
    const currentIndex = this.progressOrder.indexOf(current);
    return step < currentIndex;
  }

  isCurrentStep(stepStatus: string): boolean {
    return this.trackingData?.status === stepStatus;
  }

  getJourneyProgressStatus(): string {
    const status = this.trackingData?.status ?? '';
    if (!status) return '';

    if (status === 'Delayed' || status === 'Returned' || status === 'Failed') {
      return 'OutForDelivery';
    }

    return this.progressOrder.includes(status) ? status : 'Booked';
  }

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
    const tn = (this.form.value.trackingNumber ?? '').trim();
    if (!tn) return;

    this.form.patchValue({ trackingNumber: tn });
    this.isLoading = true;
    this.trackingData = null;
    this.timeline = [];
    this.currentLocation = null;

    this.trackingService.getByTrackingNumber(tn, true).subscribe({
      next: (data) => {
        this.trackingData = this.normalizeTrackingData(data, tn);
        this.loadTimeline(tn);
        this.loadCurrentLocation(tn);
      },
      error: () => {
        this.loadShipmentFallback(tn);
      }
    });
  }

  private loadShipmentFallback(tn: string): void {
    this.shipmentService.getByTrackingNumber(tn).subscribe({
      next: (shipment) => {
        this.trackingData = this.normalizeTrackingData(shipment, tn);
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
    this.trackingService.getTimeline(tn, true).subscribe({
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
    this.trackingService.getCurrentLocation(tn, true).subscribe({
      next: (loc) => { this.currentLocation = loc; },
      error: () => { /* location may not be available */ }
    });
  }

  private normalizeTrackingData(data: any, trackingNumber: string): any {
    if (!data) {
      return {
        trackingNumber,
        status: ''
      };
    }

    return {
      ...data,
      trackingNumber: data.trackingNumber ?? data.TrackingNumber ?? trackingNumber,
      status: data.status ?? data.Status ?? data.currentStatus ?? data.CurrentStatus ?? '',
      location: data.location ?? data.Location ?? data.currentLocation ?? data.CurrentLocation ?? null,
      timestamp: data.timestamp ?? data.Timestamp ?? data.lastUpdatedAt ?? data.LastUpdatedAt ?? null
    };
  }
}
