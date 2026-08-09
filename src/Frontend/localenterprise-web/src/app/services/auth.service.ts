import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map } from 'rxjs';
import { apiConfig } from './api-config';

interface TokenResponse {
  access_token: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenValue = signal<string | null>(null);

  readonly token = this.tokenValue.asReadonly();
  readonly isAuthenticated = computed(() => !!this.tokenValue());

  login(username: string, password: string): Observable<TokenResponse> {
    const body = new URLSearchParams({
      grant_type: 'password',
      username,
      password,
      scope: 'localenterprise.api'
    });

    return this.http
      .post<TokenResponse>(`${apiConfig.authBaseUrl}/connect/token`, body.toString(), {
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded'
        }
      })
      .pipe(
        map((result) => {
          this.tokenValue.set(result.access_token);
          return result;
        })
      );
  }

  logout(): void {
    this.tokenValue.set(null);
  }
}
