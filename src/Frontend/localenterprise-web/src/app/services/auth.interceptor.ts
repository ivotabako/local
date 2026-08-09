import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from './auth.service';
import { apiConfig } from './api-config';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.token();
  const isBackendRequest = req.url.startsWith(apiConfig.backendBaseUrl);
  const isAuthApiRequest = req.url.startsWith(`${apiConfig.authBaseUrl}/api/`);

  if (!token || (!isBackendRequest && !isAuthApiRequest)) {
    return next(req);
  }

  return next(
    req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    })
  );
};
