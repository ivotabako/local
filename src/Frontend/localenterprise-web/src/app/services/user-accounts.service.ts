import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateUserAccountRequest, ResetUserPasswordRequest, UpdateUserAccountRequest, UserAccount } from '../models/user-account';
import { apiConfig } from './api-config';

@Injectable({ providedIn: 'root' })
export class UserAccountsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${apiConfig.authBaseUrl}/api/users`;

  list(): Observable<UserAccount[]> {
    return this.http.get<UserAccount[]>(`${this.baseUrl}/`);
  }

  create(request: CreateUserAccountRequest): Observable<UserAccount> {
    return this.http.post<UserAccount>(`${this.baseUrl}/`, request);
  }

  update(id: string, request: UpdateUserAccountRequest): Observable<UserAccount> {
    return this.http.put<UserAccount>(`${this.baseUrl}/${id}`, request);
  }

  resetPassword(id: string, request: ResetUserPasswordRequest): Observable<UserAccount> {
    return this.http.post<UserAccount>(`${this.baseUrl}/${id}/reset-password`, request);
  }

  lock(id: string): Observable<UserAccount> {
    return this.http.post<UserAccount>(`${this.baseUrl}/${id}/lock`, {});
  }

  unlock(id: string): Observable<UserAccount> {
    return this.http.post<UserAccount>(`${this.baseUrl}/${id}/unlock`, {});
  }

  resetTwoFactor(id: string): Observable<UserAccount> {
    return this.http.post<UserAccount>(`${this.baseUrl}/${id}/reset-2fa`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}