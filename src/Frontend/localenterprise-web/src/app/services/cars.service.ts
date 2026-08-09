import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Car, UpsertCarRequest } from '../models/car';
import { apiConfig } from './api-config';

@Injectable({ providedIn: 'root' })
export class CarsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${apiConfig.backendBaseUrl}/api/cars`;

  list(): Observable<Car[]> {
    return this.http.get<Car[]>(this.baseUrl);
  }

  create(payload: UpsertCarRequest): Observable<Car> {
    return this.http.post<Car>(this.baseUrl, payload);
  }

  update(id: string, payload: UpsertCarRequest): Observable<Car> {
    return this.http.put<Car>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
