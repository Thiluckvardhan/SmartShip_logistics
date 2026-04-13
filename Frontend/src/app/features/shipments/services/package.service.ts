import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreatePackageDto, UpdatePackageDto } from '../../../models/shipment.models';

@Injectable({ providedIn: 'root' })
export class PackageService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  create(shipmentId: string, dto: CreatePackageDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/shipments/${shipmentId}/packages`, dto);
  }

  getAll(shipmentId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/shipments/${shipmentId}/packages`);
  }

  update(shipmentId: string, packageId: string, dto: UpdatePackageDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/shipments/${shipmentId}/packages/${packageId}`, dto);
  }

  delete(shipmentId: string, packageId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/shipments/${shipmentId}/packages/${packageId}`);
  }
}
