import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ShipmentService } from '../../shipments/services/shipment.service';
import { AdminService } from '../services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';

type StatusChoice = {
  value: string;
  label: string;
  requiresReason: boolean;
};

/** Mirrors ShipmentService.AllowedTransitions (next statuses only). */
const ALLOWED_NEXT: Record<string, string[]> = {
  Draft: ['Booked'],
  Booked: ['PickedUp', 'Delayed', 'Failed'],
  PickedUp: ['InTransit', 'Delayed', 'Failed'],
  InTransit: ['InTransit', 'OutForDelivery', 'Delayed', 'Failed'],
  OutForDelivery: ['Delivered', 'Delayed', 'Failed', 'Returned'],
  Delayed: ['InTransit', 'OutForDelivery', 'Failed', 'Returned'],
  Failed: ['Returned'],
  Delivered: [],
  Returned: []
};

const STATUS_META: Record<string, StatusChoice> = {
  Booked: { value: 'Booked', label: 'Booked', requiresReason: false },
  PickedUp: { value: 'PickedUp', label: 'Picked Up', requiresReason: false },
  InTransit: { value: 'InTransit', label: 'In Transit', requiresReason: false },
  OutForDelivery: { value: 'OutForDelivery', label: 'Out for Delivery', requiresReason: false },
  Delivered: { value: 'Delivered', label: 'Delivered', requiresReason: false },
  Delayed: { value: 'Delayed', label: 'Delayed', requiresReason: true },
  Returned: { value: 'Returned', label: 'Returned', requiresReason: true },
  Failed: { value: 'Failed', label: 'Failed', requiresReason: true }
};

