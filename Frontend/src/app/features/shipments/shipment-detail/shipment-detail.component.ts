import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ShipmentService } from '../services/shipment.service';
import { NotificationService } from '../../../core/services/notification.service';

type ShipmentIssue = {
  exceptionId?: string;
  shipmentId?: string;
  exceptionType?: string;
  description?: string;
  status?: string;
  createdAt?: string;
  resolvedAt?: string | null;
};

const ISSUE_TYPES = [
  { value: 'Billing Issue', label: 'Billing Issue' },
  { value: 'Damaged Product', label: 'Damaged Product' },
  { value: 'Other', label: 'Other' }
];

@Component({
  selector: 'app-shipment-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './shipment-detail.component.html',
  styleUrls: ['./shipment-detail.component.css']
})
export class ShipmentDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private shipmentService = inject(ShipmentService);
  private router = inject(Router);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  shipment: any = null;
  issues: ShipmentIssue[] = [];
  isLoading = true;
  isLoadingIssues = false;
  id = '';
  issueSubmitting = false;
  readonly issueTypes = ISSUE_TYPES;
  issueForm: FormGroup = this.fb.group({
    issueType: ['Billing Issue', [Validators.required]],
    description: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(1000)]]
  });

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadShipment();
  }

  private loadShipment(): void {
    this.shipmentService.getById(this.id).subscribe({
      next: (data) => {
        this.shipment = this.normalizeShipment(data);
        this.isLoading = false;
        this.loadIssues();
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

  get canReportIssue(): boolean {
    const status = String(this.shipment?.status ?? '').toLowerCase();
    return !!this.shipment && status !== 'draft';
  }

  get issueAvailabilityMessage(): string {
    const status = String(this.shipment?.status ?? '').toLowerCase();
    if (status === 'draft') {
      return 'Book your shipment first, then you can raise Billing Issue or Other. Damaged Product is allowed only after delivery.';
    }

    if (status === 'delivered') {
      return 'All issue types are available now, including Damaged Product.';
    }

    return 'Billing Issue and Other can be raised now. Damaged Product becomes available after delivery.';
  }

  get issueTypeOptions() {
    const status = String(this.shipment?.status ?? '').toLowerCase();
    const options = ISSUE_TYPES.filter((option) => option.value !== 'Damaged Product' || status === 'delivered');
    return options;
  }

  get hasPendingIssue(): boolean {
    return this.issues.some((issue) => String(issue.status ?? '').toLowerCase() !== 'resolved');
  }

  private loadIssues(): void {
    if (!this.id) return;

    this.isLoadingIssues = true;
    this.shipmentService.getIssues(this.id).subscribe({
      next: (res) => {
        const items = res?.items ?? res?.Items ?? res ?? [];
        this.issues = Array.isArray(items) ? items.map((item: any) => this.normalizeIssue(item)) : [];
        this.isLoadingIssues = false;
      },
      error: () => {
        this.issues = [];
        this.isLoadingIssues = false;
      }
    });
  }

  reportIssue(): void {
    if (this.issueSubmitting || !this.canReportIssue) return;
    if (this.issueForm.invalid) {
      this.issueForm.markAllAsTouched();
      return;
    }
    const issueType = (this.issueForm.value.issueType as string).trim();
    const description = (this.issueForm.value.description as string).trim();

    if (issueType === 'Damaged Product' && String(this.shipment?.status ?? '').toLowerCase() !== 'delivered') {
      this.notificationService.error('Damaged Product issues can be reported only after delivery.');
      return;
    }

    this.issueSubmitting = true;
    this.shipmentService.reportIssue(this.id, { issueType, description }).subscribe({
      next: () => {
        this.notificationService.success('Your issue was submitted. Our support team will review it shortly.');
        this.issueForm.reset({ issueType: 'Billing Issue', description: '' });
        this.issueSubmitting = false;
        this.loadIssues();
      },
      error: (err) => {
        this.issueSubmitting = false;
        const msg = err?.error?.message ?? 'Failed to submit issue.';
        this.notificationService.error(msg);
      }
    });
  }

  submitIssue(): void {
    this.reportIssue();
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

  private normalizeIssue(issue: any): ShipmentIssue {
    return {
      exceptionId: issue?.exceptionId ?? issue?.ExceptionId ?? issue?.id ?? issue?.Id ?? '',
      shipmentId: issue?.shipmentId ?? issue?.ShipmentId ?? this.id,
      exceptionType: issue?.exceptionType ?? issue?.ExceptionType ?? issue?.issueType ?? issue?.IssueType ?? '',
      description: issue?.description ?? issue?.Description ?? '',
      status: issue?.status ?? issue?.Status ?? 'Pending',
      createdAt: issue?.createdAt ?? issue?.CreatedAt ?? '',
      resolvedAt: issue?.resolvedAt ?? issue?.ResolvedAt ?? null
    };
  }
}
