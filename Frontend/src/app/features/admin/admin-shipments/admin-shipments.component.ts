import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../services/admin.service';
import { ShipmentService } from '../../shipments/services/shipment.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-admin-shipments',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './admin-shipments.component.html',
  styleUrls: ['./admin-shipments.component.css']
})
export class AdminShipmentsComponent implements OnInit {
  private adminService = inject(AdminService);
  private shipmentService = inject(ShipmentService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  shipments: any[] = [];
  page = 1;
  pageSize = 10;
  totalItems = 0;
  statusFilter = '';
  isLoading = false;

  readonly statuses = ['', 'Booked', 'PickedUp', 'InTransit', 'OutForDelivery', 'Delivered', 'Failed', 'Returned', 'Delayed'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.adminService.getShipments(this.page, this.pageSize).subscribe({
      next: (res) => {
        const items = res.items ?? res ?? [];
        this.shipments = items.map((shipment: any) => this.normalizeShipment(shipment));
        this.totalItems = res.totalCount ?? this.shipments.length;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  get filteredShipments(): any[] {
    if (!this.statusFilter) return this.shipments;
    return this.shipments.filter(s => s.status === this.statusFilter);
  }

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize) || 1;
  }

  prevPage(): void {
    if (this.page > 1) { this.page--; this.load(); }
  }

  nextPage(): void {
    if (this.page < this.totalPages) { this.page++; this.load(); }
  }

  viewShipment(id: string): void {
    if (!id) {
      this.notificationService.error('Unable to open shipment: missing shipment id');
      return;
    }

    this.router.navigate(['/admin/shipments', id]);
  }

  updateStatus(id: string, action: string): void {
    if (!id) {
      this.notificationService.error('Unable to update status: missing shipment id');
      return;
    }

    let call$;
    switch (action) {
      case 'pickup': call$ = this.shipmentService.markPickedUp(id); break;
      case 'in-transit': call$ = this.shipmentService.markInTransit(id); break;
      case 'out-for-delivery': call$ = this.shipmentService.markOutForDelivery(id); break;
      case 'delivered': call$ = this.shipmentService.markDelivered(id); break;
      case 'failed': call$ = this.shipmentService.markFailed(id); break;
      case 'delay': call$ = this.shipmentService.markDelayed(id); break;
      case 'return': call$ = this.shipmentService.markReturned(id); break;
      default: return;
    }
    call$.subscribe({
      next: () => { this.notificationService.success('Status updated'); this.load(); },
      error: () => this.notificationService.error('Failed to update status')
    });
  }

  private normalizeShipment(shipment: any): any {
    return {
      ...shipment,
      id: shipment.id ?? shipment.shipmentId ?? shipment.ShipmentId ?? '',
      trackingNumber: shipment.trackingNumber ?? shipment.TrackingNumber ?? '',
      status: shipment.status ?? shipment.Status ?? '',
      createdAt: shipment.createdAt ?? shipment.CreatedAt,
      senderAddress: shipment.senderAddress ?? shipment.SenderAddress,
      receiverAddress: shipment.receiverAddress ?? shipment.ReceiverAddress
    };
  }
}