@Component({
  selector: 'app-admin-shipment-status-navigator',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './admin-shipment-status-navigator.component.html',
  styleUrls: ['./admin-shipment-status-navigator.component.css']
})
export class AdminShipmentStatusNavigatorComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private shipmentService = inject(ShipmentService);
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);

  readonly shipment = signal<any | null>(null);
  /** Persisted backend status used as the source of truth for navigation rules. */
  readonly persistedStatus = signal<string>('');
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly hubs = signal<any[]>([]);
  readonly selectedHub = signal<any | null>(null);
  /** Only valid next steps from current shipment status. */
  readonly nextStatusOptions = signal<StatusChoice[]>([]);

  readonly activeLocations = computed(() => {
    const hub = this.selectedHub();
    const locations = hub?.serviceLocations ?? [];
    return (Array.isArray(locations) ? locations : []).filter((x: any) => x?.isActive ?? x?.IsActive ?? true);
  });

  readonly form = this.fb.group({
    status: ['', Validators.required],
    hubId: ['', Validators.required],
    serviceLocationId: ['', Validators.required],
    reason: ['', [Validators.maxLength(1000)]]
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    if (!id) {
      this.notificationService.error('Shipment id is missing.');
      this.router.navigate(['/admin/shipments']);
      return;
    }

    this.loadShipment(id);
    this.loadHubs();

    this.form.controls.hubId.valueChanges.subscribe((hubId) => {
      const hub = this.hubs().find((x) => x.hubId === hubId) ?? null;
      this.selectedHub.set(hub);
      this.form.patchValue({ serviceLocationId: '' }, { emitEvent: false });
    });
  }

  requiresReason(): boolean {
    const selected = this.nextStatusOptions().find((x) => x.value === this.form.value.status);
    return !!selected?.requiresReason;
  }

  save(): void {
    if (!this.shipment() || this.isSaving()) return;

    if (this.nextStatusOptions().length === 0) {
      this.notificationService.error('No further status updates are allowed for this shipment.');
      return;
    }

    this.form.markAllAsTouched();

    const status = (this.form.value.status ?? '').trim();
    const hubId = (this.form.value.hubId ?? '').trim();
    const serviceLocationId = (this.form.value.serviceLocationId ?? '').trim();

    if (!status) {
      this.notificationService.error('Please select the next status.');
      return;
    }

    if (!hubId) {
      this.notificationService.error('Please select a hub. Hub and service location are required to record where this update applies.');
      return;
    }

    if (!serviceLocationId) {
      this.notificationService.error('Please select a service location. Hub and service location cannot be empty.');
      return;
    }

    if (this.requiresReason() && !this.form.value.reason?.trim()) {
      this.form.controls.reason.setErrors({ required: true });
      this.form.controls.reason.markAsTouched();
      this.notificationService.error('Please enter a reason. It will be shown to the customer on tracking.');
      return;
    }

    const hub = this.hubs().find((x) => x.hubId === this.form.value.hubId);
    const location = this.activeLocations().find((x: any) => x.locationId === this.form.value.serviceLocationId);
    if (!hub || !location) {
      this.notificationService.error('Please select a valid hub and service location.');
      return;
    }

    this.isSaving.set(true);
    this.shipmentService.adminUpdateStatusJourney(this.shipment().id, {
      status: this.form.value.status ?? '',
      hubName: hub.name,
      serviceLocationName: location.name,
      reason: this.form.value.reason?.trim() || undefined
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.notificationService.success('Shipment status updated with journey details.');
        this.router.navigate(['/admin/shipments', this.shipment().id]);
      },
      error: (err) => {
        this.isSaving.set(false);
        this.notificationService.error(err?.error?.message ?? 'Failed to update shipment status.');
      }
    });
  }

  private loadShipment(id: string): void {
    this.isLoading.set(true);
    this.adminService.getShipment(id).subscribe({
      next: (res) => {
        const normalized = this.normalizeShipment(res);
        this.shipment.set(normalized);
        this.persistedStatus.set(normalized.status);
        this.refreshNextStatusOptions();
        this.form.patchValue({ status: '', hubId: '', serviceLocationId: '', reason: '' }, { emitEvent: false });
        if (this.nextStatusOptions().length === 1) {
          this.form.patchValue({ status: this.nextStatusOptions()[0].value });
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.notificationService.error('Failed to load shipment.');
      }
    });
  }

  private loadHubs(): void {
    this.adminService.getHubs(1, 200).subscribe({
      next: (res) => {
        this.hubs.set((res?.items ?? []).map((h: any) => ({
          hubId: h.hubId ?? h.HubId,
          name: h.name ?? h.Name,
          serviceLocations: (h.serviceLocations ?? h.ServiceLocations ?? []).map((location: any) => ({
            locationId: location.locationId ?? location.LocationId,
            name: location.name ?? location.Name,
            isActive: location.isActive ?? location.IsActive ?? true
          }))
        })));
      },
      error: () => {
        this.notificationService.error('Failed to load hubs and service locations.');
      }
    });
  }

  private normalizeShipment(shipment: any): any {
    return {
      ...shipment,
      id: shipment?.id ?? shipment?.shipmentId ?? shipment?.ShipmentId ?? '',
      trackingNumber: shipment?.trackingNumber ?? shipment?.TrackingNumber ?? '',
      status: this.normalizeShipmentStatus(shipment?.status ?? shipment?.Status ?? '')
    };
  }

  private normalizeShipmentStatus(raw: string): string {
    const s = String(raw ?? '').trim();
    if (!s) return '';
    const key = s.toLowerCase().replace(/[\s_-]+/g, '');
    const map: Record<string, string> = {
      draft: 'Draft',
      booked: 'Booked',
      pickedup: 'PickedUp',
      intransit: 'InTransit',
      outfordelivery: 'OutForDelivery',
      delivered: 'Delivered',
      delayed: 'Delayed',
      returned: 'Returned',
      failed: 'Failed'
    };
    return map[key] ?? s;
  }

  private computeNextStatusOptions(currentStatus: string): StatusChoice[] {
    const key = this.normalizeShipmentStatus(currentStatus);
    const codes = ALLOWED_NEXT[key] ?? [];
    return codes
      .map((code) => STATUS_META[code])
      .filter((x): x is StatusChoice => !!x);
  }

  private refreshNextStatusOptions(): void {
    // Keep options tied to persisted status until update is explicitly submitted.
    const next = this.computeNextStatusOptions(this.persistedStatus());
    this.nextStatusOptions.set(next);
  }
}
