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
        this.shipment = data;
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
}
