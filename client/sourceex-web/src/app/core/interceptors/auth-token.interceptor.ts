import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { injectSourceExApiConfig, isSourceExApiRequest } from '../config/api.config';
import { TokenStorageService } from '../services/token-storage.service';

export const authTokenInterceptor: HttpInterceptorFn = (request, next) => {
  const tokenStorage = inject(TokenStorageService);
  const apiConfig = injectSourceExApiConfig();
  const accessToken = tokenStorage.readAccessToken();

  if (!accessToken || !isSourceExApiRequest(request.url, apiConfig)) {
    return next(request);
  }

  return next(request.clone({
    setHeaders: {
      Authorization: `Bearer ${accessToken}`
    }
  }));
};
