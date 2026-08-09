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
  private readonly pkceStateKey = 'localenterprise.auth.pkce.state';
  private readonly pkceVerifierKey = 'localenterprise.auth.pkce.verifier';

  readonly token = this.tokenValue.asReadonly();
  readonly isAuthenticated = computed(() => !!this.tokenValue());

  async beginAuthorizationCodeLogin(redirectUri: string): Promise<string> {
    const state = this.generateRandomString(32);
    const verifier = this.generateRandomString(64);
    const challenge = await this.generateCodeChallenge(verifier);

    sessionStorage.setItem(this.pkceStateKey, state);
    sessionStorage.setItem(this.pkceVerifierKey, verifier);

    const params = new URLSearchParams({
      client_id: apiConfig.authClientId,
      response_type: 'code',
      redirect_uri: redirectUri,
      scope: apiConfig.authScope,
      state,
      code_challenge: challenge,
      code_challenge_method: 'S256'
    });

    return `${apiConfig.authBaseUrl}/connect/authorize?${params.toString()}`;
  }

  completeAuthorizationCodeLogin(params: URLSearchParams, redirectUri: string): Observable<TokenResponse> {
    const code = params.get('code');
    const returnedState = params.get('state');
    if (!code || !returnedState) {
      throw new Error('Authorization code callback is missing required parameters.');
    }

    const expectedState = sessionStorage.getItem(this.pkceStateKey);
    const verifier = sessionStorage.getItem(this.pkceVerifierKey);
    if (!expectedState || !verifier || expectedState !== returnedState) {
      this.clearPkceArtifacts();
      throw new Error('PKCE state validation failed.');
    }

    this.clearPkceArtifacts();
    const body = new URLSearchParams({
      grant_type: 'authorization_code',
      code,
      redirect_uri: redirectUri,
      client_id: apiConfig.authClientId,
      code_verifier: verifier
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

  getAuthorizationError(params: URLSearchParams): string | null {
    return params.get('error_description') ?? params.get('error');
  }

  buildLogoutUrl(postLogoutRedirectUri: string): string {
    const params = new URLSearchParams({
      postLogoutRedirectUri
    });

    return `${apiConfig.authBaseUrl}/account/logout?${params.toString()}`;
  }

  logout(): void {
    this.tokenValue.set(null);
    this.clearPkceArtifacts();
  }

  private clearPkceArtifacts(): void {
    sessionStorage.removeItem(this.pkceStateKey);
    sessionStorage.removeItem(this.pkceVerifierKey);
  }

  private generateRandomString(byteLength: number): string {
    const values = new Uint8Array(byteLength);
    crypto.getRandomValues(values);
    return this.base64UrlEncode(values);
  }

  private async generateCodeChallenge(verifier: string): Promise<string> {
    const buffer = new TextEncoder().encode(verifier);
    const digest = await crypto.subtle.digest('SHA-256', buffer);
    return this.base64UrlEncode(new Uint8Array(digest));
  }

  private base64UrlEncode(bytes: Uint8Array): string {
    let binary = '';
    for (const value of bytes) {
      binary += String.fromCharCode(value);
    }

    return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
  }
}
