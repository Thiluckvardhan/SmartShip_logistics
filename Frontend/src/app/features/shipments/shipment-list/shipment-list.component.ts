import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ShipmentService } from '../services/shipment.service';

@Component({
  selector: 'app-shipment-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './shipment-list.component.html',
  styleUrls: ['./shipment-list.component.css']
})
export class ShipmentListComponent implements OnInit {
  private shipmentService = inject(ShipmentService);
  private router = inject(Router);

  shipments: any[] = [];
  isLoading = true;
  pageSize = 5;
  currentPage = 1;
  searchQuery = '';
  statusFilter = '';

  readonly statuses = ['Draft', 'Booked', 'PickedUp', 'InTransit', 'OutForDelivery', 'Delivered', 'Delayed', 'Failed', 'Returned'];

  ngOnInit(): void {
    this.shipmentService.getAll().subscribe({
      next: (data) => {
        const items = data ?? [];
        this.shipments = items.map((shipment: any) => this.normalizeShipment(shipment));
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  get filteredShipments(): any[] {
    return this.shipments.filter(s => {
      const matchSearch = !this.searchQuery || (s.trackingNumber ?? '').toLowerCase().includes(this.searchQuery.toLowerCase());
      const matchStatus = !this.statusFilter || s.status === this.statusFilter;
      return matchSearch && matchStatus;
    });
  }

  get totalFilteredPages(): number {
    return Math.ceil(this.filteredShipments.length / this.pageSize) || 1;
  }

  get pagedFiltered(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredShipments.slice(start, start + this.pageSize);
  }

  prevPage(): void { if (this.currentPage > 1) this.currentPage--; }
  nextPage(): void { if (this.currentPage < this.totalFilteredPages) this.currentPage++; }

  get totalPages(): number { return this.totalFilteredPages; }
  get pagedShipments(): any[] { return this.pagedFiltered; }
  get pages(): number[] { return Array.from({ length: this.totalPages }, (_, i) => i + 1); }
  goToPage(page: number): void { if (page >= 1 && page <= this.totalPages) this.currentPage = page; }
  getStatusClass(status: string): string { return 'status-' + (status ?? '').toLowerCase(); }
  viewShipment(id: string): void { this.router.navigate(['/shipments', id]); }

  private normalizeShipment(shipment: any): any {
    return {
      ...shipment,
      id: shipment.id ?? shipment.shipmentId ?? shipment.ShipmentId ?? '',
      trackingNumber: shipment.trackingNumber ?? shipment.TrackingNumber ?? '',
      status: shipment.status ?? shipment.Status ?? '',
      createdAt: shipment.createdAt ?? shipment.CreatedAt,
      senderAddress: shipment.senderAddress ?? shipment.SenderAddress,
      receiverAddress: shipment.receiverAddress ?? shipment.ReceiverAddress,
      senderName: shipment.senderName ?? shipment.SenderName,
      receiverName: shipment.receiverName ?? shipment.ReceiverName
    };
  }
}
