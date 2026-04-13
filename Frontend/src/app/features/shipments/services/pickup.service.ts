import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreatePickupDto, UpdatePickupDto } from '../../../models/shipment.models';

@Injectable({ providedIn: 'root' })
export class PickupService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  create(dto: CreatePickupDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/pickups`, dto);
  }

  getAll(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/pickups`);
  }

  getByShipment(shipmentId: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/pickups/${shipmentId}`);
  }

  update(shipmentId: string, dto: UpdatePickupDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/pickups/${shipmentId}`, dto);
  }
}
