import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

class AuthServiceStub {
  readonly tokenValue = signal<string | null>('issued-token');
  readonly token = this.tokenValue.asReadonly();
}

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useClass: AuthServiceStub }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
  });

  it('should attach the bearer token to backend requests', () => {
    httpClient.get('https://localhost:7243/api/cars').subscribe();

    const request = httpController.expectOne('https://localhost:7243/api/cars');
    expect(request.request.headers.get('Authorization')).toBe('Bearer issued-token');
    request.flush([]);
  });

  it('should not attach the bearer token to external requests', () => {
    httpClient.get('https://example.com/api/cars').subscribe();

    const request = httpController.expectOne('https://example.com/api/cars');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);
  });

  it('should not attach the bearer token to auth server endpoints', () => {
    httpClient.post('https://localhost:7081/connect/token', {}).subscribe();

    const request = httpController.expectOne('https://localhost:7081/connect/token');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('should attach the bearer token to auth API endpoints', () => {
    httpClient.get('https://localhost:7081/api/users/me').subscribe();

    const request = httpController.expectOne('https://localhost:7081/api/users/me');
    expect(request.request.headers.get('Authorization')).toBe('Bearer issued-token');
    request.flush({});
  });
});