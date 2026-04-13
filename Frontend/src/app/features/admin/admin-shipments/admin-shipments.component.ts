import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
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

  shipments: any[] = [];
  page = 1;
  pageSize = 10;
  totalItems = 0;
  statusFilter = '';
  isLoading = false;

  readonly statuses = ['', 'Draft', 'Booked', 'PickedUp', 'InTransit', 'OutForDelivery', 'Delivered', 'Delayed', 'Failed', 'Returned'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.adminService.getShipments(this.page, this.pageSize).subscribe({
      next: (res) => {
        this.shipments = res.items ?? res ?? [];
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

  updateStatus(id: string, action: string): void {
    let call$;
    switch (action) {
      case 'pickup': call$ = this.shipmentService.markPickedUp(id); break;
      case 'in-transit': call$ = this.shipmentService.markInTransit(id); break;
      case 'out-for-delivery': call$ = this.shipmentService.markOutForDelivery(id); break;
      case 'delivered': call$ = this.shipmentService.markDelivered(id); break;
      case 'delay': call$ = this.shipmentService.markDelayed(id); break;
      case 'return': call$ = this.shipmentService.markReturned(id); break;
      default: return;
    }
    call$.subscribe({
      next: () => { this.notificationService.success('Status updated'); this.load(); },
      error: () => this.notificationService.error('Failed to update status')
    });
  }
}
