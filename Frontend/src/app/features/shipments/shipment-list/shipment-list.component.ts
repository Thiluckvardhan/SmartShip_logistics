import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { ShipmentService } from '../services/shipment.service';

@Component({
  selector: 'app-shipment-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './shipment-list.component.html',
  styleUrls: ['./shipment-list.component.css']
})
export class ShipmentListComponent implements OnInit {
  private shipmentService = inject(ShipmentService);
  private router = inject(Router);

  shipments: any[] = [];
  isLoading = true;
  pageSize = 10;
  currentPage = 1;

  ngOnInit(): void {
    this.shipmentService.getAll().subscribe({
      next: (data) => {
        this.shipments = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });
  }

  get totalPages(): number {
    return Math.ceil(this.shipments.length / this.pageSize);
  }

  get pagedShipments(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.shipments.slice(start, start + this.pageSize);
  }

  get pages(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
    }
  }

  getStatusClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'badge-draft',
      Booked: 'badge-booked',
      PickedUp: 'badge-pickedup',
      InTransit: 'badge-intransit',
      OutForDelivery: 'badge-outfordelivery',
      Delivered: 'badge-delivered',
      Delayed: 'badge-delayed',
      Failed: 'badge-failed',
      Returned: 'badge-returned'
    };
    return map[status] ?? 'badge-default';
  }

  viewShipment(id: string): void {
    this.router.navigate(['/shipments', id]);
  }
}
