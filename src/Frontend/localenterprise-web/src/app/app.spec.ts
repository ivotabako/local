import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { of, throwError } from 'rxjs';
import { App } from './app';
import { AuthService } from './services/auth.service';
import { CarsService } from './services/cars.service';

class AuthServiceStub {
  readonly token = () => null;
  readonly isAuthenticated = () => false;
  login = vi.fn(() => of({ access_token: 'token' }));
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

  it('should not prefill login credentials', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const inputs = compiled.querySelectorAll('input');

    expect(inputs[0]?.getAttribute('value') ?? '').toBe('');
    expect(inputs[1]?.getAttribute('value') ?? '').toBe('');
  });

  it('should show an error when authentication fails', async () => {
    authService.login.mockReturnValueOnce(throwError(() => new Error('nope')));

    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    app['loginForm'].username().value.set('apiadmin');
    app['loginForm'].password().value.set('bad-password');

    fixture.detectChanges();
    app['login'](new Event('submit'));
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.error')?.textContent).toContain('Authentication failed.');
  });
});
