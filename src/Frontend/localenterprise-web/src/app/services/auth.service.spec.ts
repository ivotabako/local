import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { apiConfig } from './api-config';

describe('AuthService', () => {
  let service: AuthService;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), AuthService]
    });

    service = TestBed.inject(AuthService);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    sessionStorage.clear();
    httpController.verify();
  });

  it('should exchange authorization code with PKCE verifier and store the access token', () => {
    let payload: { access_token: string } | undefined;
    const redirectUri = `https://localhost:4200${apiConfig.authRedirectPath}`;
    const params = new URLSearchParams({
      code: 'auth-code-123',
      state: 'state-123'
    });

    sessionStorage.setItem('localenterprise.auth.pkce.state', 'state-123');
    sessionStorage.setItem('localenterprise.auth.pkce.verifier', 'verifier-456');

    service.completeAuthorizationCodeLogin(params, redirectUri).subscribe((result) => {
      payload = result;
    });

    const request = httpController.expectOne('https://localhost:7081/connect/token');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Content-Type')).toBe('application/x-www-form-urlencoded');
    expect(request.request.body).toContain('grant_type=authorization_code');
    expect(request.request.body).toContain('client_id=localenterprise-web');
    expect(request.request.body).toContain('code=auth-code-123');
    expect(request.request.body).toContain('code_verifier=verifier-456');

    request.flush({ access_token: 'issued-token' });

    expect(payload?.access_token).toBe('issued-token');
    expect(service.token()).toBe('issued-token');
    expect(service.isAuthenticated()).toBe(true);
  });

  it('should throw when callback state validation fails', () => {
    const redirectUri = `https://localhost:4200${apiConfig.authRedirectPath}`;
    const params = new URLSearchParams({
      code: 'auth-code-123',
      state: 'wrong-state'
    });

    sessionStorage.setItem('localenterprise.auth.pkce.state', 'expected-state');
    sessionStorage.setItem('localenterprise.auth.pkce.verifier', 'verifier-456');

    expect(() => service.completeAuthorizationCodeLogin(params, redirectUri)).toThrowError('PKCE state validation failed.');
  });

  it('should build logout URL with encoded post-logout redirect', () => {
    const url = service.buildLogoutUrl('https://localhost:4200');

    expect(url).toBe('https://localhost:7081/account/logout?postLogoutRedirectUri=https%3A%2F%2Flocalhost%3A4200');
  });
});