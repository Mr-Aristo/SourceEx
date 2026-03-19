import { HttpInterceptorFn } from '@angular/common/http';
import { getApiVersionHeaderValue, injectSourceExApiConfig, isSourceExApiRequest } from '../config/api.config';

export const apiVersionInterceptor: HttpInterceptorFn = (request, next) => {
  const apiConfig = injectSourceExApiConfig();

  if (!isSourceExApiRequest(request.url, apiConfig)) {
    return next(request);
  }

  return next(request.clone({
    setHeaders: {
      'x-api-version': getApiVersionHeaderValue(apiConfig)
    }
  }));
};
