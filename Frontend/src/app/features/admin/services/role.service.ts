import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreateRoleDto } from '../../../models/auth.models';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  getAll(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/api/roles`);
  }

  getById(id: number): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/roles/${id}`);
  }

  create(dto: CreateRoleDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/roles`, dto);
  }

  delete(id: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/api/roles/${id}`);
  }
}
