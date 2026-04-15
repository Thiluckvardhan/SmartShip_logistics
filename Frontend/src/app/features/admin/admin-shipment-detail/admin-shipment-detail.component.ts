import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AdminService } from '../services/admin.service';
import { TrackingService } from '../../tracking/services/tracking.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-admin-shipment-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-shipment-detail.component.html',
  styleUrls: ['./admin-shipment-detail.component.css']
})
export class AdminShipmentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private adminService = inject(AdminService);
  private trackingService = inject(TrackingService);
  private notificationService = inject(NotificationService);

  shipment: any = null;
  creator: { name: string; email: string } | null = null;
  travelHistory: Array<{
    fromHub: string;
    toHub: string;
    fromServiceLocation: string;
    toServiceLocation: string;
    status: string;
    timestamp: string | null;
  }> = [];
  isLoading = true;
  private travelHistoryTimer: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.adminService.getShipment(id).subscribe({
      next: (data) => {
        this.shipment = this.normalizeShipment(data);
        this.isLoading = false;
        this.loadCreatorInfo();
        this.startTravelHistoryAutoRefresh();
      },
      error: () => { this.isLoading = false; }
    });
  }

  ngOnDestroy(): void {
    this.stopTravelHistoryAutoRefresh();
  }

  private loadTravelHistory(): void {
    const trackingNumber = String(this.shipment?.trackingNumber ?? '').trim();
    if (!trackingNumber) {
      this.travelHistory = [];
      return;
    }

    this.trackingService.getTimeline(trackingNumber, true).subscribe({
      next: (events) => {
        this.travelHistory = this.mapTravelHistory(events ?? []);
      },
      error: () => {
        this.travelHistory = [];
      }
    });
  }

  private startTravelHistoryAutoRefresh(): void {
    this.stopTravelHistoryAutoRefresh();
    this.loadTravelHistory();

    this.travelHistoryTimer = setInterval(() => {
      this.loadTravelHistory();
    }, 4000);
  }

  private stopTravelHistoryAutoRefresh(): void {
    if (this.travelHistoryTimer) {
      clearInterval(this.travelHistoryTimer);
      this.travelHistoryTimer = null;
    }
  }

  private mapTravelHistory(events: any[]): Array<{
    fromHub: string;
    toHub: string;
    fromServiceLocation: string;
    toServiceLocation: string;
    status: string;
    timestamp: string | null;
  }> {
    const sorted = [...events].sort((a, b) => {
      const first = new Date(a?.timestamp ?? a?.Timestamp ?? 0).getTime();
      const second = new Date(b?.timestamp ?? b?.Timestamp ?? 0).getTime();
      return Number.isNaN(first) || Number.isNaN(second) ? 0 : first - second;
    });

    const cleaned = sorted
      .map((event) => ({
        location: this.parseTimelineLocation(event?.location ?? event?.Location ?? ''),
        status: String(event?.status ?? event?.Status ?? '').trim(),
        timestamp: event?.timestamp ?? event?.Timestamp ?? null
      }))
      .filter((event) => !!event.location.hub || !!event.location.serviceLocation);

    const transitions: Array<{
      fromHub: string;
      toHub: string;
      fromServiceLocation: string;
      toServiceLocation: string;
      status: string;
      timestamp: string | null;
    }> = [];

    for (let i = 1; i < cleaned.length; i++) {
      const from = cleaned[i - 1];
      const to = cleaned[i];

      const sameHub = from.location.hub === to.location.hub;
      const sameServiceLocation = from.location.serviceLocation === to.location.serviceLocation;
      if (sameHub && sameServiceLocation) {
        continue;
      }

      transitions.push({
        fromHub: from.location.hub || '—',
        toHub: to.location.hub || '—',
        fromServiceLocation: from.location.serviceLocation || '—',
        toServiceLocation: to.location.serviceLocation || '—',
        status: to.status,
        timestamp: to.timestamp
      });
    }

    return transitions;
  }

  private parseTimelineLocation(raw: string): { hub: string; serviceLocation: string } {
    const location = String(raw ?? '').trim();
    if (!location) {
      return { hub: '', serviceLocation: '' };
    }

    const ignored = ['system', 'hub', 'destination', ''];
    const parts = location
      .split(/[·•|]/)
      .map((part) => part.trim())
      .filter((part) => !ignored.includes(part.toLowerCase()));

    if (parts.length >= 2) {
      return {
        serviceLocation: parts[0],
        hub: parts[parts.length - 1]
      };
    }

    if (parts.length === 1) {
      return {
        serviceLocation: parts[0],
        hub: parts[0]
      };
    }

    return { hub: '', serviceLocation: '' };
  }

  private normalizeShipment(shipment: any): any {
    if (!shipment) return null;

    return {
      ...shipment,
      id: shipment?.id ?? shipment?.shipmentId ?? shipment?.ShipmentId ?? '',
      customerId: shipment?.customerId ?? shipment?.CustomerId ?? '',
      trackingNumber: shipment?.trackingNumber ?? shipment?.TrackingNumber ?? '',
      status: shipment?.status ?? shipment?.Status ?? '',
      createdAt: shipment?.createdAt ?? shipment?.CreatedAt,
      totalWeight: shipment?.totalWeight ?? shipment?.TotalWeight ?? 0,
      estimatedRate: shipment?.estimatedRate ?? shipment?.EstimatedRate ?? shipment?.cost ?? shipment?.Cost ?? 0,
      senderAddress: this.normalizeAddress(shipment?.senderAddress ?? shipment?.SenderAddress),
      receiverAddress: this.normalizeAddress(shipment?.receiverAddress ?? shipment?.ReceiverAddress),
      items: this.normalizeItems(shipment?.items ?? shipment?.Items ?? shipment?.packages ?? shipment?.Packages)
    };
  }

  private normalizeAddress(address: any): any {
    if (!address) return null;

    return {
      ...address,
      name: address.name ?? address.Name ?? '',
      phone: address.phone ?? address.Phone ?? '',
      street: address.street ?? address.Street ?? '',
      city: address.city ?? address.City ?? '',
      state: address.state ?? address.State ?? '',
      country: address.country ?? address.Country ?? '',
      pincode: address.pincode ?? address.postalCode ?? address.PostalCode ?? ''
    };
  }

  private normalizeItems(items: any): any[] {
    const source = Array.isArray(items) ? items : [];

    return source.map((item: any) => ({
      ...item,
      itemName: item.itemName ?? item.ItemName ?? '',
      quantity: item.quantity ?? item.Quantity ?? 0,
      weight: item.weight ?? item.Weight ?? 0,
      description: item.description ?? item.Description ?? ''
    }));
  }

  private loadCreatorInfo(): void {
    const customerId = this.shipment?.customerId;
    if (!customerId) return;

    this.adminService.getUserById(customerId).subscribe({
      next: (user) => {
        this.creator = {
          name: user?.name ?? user?.Name ?? 'Unknown User',
          email: user?.email ?? user?.Email ?? '—'
        };
      },
      error: () => {
        this.creator = {
          name: 'Unknown User',
          email: '—'
        };
      }
    });
  }
}
