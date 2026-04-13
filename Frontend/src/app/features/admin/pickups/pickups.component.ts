import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PickupService } from '../../shipments/services/pickup.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-pickups',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './pickups.component.html',
  styleUrls: ['./pickups.component.css']
})
export class PickupsComponent implements OnInit {
  private pickupService = inject(PickupService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  pickups: any[] = [];
  isLoading = false;
  showCreateForm = false;
  editingShipmentId: string | null = null;

  createForm: FormGroup = this.fb.group({
    shipmentId: ['', Validators.required],
    pickupDate: ['', Validators.required],
    notes: ['']
  });

  editForm: FormGroup = this.fb.group({
    pickupDate: ['', Validators.required],
    notes: ['']
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.pickupService.getAll().subscribe({
      next: (data) => { this.pickups = data; this.isLoading = false; },
      error: () => { this.isLoading = false; }
    });
  }

  openCreate(): void {
    this.createForm.reset();
    this.showCreateForm = true;
  }

  cancelCreate(): void {
    this.showCreateForm = false;
  }

  submitCreate(): void {
    if (this.createForm.invalid) return;
    this.pickupService.create(this.createForm.value).subscribe({
      next: () => { this.notificationService.success('Pickup created'); this.cancelCreate(); this.load(); },
      error: () => this.notificationService.error('Failed to create pickup')
    });
  }

  startEdit(pickup: any): void {
    this.editingShipmentId = pickup.shipmentId ?? pickup.id;
    this.editForm.patchValue({ pickupDate: pickup.pickupDate, notes: pickup.notes ?? '' });
  }

  cancelEdit(): void {
    this.editingShipmentId = null;
  }

  submitEdit(shipmentId: string): void {
    if (this.editForm.invalid) return;
    this.pickupService.update(shipmentId, this.editForm.value).subscribe({
      next: () => { this.notificationService.success('Pickup updated'); this.cancelEdit(); this.load(); },
      error: () => this.notificationService.error('Failed to update pickup')
    });
  }
}
