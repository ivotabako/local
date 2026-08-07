import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Car, UpsertCarRequest } from '../models/car';
import { apiConfig } from './api-config';

@Injectable({ providedIn: 'root' })
export class CarsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${apiConfig.backendBaseUrl}/api/cars`;

  list() {
    return this.http.get<Car[]>(this.baseUrl);
  }

  create(payload: UpsertCarRequest) {
    return this.http.post<Car>(this.baseUrl, payload);
  }

  update(id: string, payload: UpsertCarRequest) {
    return this.http.put<Car>(`${this.baseUrl}/${id}`, payload);
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
