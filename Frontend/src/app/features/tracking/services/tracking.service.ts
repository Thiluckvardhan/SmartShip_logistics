import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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

  /** GET /api/tracking/{tn} */
  getByTrackingNumber(tn: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/tracking/${tn}`);
  }

  /** GET /api/tracking/{tn}/timeline */
  getTimeline(tn: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/tracking/${tn}/timeline`);
  }

  /** GET /api/tracking/{tn}/events */
  getEvents(tn: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/tracking/${tn}/events`);
  }

  /** GET /api/tracking/location/{tn} */
  getCurrentLocation(tn: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/tracking/location/${tn}`);
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
