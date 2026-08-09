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

  it('should exchange authorization code, store the access token, and load the current user', () => {
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

    const meRequest = httpController.expectOne('https://localhost:7081/api/users/me');
    expect(meRequest.request.method).toBe('GET');
    meRequest.flush({
      id: 'user-1',
      username: 'apiadmin',
      roles: ['Admin'],
      createdAt: '2026-08-10T00:00:00Z',
      createdBy: 'Bootstrap',
      requiresPasswordChange: false,
      lastPasswordChangedAt: '2026-08-10T00:00:00Z',
      isLocked: false,
      twoFactorEnabled: false,
      recoveryCodesRemaining: 0
    });

    expect(payload?.access_token).toBe('issued-token');
    expect(service.token()).toBe('issued-token');
    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.username).toBe('apiadmin');
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

  it('should post password changes and update the current user state', () => {
    let username = '';

    service.changePassword({ currentPassword: 'OldPassword_1234!', newPassword: 'NewPassword_5678!' }).subscribe((user) => {
      username = user.username;
    });

    const request = httpController.expectOne('https://localhost:7081/api/users/change-password');
    expect(request.request.method).toBe('POST');
    request.flush({
      id: 'user-1',
      username: 'apiadmin',
      roles: ['Admin'],
      createdAt: '2026-08-10T00:00:00Z',
      createdBy: 'Bootstrap',
      requiresPasswordChange: false,
      lastPasswordChangedAt: '2026-08-10T00:10:00Z',
      isLocked: false,
      twoFactorEnabled: false,
      recoveryCodesRemaining: 0
    });

    expect(username).toBe('apiadmin');
    expect(service.currentUser()?.requiresPasswordChange).toBe(false);
  });

  it('should start and verify two-factor enrollment', () => {
    let recoveryCodes: string[] = [];

    service.beginTwoFactorEnrollment().subscribe();
    const enrollmentRequest = httpController.expectOne('https://localhost:7081/api/users/me/2fa/enrollment');
    expect(enrollmentRequest.request.method).toBe('POST');
    enrollmentRequest.flush({
      sharedSecret: 'SECRET',
      provisioningUri: 'otpauth://totp/example',
      twoFactorEnabled: false
    });

    service.verifyTwoFactor('123456').subscribe((result) => {
      recoveryCodes = result.recoveryCodes;
    });

    const verifyRequest = httpController.expectOne('https://localhost:7081/api/users/me/2fa/verify');
    expect(verifyRequest.request.method).toBe('POST');
    verifyRequest.flush({
      user: {
        id: 'user-1',
        username: 'apiadmin',
        roles: ['Admin'],
        createdAt: '2026-08-10T00:00:00Z',
        createdBy: 'Bootstrap',
        requiresPasswordChange: false,
        lastPasswordChangedAt: '2026-08-10T00:10:00Z',
        isLocked: false,
        twoFactorEnabled: true,
        recoveryCodesRemaining: 8
      },
      recoveryCodes: ['CODE-1', 'CODE-2']
    });

    expect(service.currentUser()?.twoFactorEnabled).toBe(true);
    expect(recoveryCodes).toEqual(['CODE-1', 'CODE-2']);
  });
});