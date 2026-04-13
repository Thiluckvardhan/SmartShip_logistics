import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminService } from '../services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-hubs',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './hubs.component.html',
  styleUrls: ['./hubs.component.css']
})
export class HubsComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  hubs: any[] = [];
  page = 1;
  pageSize = 5;
  totalItems = 0;
  isLoading = false;
  showForm = false;
  editingId: string | null = null;

  form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    address: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(500)]],
    contactNumber: ['', [Validators.required, Validators.maxLength(30)]],
    managerName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    isActive: [true]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.adminService.getHubs(this.page, this.pageSize).subscribe({
      next: (res) => {
        this.hubs = res.items ?? res ?? [];
        this.totalItems = res.totalCount ?? this.hubs.length;
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

  openCreate(): void {
    this.editingId = null;
    this.form.reset({ isActive: true });
    this.showForm = true;
  }

  openEdit(hub: any): void {
    this.editingId = hub.id;
    this.form.patchValue(hub);
    this.showForm = true;
  }

  cancel(): void {
    this.showForm = false;
    this.editingId = null;
  }

  submit(): void {
    if (this.form.invalid) return;
    const dto = this.form.value;
    const call$ = this.editingId
      ? this.adminService.updateHub(this.editingId, dto)
      : this.adminService.createHub(dto);

    call$.subscribe({
      next: () => {
        this.notificationService.success(this.editingId ? 'Hub updated' : 'Hub created');
        this.cancel();
        this.load();
      },
      error: () => this.notificationService.error('Operation failed')
    });
  }

  delete(id: string): void {
    if (!confirm('Delete this hub?')) return;
    this.adminService.deleteHub(id).subscribe({
      next: () => { this.notificationService.success('Hub deleted'); this.load(); },
      error: () => this.notificationService.error('Delete failed')
    });
  }
}
