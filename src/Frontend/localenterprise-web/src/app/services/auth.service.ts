import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { map } from 'rxjs';
import { apiConfig } from './api-config';

interface TokenResponse {
  access_token: string;
  token_type: string;
  expires_in: number;
  scope: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenValue = signal<string | null>(null);

  readonly token = this.tokenValue.asReadonly();
  readonly isAuthenticated = computed(() => !!this.tokenValue());

  login(username: string, password: string) {
    return this.http
      .post<TokenResponse>(`${apiConfig.authBaseUrl}/connect/token`, { username, password })
      .pipe(
        map((result) => {
          this.tokenValue.set(result.access_token);
          return result;
        })
      );
  }

  logout() {
    this.tokenValue.set(null);
  }
}
