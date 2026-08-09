import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, firstValueFrom, map, of, switchMap, tap } from 'rxjs';
import { apiConfig } from './api-config';
import { ChangePasswordRequest, TwoFactorEnrollment, TwoFactorVerificationResult, UserAccount } from '../models/user-account';

interface TokenResponse {
  access_token: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenValue = signal<string | null>(null);
  private readonly currentUserValue = signal<UserAccount | null>(null);
  private readonly currentUserLoadedValue = signal(false);
  private readonly pkceStateKey = 'localenterprise.auth.pkce.state';
  private readonly pkceVerifierKey = 'localenterprise.auth.pkce.verifier';

  readonly token = this.tokenValue.asReadonly();
  readonly isAuthenticated = computed(() => !!this.tokenValue());
  readonly currentUser = this.currentUserValue.asReadonly();
  readonly currentUserLoaded = this.currentUserLoadedValue.asReadonly();
  readonly isAdmin = computed(() => this.currentUser()?.roles.includes('Admin') ?? false);
  readonly requiresPasswordChange = computed(() => this.currentUser()?.requiresPasswordChange ?? false);

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
        tap((result) => {
          this.tokenValue.set(result.access_token);
        }),
        switchMap((result) => this.loadCurrentUser().pipe(map(() => result)))
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
    this.currentUserValue.set(null);
    this.currentUserLoadedValue.set(false);
    this.clearPkceArtifacts();
  }

  loadCurrentUser(): Observable<UserAccount> {
    return this.http.get<UserAccount>(`${apiConfig.authBaseUrl}/api/users/me`).pipe(
      tap((user) => {
        this.currentUserValue.set(user);
        this.currentUserLoadedValue.set(true);
      })
    );
  }

  async ensureCurrentUserLoaded(): Promise<boolean> {
    if (!this.isAuthenticated()) {
      return false;
    }

    if (this.currentUserLoadedValue() && this.currentUserValue() !== null) {
      return true;
    }

    return firstValueFrom(
      this.loadCurrentUser().pipe(
        map(() => true),
        catchError(() => {
          this.logout();
          return of(false);
        })
      )
    );
  }

  changePassword(request: ChangePasswordRequest): Observable<UserAccount> {
    return this.http.post<UserAccount>(`${apiConfig.authBaseUrl}/api/users/change-password`, request).pipe(
      tap((user) => {
        this.currentUserValue.set(user);
        this.currentUserLoadedValue.set(true);
      })
    );
  }

  beginTwoFactorEnrollment(): Observable<TwoFactorEnrollment> {
    return this.http.post<TwoFactorEnrollment>(`${apiConfig.authBaseUrl}/api/users/me/2fa/enrollment`, {});
  }

  verifyTwoFactor(code: string): Observable<TwoFactorVerificationResult> {
    return this.http.post<TwoFactorVerificationResult>(`${apiConfig.authBaseUrl}/api/users/me/2fa/verify`, { code }).pipe(
      tap((result) => {
        this.currentUserValue.set(result.user);
        this.currentUserLoadedValue.set(true);
      })
    );
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
