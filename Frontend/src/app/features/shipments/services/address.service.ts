import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreateAddressDto } from '../../../models/shipment.models';

@Injectable({ providedIn: 'root' })
export class AddressService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  create(dto: CreateAddressDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/api/addresses`, dto);
  }

  getById(id: string): Observable<any> {
    return this.http.get(`${this.apiUrl}/api/addresses/${id}`);
  }
}
