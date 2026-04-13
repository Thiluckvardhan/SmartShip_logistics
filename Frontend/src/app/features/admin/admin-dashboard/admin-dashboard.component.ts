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
      next: (data) => { this.statistics = data; this.checkLoading(); },
      error: () => this.checkLoading()
    });
  }

  private checkLoading(): void {
    if (this.dashboard !== null || this.statistics !== null) {
      this.isLoading = false;
    }
  }
}
