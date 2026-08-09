import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { signal } from '@angular/core';
import { AuthService } from './auth.service';
import { adminGuard, authGuard, passwordChangeGuard } from './auth.guards';

class AuthServiceStub {
  readonly authenticated = signal(false);
  readonly admin = signal(false);
  readonly passwordChangeRequired = signal(false);
  ensureCurrentUserLoaded = vi.fn(async () => true);

  readonly isAuthenticated = this.authenticated.asReadonly();
  readonly isAdmin = this.admin.asReadonly();
  readonly requiresPasswordChange = this.passwordChangeRequired.asReadonly();
}

describe('auth guards', () => {
  let authService: AuthServiceStub;
  let router: Router;

  beforeEach(() => {
    authService = new AuthServiceStub();

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: authService }]
    });

    router = TestBed.inject(Router);
  });

  it('redirects unauthenticated users to the landing page', async () => {
    const result = await TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

    expect(result instanceof UrlTree).toBe(true);
    expect(router.serializeUrl(result as UrlTree)).toBe('/');
  });

  it('redirects authenticated users with pending password change to the password route', async () => {
    authService.authenticated.set(true);
    authService.passwordChangeRequired.set(true);

    const result = await TestBed.runInInjectionContext(() => passwordChangeGuard({} as never, {} as never));

    expect(result instanceof UrlTree).toBe(true);
    expect(router.serializeUrl(result as UrlTree)).toBe('/account/password');
  });

  it('redirects non-admin users away from the admin route', async () => {
    authService.authenticated.set(true);

    const result = await TestBed.runInInjectionContext(() => adminGuard({} as never, {} as never));

    expect(result instanceof UrlTree).toBe(true);
    expect(router.serializeUrl(result as UrlTree)).toBe('/account');
  });

  it('allows admins through the admin guard', async () => {
    authService.authenticated.set(true);
    authService.admin.set(true);

    const result = await TestBed.runInInjectionContext(() => adminGuard({} as never, {} as never));

    expect(result).toBe(true);
  });
});