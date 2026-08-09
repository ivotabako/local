import { TestBed } from '@angular/core/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { AuthService } from './services/auth.service';

class AuthServiceStub {
  readonly token = () => null;
  readonly isAuthenticated = () => false;
  readonly currentUser = () => null;
  readonly isAdmin = () => false;
  readonly requiresPasswordChange = () => false;
  beginAuthorizationCodeLogin = vi.fn(async () => 'https://localhost:7081/connect/authorize?mock=true');
  logout = vi.fn();
  ensureCurrentUserLoaded = vi.fn(async () => true);
}

describe('App', () => {
  let authService: AuthServiceStub;

  beforeEach(async () => {
    authService = new AuthServiceStub();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideZonelessChangeDetection(),
        provideRouter([]),
        { provide: AuthService, useValue: authService },
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
    expect(compiled.querySelector('h1')?.textContent).toContain('LocalEnterprise Access Hub');
  });

  it('should render sign-in action', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Sign In');
  });

  it('should render overview navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Overview');
  });
});
