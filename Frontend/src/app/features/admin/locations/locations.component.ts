import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AdminService } from '../services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { nameValidator } from '../../../shared/validators/custom-validators';

@Component({
  selector: 'app-locations',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './locations.component.html',
  styleUrls: ['./locations.component.css']
})
export class LocationsComponent implements OnInit {
  private adminService = inject(AdminService);
  private notificationService = inject(NotificationService);
  private fb = inject(FormBuilder);

  locations: any[] = [];
  page = 1;
  pageSize = 5;
  totalItems = 0;
  isLoading = false;
  showForm = false;
  editingId: string | null = null;

  form: FormGroup = this.fb.group({
    hubId: ['', Validators.required],
    name: ['', [Validators.required, nameValidator()]],
    zipCode: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20)]],
    isActive: [true]
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading = true;
    this.adminService.getLocations(this.page, this.pageSize).subscribe({
      next: (res) => {
        const items = res.items ?? res ?? [];
        this.locations = items.map((location: any) => this.normalizeLocation(location));
        this.totalItems = res.totalCount ?? this.locations.length;
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

  openEdit(loc: any): void {
    this.editingId = loc.id;
    this.form.patchValue({
      hubId: loc.hubId,
      name: loc.name,
      zipCode: loc.zipCode,
      isActive: loc.isActive
    });
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
      ? this.adminService.updateLocation(this.editingId, dto)
      : this.adminService.createLocation(dto);

    call$.subscribe({
      next: () => {
        this.notificationService.success(this.editingId ? 'Location updated' : 'Location created');
        this.cancel();
        this.load();
      },
      error: () => this.notificationService.error('Operation failed')
    });
  }

  delete(id: string): void {
    if (!id) {
      this.notificationService.error('Unable to delete location: missing location id');
      return;
    }

    if (!confirm('Delete this location?')) return;
    this.adminService.deleteLocation(id).subscribe({
      next: () => { this.notificationService.success('Location deleted'); this.load(); },
      error: () => this.notificationService.error('Delete failed')
    });
  }

  private normalizeLocation(location: any): any {
    return {
      id: location.id ?? location.locationId ?? location.LocationId ?? '',
      hubId: location.hubId ?? location.HubId ?? '',
      name: location.name ?? location.Name ?? '',
      zipCode: location.zipCode ?? location.ZipCode ?? '',
      isActive: location.isActive ?? location.IsActive ?? false
    };
  }
}
