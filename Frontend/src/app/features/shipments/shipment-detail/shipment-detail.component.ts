import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ShipmentService } from '../services/shipment.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-shipment-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './shipment-detail.component.html',
  styleUrls: ['./shipment-detail.component.css']
})
export class ShipmentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private shipmentService = inject(ShipmentService);
  private router = inject(Router);
  private notificationService = inject(NotificationService);

  shipment: any = null;
  isLoading = true;
  id = '';

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadShipment();
  }

  private loadShipment(): void {
    this.shipmentService.getById(this.id).subscribe({
      next: (data) => {
        this.shipment = this.normalizeShipment(data);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.notificationService.error('Failed to load shipment.');
      }
    });
  }

  book(): void {
    this.shipmentService.book(this.id).subscribe({
      next: () => {
        this.notificationService.success('Shipment booked successfully.');
        this.loadShipment();
      },
      error: () => {
        this.notificationService.error('Failed to book shipment.');
      }
    });
  }

  delete(): void {
    if (!confirm('Are you sure you want to delete this shipment?')) return;
    this.shipmentService.delete(this.id).subscribe({
      next: () => {
        this.notificationService.success('Shipment deleted.');
        this.router.navigate(['/shipments']);
      },
      error: () => {
        this.notificationService.error('Failed to delete shipment.');
      }
    });
  }

  private normalizeShipment(shipment: any): any {
    if (!shipment) return null;

    return {
      ...shipment,
      id: shipment.id ?? shipment.shipmentId ?? shipment.ShipmentId ?? '',
      trackingNumber: shipment.trackingNumber ?? shipment.TrackingNumber ?? '',
      status: shipment.status ?? shipment.Status ?? '',
      totalWeight: shipment.totalWeight ?? shipment.TotalWeight ?? 0,
      estimatedRate: shipment.estimatedRate ?? shipment.EstimatedRate ?? shipment.cost ?? shipment.Cost ?? 0,
      senderAddress: this.normalizeAddress(shipment.senderAddress ?? shipment.SenderAddress),
      receiverAddress: this.normalizeAddress(shipment.receiverAddress ?? shipment.ReceiverAddress),
      items: this.normalizeItems(shipment.items ?? shipment.Items ?? shipment.packages ?? shipment.Packages)
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
}
