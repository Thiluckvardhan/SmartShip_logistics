import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /** POST /api/documents/upload (multipart) */
  upload(shipmentId: string, file: File): Observable<any> {
    const fd = new FormData();
    fd.append('ShipmentId', shipmentId);
    fd.append('File', file);
    return this.http.post(`${this.apiUrl}/api/documents/upload`, fd);
  }

  /** POST /api/documents/upload-invoice (multipart) */
  uploadInvoice(shipmentId: string, file: File): Observable<any> {
    const fd = new FormData();
    fd.append('ShipmentId', shipmentId);
    fd.append('File', file);
    return this.http.post(`${this.apiUrl}/api/documents/upload-invoice`, fd);
  }

  /** POST /api/documents/upload-label (multipart) */
  uploadLabel(shipmentId: string, file: File): Observable<any> {
    const fd = new FormData();
    fd.append('ShipmentId', shipmentId);
    fd.append('File', file);
    return this.http.post(`${this.apiUrl}/api/documents/upload-label`, fd);
  }

  /** POST /api/documents/upload-customs (multipart) */
  uploadCustoms(shipmentId: string, file: File): Observable<any> {
    const fd = new FormData();
    fd.append('ShipmentId', shipmentId);
    fd.append('File', file);
    return this.http.post(`${this.apiUrl}/api/documents/upload-customs`, fd);
  }

  /** GET /api/documents/{id} */
  getById(id: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/documents/${id}`);
  }

  /** GET /api/documents/{id}/download */
  download(id: string): Observable<HttpResponse<Blob>> {
    return this.http.get(`${this.apiUrl}/api/documents/${id}/download`, {
      responseType: 'blob',
      observe: 'response'
    });
  }

  /** PUT /api/documents/{id} (multipart) */
  update(id: string, shipmentId: string, file?: File): Observable<any> {
    const fd = new FormData();
    fd.append('ShipmentId', shipmentId);
    if (file) {
      fd.append('File', file);
    }
    return this.http.put(`${this.apiUrl}/api/documents/${id}`, fd);
  }

  /** DELETE /api/documents/{id} */
  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/documents/${id}`);
  }

  /** GET /api/documents/shipment/{shipmentId}?pageNumber=&pageSize= */
  getByShipment(shipmentId: string, page = 1, size = 5): Observable<any> {
    const params = new HttpParams()
      .set('pageNumber', page.toString())
      .set('pageSize', size.toString());
    return this.http.get(`${this.apiUrl}/api/documents/shipment/${shipmentId}`, { params });
  }

  /** GET /api/documents/customer/{customerId} */
  getByCustomer(customerId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/documents/customer/${customerId}`);
  }

  /** POST /api/documents/delivery-proof/{shipmentId} (multipart) */
  createDeliveryProof(
    shipmentId: string,
    signerName: string,
    notes: string | undefined,
    file: File
  ): Observable<any> {
    const fd = new FormData();
    fd.append('SignerName', signerName);
    if (notes) {
      fd.append('Notes', notes);
    }
    fd.append('File', file);
    return this.http.post(`${this.apiUrl}/api/documents/delivery-proof/${shipmentId}`, fd);
  }

  /** GET /api/documents/delivery-proof/{shipmentId} */
  getDeliveryProof(shipmentId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/documents/delivery-proof/${shipmentId}`);
  }
}
