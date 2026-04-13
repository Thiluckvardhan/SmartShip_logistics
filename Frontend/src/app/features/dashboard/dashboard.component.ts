import { Component, OnInit, inject } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ShipmentService } from '../shipments/services/shipment.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, AsyncPipe, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  shipmentService = inject(ShipmentService);
  authService = inject(AuthService);

  shipments: any[] = [];
  isLoading = true;

  get totalShipments(): number {
    return this.shipments.length;
  }

  get inTransitCount(): number {
    return this.shipments.filter(s => s.status === 'InTransit').length;
  }

  get deliveredCount(): number {
    return this.shipments.filter(s => s.status === 'Delivered').length;
  }

  get recentShipments(): any[] {
    return [...this.shipments]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);
  }

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
}
