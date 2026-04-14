import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AdminService } from '../services/admin.service';
import { ShipmentService } from '../../shipments/services/shipment.service';
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
  private shipmentService = inject(ShipmentService);
  private notificationService = inject(NotificationService);

  shipment: any = null;
  creator: { name: string; email: string } | null = null;
  isLoading = true;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.adminService.getShipment(id).subscribe({
      next: (data) => {
        this.shipment = this.normalizeShipment(data);
        this.isLoading = false;
        this.loadCreatorInfo();
      },
      error: () => { this.isLoading = false; }
    });
  }

  action(type: string): void {
    const id = this.shipment?.id;
    if (!id) {
      this.notificationService.error('Unable to update status: missing shipment id');
      return;
    }

    let call$;
    switch (type) {
      case 'pickup': call$ = this.shipmentService.markPickedUp(id); break;
      case 'in-transit': call$ = this.shipmentService.markInTransit(id); break;
      case 'out-for-delivery': call$ = this.shipmentService.markOutForDelivery(id); break;
      case 'delivered': call$ = this.shipmentService.markDelivered(id); break;
      case 'delay': call$ = this.shipmentService.markDelayed(id); break;
      case 'return': call$ = this.shipmentService.markReturned(id); break;
      default: return;
    }
    call$.subscribe({
      next: (updated) => {
        this.shipment = { ...this.shipment, ...updated };
        this.notificationService.success('Status updated successfully');
      },
      error: () => this.notificationService.error('Failed to update status')
    });
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
