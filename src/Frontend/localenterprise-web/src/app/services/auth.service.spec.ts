import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';

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
    httpController.verify();
  });

  it('should submit an OAuth password grant request and store the access token', () => {
    let payload: { access_token: string } | undefined;

    service.login('apiadmin', 'CorrectHorseBatteryStaple_123!').subscribe((result) => {
      payload = result;
    });

    const request = httpController.expectOne('https://localhost:7081/connect/token');
    expect(request.request.method).toBe('POST');
    expect(request.request.headers.get('Content-Type')).toBe('application/x-www-form-urlencoded');
    expect(request.request.body).toContain('grant_type=password');
    expect(request.request.body).toContain('username=apiadmin');
    expect(request.request.body).toContain('scope=localenterprise.api');

    request.flush({ access_token: 'issued-token' });

    expect(payload?.access_token).toBe('issued-token');
    expect(service.token()).toBe('issued-token');
    expect(service.isAuthenticated()).toBe(true);
  });
});