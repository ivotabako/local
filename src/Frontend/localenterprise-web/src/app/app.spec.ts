import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { of } from 'rxjs';
import { App } from './app';
import { AuthService } from './services/auth.service';
import { CarsService } from './services/cars.service';

class AuthServiceStub {
  readonly token = () => null;
  readonly isAuthenticated = () => false;
  beginAuthorizationCodeLogin = vi.fn(async () => 'https://localhost:7081/connect/authorize?mock=true');
  completeAuthorizationCodeLogin = vi.fn(() => of({ access_token: 'token' }));
  getAuthorizationError = vi.fn(() => null);
  logout = vi.fn();
}

class CarsServiceStub {
  list = vi.fn(() => of([]));
  create = vi.fn();
  update = vi.fn();
  delete = vi.fn();
}

describe('App', () => {
  let authService: AuthServiceStub;

  beforeEach(async () => {
    authService = new AuthServiceStub();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideZonelessChangeDetection(),
        { provide: AuthService, useValue: authService },
        { provide: CarsService, useClass: CarsServiceStub }
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('LocalEnterprise Cars Platform');
  });

  it('should render sign-in action', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Authorization Code + PKCE');
  });

  it('should show an error when sign-in redirect initialization fails', async () => {
    authService.beginAuthorizationCodeLogin.mockRejectedValueOnce(new Error('nope'));

    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;

    await app['login'](new Event('submit'));
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.error')?.textContent).toContain('Sign-in redirect failed.');
  });
});
