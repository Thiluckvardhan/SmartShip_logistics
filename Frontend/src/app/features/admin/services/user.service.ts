import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { UpdateUserRoleDto } from '../../../models/auth.models';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getAll(page = 1, size = 5): Observable<any> {
    const params = new HttpParams().set('pageNumber', page).set('pageSize', size);
    return this.http.get<any>(`${this.apiUrl}/api/users`, { params });
  }

  getById(id: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/users/${id}`);
  }

  getByEmail(email: string): Observable<any> {
    const params = new HttpParams().set('email', email);
    return this.http.get(`${this.apiUrl}/api/users/by-email`, { params });
  }

  updateRole(id: string, dto: UpdateUserRoleDto): Observable<any> {
    return this.http.put(`${this.apiUrl}/api/users/${id}/role`, dto);
  }

  updateRoleByEmail(email: string, dto: UpdateUserRoleDto): Observable<any> {
    const params = new HttpParams().set('email', email);
    return this.http.put(`${this.apiUrl}/api/users/by-email/role`, dto, { params });
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/users/${id}`);
  }

  deleteByEmail(email: string): Observable<any> {
    const params = new HttpParams().set('email', email);
    return this.http.delete(`${this.apiUrl}/api/users/by-email`, { params });
  }
}
