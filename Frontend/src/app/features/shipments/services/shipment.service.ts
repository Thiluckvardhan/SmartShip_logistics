import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateShipmentDto,
  UpdateShipmentDto,
  UpdateShipmentStatusDto,
  CalculateRateDto
} from '../../../models/shipment.models';

@Injectable({ providedIn: 'root' })
export class ShipmentService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  create(dto: CreateShipmentDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/shipments`, dto);
  }

  getAll(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/shipments`);
  }

  getById(id: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/shipments/${id}`);
  }

  getByTrackingNumber(tn: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/shipments/track/${tn}`);
  }

  update(id: string, dto: UpdateShipmentDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/shipments/${id}`, dto);
  }

  book(id: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/shipments/${id}/book`, {});
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/shipments/${id}`);
  }

  calculateRate(dto: CalculateRateDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/shipments/calculate-rate`, dto);
  }

  getAllAdmin(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/shipments/all`);
  }

  getStats(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/shipments/stats`);
  }

  updateStatus(id: string, dto: UpdateShipmentStatusDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/shipments/${id}/status`, dto);
  }

  markPickedUp(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/shipments/${id}/pickup`, {});
  }

  markInTransit(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/shipments/${id}/in-transit`, {});
  }

  markOutForDelivery(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/shipments/${id}/out-for-delivery`, {});
  }

  markDelivered(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/shipments/${id}/delivered`, {});
  }

  markDelayed(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/shipments/${id}/delay`, {});
  }

  markReturned(id: string): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/shipments/${id}/return`, {});
  }
}
