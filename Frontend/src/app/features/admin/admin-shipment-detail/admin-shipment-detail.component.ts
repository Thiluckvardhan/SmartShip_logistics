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
  isLoading = true;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.adminService.getShipment(id).subscribe({
      next: (data) => { this.shipment = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  action(type: string): void {
    const id = this.shipment?.id;
    if (!id) return;
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
}
