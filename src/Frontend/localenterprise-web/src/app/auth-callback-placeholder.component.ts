import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-auth-callback-placeholder',
  standalone: true,
  template: `
    <section class="callback-state">
      <h2>Completing sign-in...</h2>
      @if (errorMessage()) {
        <p class="error">{{ errorMessage() }}</p>
      }
    </section>
  `,
  styles: [
    `
      .callback-state { width: min(34rem, 100%); margin: 6rem auto 0; text-align: center; }
      .error { color: #8d1f1f; font-weight: 600; }
    `
  ]
})
export class AuthCallbackPlaceholderComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly errorMessage = signal('');

  constructor() {
    this.finishSignIn();
  }

  private finishSignIn(): void {
    const currentUrl = new URL(window.location.href);
    const error = this.authService.getAuthorizationError(currentUrl.searchParams);
    if (error) {
      this.errorMessage.set(`Authentication failed: ${error}`);
      return;
    }

    const redirectUri = `${window.location.origin}${currentUrl.pathname}`;
    this.authService
      .completeAuthorizationCodeLogin(currentUrl.searchParams, redirectUri)
      .pipe(finalize(() => undefined))
      .subscribe({
        next: () => {
          const target = this.authService.requiresPasswordChange() ? '/account/password' : '/cars';
          void this.router.navigateByUrl(target);
        },
        error: () => {
          this.errorMessage.set('Authentication failed during token exchange.');
        }
      });
  }
}
