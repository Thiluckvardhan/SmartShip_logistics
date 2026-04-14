import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  CreateTrackingEventDto,
  UpdateTrackingEventDto,
  UpdateTrackingStatusDto,
  CreateTrackingLocationDto
} from '../../../models/tracking.models';

@Injectable({ providedIn: 'root' })
export class TrackingService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  private getRequestOptions(silent = false): { headers?: HttpHeaders } {
    if (!silent) return {};

    return {
      headers: new HttpHeaders({ 'X-Skip-Error-Toast': 'true' })
    };
  }

  /** GET /api/tracking/{tn} */
  getByTrackingNumber(tn: string, silent = false): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/tracking/${tn}`, this.getRequestOptions(silent));
  }

  /** GET /api/tracking/{tn}/timeline */
  getTimeline(tn: string, silent = false): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/tracking/${tn}/timeline`, this.getRequestOptions(silent));
  }

  /** GET /api/tracking/{tn}/events */
  getEvents(tn: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/tracking/${tn}/events`);
  }

  /** GET /api/tracking/location/{tn} */
  getCurrentLocation(tn: string, silent = false): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/tracking/location/${tn}`, this.getRequestOptions(silent));
  }

  /** GET /api/tracking/{tn}/status */
  getStatus(tn: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/tracking/${tn}/status`);
  }

  /** POST /api/tracking/events */
  createEvent(dto: CreateTrackingEventDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/tracking/events`, dto);
  }

  /** PUT /api/tracking/events/{id} */
  updateEvent(id: string, dto: UpdateTrackingEventDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/tracking/events/${id}`, dto);
  }

  /** DELETE /api/tracking/events/{id} */
  deleteEvent(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/tracking/events/${id}`);
  }

  /** POST /api/tracking/location */
  createLocation(dto: CreateTrackingLocationDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/tracking/location`, dto);
  }

  /** PUT /api/tracking/{tn}/status */
  updateStatus(tn: string, dto: UpdateTrackingStatusDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/tracking/${tn}/status`, dto);
  }
}
