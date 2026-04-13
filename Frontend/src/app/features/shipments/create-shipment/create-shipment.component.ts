import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ShipmentService } from '../services/shipment.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-create-shipment',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './create-shipment.component.html',
  styleUrls: ['./create-shipment.component.css']
})
export class CreateShipmentComponent {
  private fb = inject(FormBuilder);
  private shipmentService = inject(ShipmentService);
  private router = inject(Router);
  private notificationService = inject(NotificationService);

  isSubmitting = false;

  form: FormGroup = this.fb.group({
    senderAddress: this.buildAddressGroup(),
    receiverAddress: this.buildAddressGroup(),
    items: this.fb.array([this.buildItemGroup()])
  });

  private buildAddressGroup(): FormGroup {
    return this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      phone: ['', [Validators.required, Validators.maxLength(20)]],
      street: ['', [Validators.required]],
      city: ['', [Validators.required]],
      state: ['', [Validators.required]],
      country: ['', [Validators.required]],
      pincode: ['', [Validators.required]]
    });
  }

  private buildItemGroup(): FormGroup {
    return this.fb.group({
      itemName: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(200)]],
      quantity: [1, [Validators.required, Validators.min(1), Validators.max(10000)]],
      weight: [0.1, [Validators.required, Validators.min(0.01), Validators.max(50000)]],
      description: ['']
    });
  }

  get senderAddress(): FormGroup {
    return this.form.get('senderAddress') as FormGroup;
  }

  get receiverAddress(): FormGroup {
    return this.form.get('receiverAddress') as FormGroup;
  }

  get items(): FormArray {
    return this.form.get('items') as FormArray;
  }

  addItem(): void {
    this.items.push(this.buildItemGroup());
  }

  removeItem(i: number): void {
    if (this.items.length > 1) {
      this.items.removeAt(i);
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.isSubmitting = true;
    this.shipmentService.create(this.form.value).subscribe({
      next: () => {
        this.notificationService.success('Shipment created successfully.');
        this.router.navigate(['/shipments']);
      },
      error: () => {
        this.notificationService.error('Failed to create shipment.');
        this.isSubmitting = false;
      }
    });
  }

  hasError(group: FormGroup, field: string, error: string): boolean {
    const ctrl = group.get(field);
    return !!(ctrl && ctrl.touched && ctrl.hasError(error));
  }
}
