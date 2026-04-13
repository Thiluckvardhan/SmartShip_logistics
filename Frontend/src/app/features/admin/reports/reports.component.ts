import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../services/admin.service';

type ReportTab = 'overview' | 'shipment-performance' | 'delivery-sla' | 'revenue' | 'hub-performance';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.css']
})
export class ReportsComponent {
  private adminService = inject(AdminService);

  activeTab: ReportTab = 'overview';
  reportData: any = null;
  isLoading = false;

  readonly tabs: { key: ReportTab; label: string }[] = [
    { key: 'overview', label: 'Overview' },
    { key: 'shipment-performance', label: 'Shipment Performance' },
    { key: 'delivery-sla', label: 'Delivery SLA' },
    { key: 'revenue', label: 'Revenue' },
    { key: 'hub-performance', label: 'Hub Performance' }
  ];

  selectTab(tab: ReportTab): void {
    this.activeTab = tab;
    this.reportData = null;
    this.loadReport(tab);
  }

  private loadReport(tab: ReportTab): void {
    this.isLoading = true;
    let call$;
    switch (tab) {
      case 'overview': call$ = this.adminService.getReports(); break;
      case 'shipment-performance': call$ = this.adminService.getShipmentPerformanceReport(); break;
      case 'delivery-sla': call$ = this.adminService.getDeliverySlaReport(); break;
      case 'revenue': call$ = this.adminService.getRevenueReport(); break;
      case 'hub-performance': call$ = this.adminService.getHubPerformanceReport(); break;
    }
    call$.subscribe({
      next: (data) => { this.reportData = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  get reportEntries(): { key: string; value: any }[] {
    if (!this.reportData || typeof this.reportData !== 'object') return [];
    return Object.entries(this.reportData).map(([key, value]) => ({ key, value }));
  }
}
