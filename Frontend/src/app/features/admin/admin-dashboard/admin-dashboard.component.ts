import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AdminService } from '../services/admin.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  private adminService = inject(AdminService);

  dashboard: any = null;
  statistics: any = null;
  isLoading = true;

  ngOnInit(): void {
    this.adminService.getDashboard().subscribe({
      next: (data) => { this.dashboard = data; this.checkLoading(); },
      error: () => this.checkLoading()
    });
    this.adminService.getStatistics().subscribe({
      next: (data) => { this.statistics = this.normalizeStatistics(data); this.checkLoading(); },
      error: () => this.checkLoading()
    });
  }

  private normalizeStatistics(data: any): any {
    const openExceptions = data?.openExceptions ?? data?.OpenExceptions ?? 0;
    const pendingExceptions = data?.pendingExceptions ?? data?.PendingExceptions ?? openExceptions;

    return {
      ...data,
      totalShipments: data?.totalShipments ?? data?.TotalShipments ?? 0,
      activeHubs: data?.activeHubs ?? data?.ActiveHubs ?? 0,
      totalUsers: data?.totalUsers ?? data?.TotalUsers ?? 0,
      totalExceptions: data?.totalExceptions ?? data?.TotalExceptions ?? 0,
      openExceptions,
      pendingExceptions
    };
  }

  private checkLoading(): void {
    if (this.dashboard !== null || this.statistics !== null) {
      this.isLoading = false;
    }
  }
}
