import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';

async function ensureUserLoaded(): Promise<boolean> {
  const authService = inject(AuthService);
  return authService.ensureCurrentUserLoaded();
}

export const authGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return router.createUrlTree(['/']);
  }

  return (await ensureUserLoaded()) ? true : router.createUrlTree(['/']);
};

export const passwordChangeGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!(await ensureUserLoaded())) {
    return router.createUrlTree(['/']);
  }

  return authService.requiresPasswordChange() ? router.createUrlTree(['/account/password']) : true;
};

export const adminGuard: CanActivateFn = async () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!(await ensureUserLoaded())) {
    return router.createUrlTree(['/']);
  }

  return authService.isAdmin() ? true : router.createUrlTree(['/account']);
};