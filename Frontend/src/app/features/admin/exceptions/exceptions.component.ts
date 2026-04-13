import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminService } from '../services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';

type ModalType = 'resolve' | 'delay' | 'return' | null;

@Component({
  selector: 'app-exceptions',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './exceptions.component.html',
  styleUrls: ['./exceptions.component.css']
})
export class ExceptionsComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  exceptions: any[] = [];
  page = 1;
  pageSize = 5;
  totalItems = 0;
  isLoading = false;

  modalType: ModalType = null;
  selectedId: string | null = null;

  resolveForm: FormGroup = this.fb.group({
    resolutionNotes: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(1000)]]
  });

  delayForm: FormGroup = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(1000)]]
  });

  returnForm: FormGroup = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(1000)]]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.adminService.getExceptions(this.page, this.pageSize).subscribe({
      next: (res) => {
        this.exceptions = res.items ?? res ?? [];
        this.totalItems = res.totalCount ?? this.exceptions.length;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize) || 1;
  }

  prevPage(): void { if (this.page > 1) { this.page--; this.load(); } }
  nextPage(): void { if (this.page < this.totalPages) { this.page++; this.load(); } }

  openModal(type: ModalType, id: string): void {
    this.modalType = type;
    this.selectedId = id;
    this.resolveForm.reset();
    this.delayForm.reset();
    this.returnForm.reset();
  }

  closeModal(): void {
    this.modalType = null;
    this.selectedId = null;
  }

  submitResolve(): void {
    if (this.resolveForm.invalid || !this.selectedId) return;
    this.adminService.resolveShipment(this.selectedId, this.resolveForm.value).subscribe({
      next: () => { this.notificationService.success('Shipment resolved'); this.closeModal(); this.load(); },
      error: () => this.notificationService.error('Failed to resolve')
    });
  }

  submitDelay(): void {
    if (this.delayForm.invalid || !this.selectedId) return;
    this.adminService.delayShipment(this.selectedId, this.delayForm.value).subscribe({
      next: () => { this.notificationService.success('Shipment delayed'); this.closeModal(); this.load(); },
      error: () => this.notificationService.error('Failed to delay')
    });
  }

  submitReturn(): void {
    if (this.returnForm.invalid || !this.selectedId) return;
    this.adminService.returnShipment(this.selectedId, this.returnForm.value).subscribe({
      next: () => { this.notificationService.success('Shipment returned'); this.closeModal(); this.load(); },
      error: () => this.notificationService.error('Failed to return')
    });
  }
}
