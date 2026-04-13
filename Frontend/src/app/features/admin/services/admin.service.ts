import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateHubDto,
  UpdateHubDto,
  CreateServiceLocationDto,
  UpdateServiceLocationDto,
  ResolveShipmentDto,
  DelayShipmentDto,
  ReturnShipmentDto
} from '../../../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getDashboard(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/admin/dashboard`);
  }

  getStatistics(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/admin/statistics`);
  }

  getHubs(page = 1, size = 5): Observable<any> {
    const params = new HttpParams().set('pageNumber', page).set('pageSize', size);
    return this.http.get(`${this.apiUrl}/api/admin/hubs`, { params });
  }

  createHub(dto: CreateHubDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/admin/hubs`, dto);
  }

  getHub(id: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/admin/hubs/${id}`);
  }

  updateHub(id: string, dto: UpdateHubDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/admin/hubs/${id}`, dto);
  }

  deleteHub(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/admin/hubs/${id}`);
  }

  getLocations(page = 1, size = 5): Observable<any> {
    const params = new HttpParams().set('pageNumber', page).set('pageSize', size);
    return this.http.get(`${this.apiUrl}/api/admin/locations`, { params });
  }

  createLocation(dto: CreateServiceLocationDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/admin/locations`, dto);
  }

  updateLocation(id: string, dto: UpdateServiceLocationDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/admin/locations/${id}`, dto);
  }

  deleteLocation(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/admin/locations/${id}`);
  }

  getExceptions(page = 1, size = 5): Observable<any> {
    const params = new HttpParams().set('pageNumber', page).set('pageSize', size);
    return this.http.get(`${this.apiUrl}/api/admin/exceptions`, { params });
  }

  resolveShipment(id: string, dto: ResolveShipmentDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/admin/shipments/${id}/resolve`, dto);
  }

  delayShipment(id: string, dto: DelayShipmentDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/admin/shipments/${id}/delay`, dto);
  }

  returnShipment(id: string, dto: ReturnShipmentDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/admin/shipments/${id}/return`, dto);
  }

  getShipments(page = 1, size = 5): Observable<any> {
    const params = new HttpParams().set('pageNumber', page).set('pageSize', size);
    return this.http.get(`${this.apiUrl}/api/admin/shipments`, { params });
  }

  getShipment(id: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/admin/shipments/${id}`);
  }

  getShipmentsByHub(hubId: string, page = 1, size = 5): Observable<any> {
    const params = new HttpParams().set('pageNumber', page).set('pageSize', size);
    return this.http.get(`${this.apiUrl}/api/admin/shipments/hub/${hubId}`, { params });
  }

  getReports(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/admin/reports`);
  }

  getShipmentPerformanceReport(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/admin/reports/shipment-performance`);
  }

  getDeliverySlaReport(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/admin/reports/delivery-sla`);
  }

  getRevenueReport(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/admin/reports/revenue`);
  }

  getHubPerformanceReport(): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/admin/reports/hub-performance`);
  }
}
