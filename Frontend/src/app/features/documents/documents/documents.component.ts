import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DocumentService } from '../services/document.service';
import { ShipmentService } from '../../shipments/services/shipment.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';

const ALLOWED_TYPES = ['application/pdf', 'image/png', 'image/jpeg'];
const MAX_SIZE_MB = 10;

@Component({
  selector: 'app-documents',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './documents.component.html',
  styleUrls: ['./documents.component.css']
})
export class DocumentsComponent implements OnInit {
  private documentService = inject(DocumentService);
  private shipmentService = inject(ShipmentService);
  private notificationService = inject(NotificationService);
  private authService = inject(AuthService);

  documents: any[] = [];
  shipments: any[] = [];

  selectedShipmentId = '';
  selectedFile: File | null = null;
  selectedDocType = 'General';
  fileError = '';
  isUploading = false;

  // Pagination
  currentPage = 1;
  pageSize = 5;
  totalItems = 0;

  docTypes = ['General', 'Label', 'Customs'];
  isDragOver = false;

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = true;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;
    const file = event.dataTransfer?.files?.[0];
    if (file) this.validateAndSetFile(file);
  }

  private validateAndSetFile(file: File): void {
    this.fileError = '';
    if (!ALLOWED_TYPES.includes(file.type)) {
      this.fileError = 'Only PDF, PNG, JPG/JPEG files are allowed.';
      this.selectedFile = null;
      return;
    }
    if (file.size > MAX_SIZE_MB * 1024 * 1024) {
      this.fileError = `File size must not exceed ${MAX_SIZE_MB}MB.`;
      this.selectedFile = null;
      return;
    }
    this.selectedFile = file;
  }

  ngOnInit(): void {
    const isAdmin = (this.authService.getRole() ?? '').toLowerCase() === 'admin';
    const shipments$ = isAdmin ? this.shipmentService.getAllAdmin() : this.shipmentService.getAll();

    shipments$.subscribe({
      next: (data) => { this.shipments = data; },
      error: () => { this.notificationService.error('Failed to load shipments.'); }
    });
  }

  onFileChange(event: Event): void {
    this.fileError = '';
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (!file) { this.selectedFile = null; return; }
    this.validateAndSetFile(file);
  }

  onUpload(): void {
    if (!this.selectedShipmentId || !this.selectedFile || this.fileError) return;

    this.isUploading = true;
    const upload$ = this.getUploadObservable();

    upload$.subscribe({
      next: () => {
        this.notificationService.success('Document uploaded successfully.');
        this.isUploading = false;
        this.selectedFile = null;
        this.loadDocuments(this.selectedShipmentId);
      },
      error: () => {
        this.notificationService.error('Upload failed. Please try again.');
        this.isUploading = false;
      }
    });
  }

  private getUploadObservable() {
    const { selectedShipmentId: id, selectedFile: file, selectedDocType: type } = this;
    switch (type) {
      case 'Label':   return this.documentService.uploadLabel(id, file!);
      case 'Customs': return this.documentService.uploadCustoms(id, file!);
      default:        return this.documentService.upload(id, file!);
    }
  }

  loadDocuments(shipmentId: string): void {
    this.selectedShipmentId = shipmentId;
    this.currentPage = 1;
    this.fetchPage();
  }

  fetchPage(): void {
    if (!this.selectedShipmentId) return;
    this.documentService.getByShipment(this.selectedShipmentId, this.currentPage, this.pageSize).subscribe({
      next: (res) => {
        // Support both paginated response objects and plain arrays
        if (Array.isArray(res)) {
          this.documents = res;
          this.totalItems = res.length;
        } else {
          this.documents = res.items ?? res.data ?? [];
          this.totalItems = res.totalCount ?? res.total ?? this.documents.length;
        }
      },
      error: () => { this.notificationService.error('Failed to load documents.'); }
    });
  }

  downloadDocument(doc: any): void {
    const id = this.getDocumentId(doc);
    if (!id) {
      this.notificationService.error('Unable to download: missing document id.');
      return;
    }

    this.documentService.download(id).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) {
          this.notificationService.error('Document download failed.');
          return;
        }

        const fileName = this.extractFileName(response.headers.get('content-disposition'))
          ?? doc.fileName
          ?? doc.name
          ?? `document-${id}`;

        const url = window.URL.createObjectURL(blob);
        const link = window.document.createElement('a');
        link.href = url;
        link.download = fileName;
        window.document.body.appendChild(link);
        link.click();
        link.remove();
        window.URL.revokeObjectURL(url);
      },
      error: () => {
        this.notificationService.error('Failed to download document.');
      }
    });
  }

  deleteDocument(id: string): void {
    if (!confirm('Are you sure you want to delete this document?')) return;
    this.documentService.delete(id).subscribe({
      next: () => {
        this.notificationService.success('Document deleted.');
        this.fetchPage();
      },
      error: () => { this.notificationService.error('Failed to delete document.'); }
    });
  }

  get totalPages(): number {
    return Math.ceil(this.totalItems / this.pageSize);
  }

  get pages(): number[] {
    return Array.from({ length: this.totalPages }, (_, i) => i + 1);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.fetchPage();
    }
  }

  getDocumentId(doc: any): string {
    return doc?.id ?? doc?.documentId ?? doc?.DocumentId ?? '';
  }

  private extractFileName(contentDisposition: string | null): string | null {
    if (!contentDisposition) return null;

    const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
    if (utf8Match?.[1]) {
      return decodeURIComponent(utf8Match[1]);
    }

    const basicMatch = /filename="?([^";]+)"?/i.exec(contentDisposition);
    return basicMatch?.[1] ?? null;
  }
}
