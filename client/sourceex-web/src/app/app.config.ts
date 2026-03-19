import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { environment } from '../environments/environment';
import { appRoutes } from './app.routes';
import { SOURCEEX_API_CONFIG } from './core/config/api.config';
import { apiVersionInterceptor } from './core/interceptors/api-version.interceptor';
import { authTokenInterceptor } from './core/interceptors/auth-token.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(appRoutes),
    provideHttpClient(withFetch(), withInterceptors([apiVersionInterceptor, authTokenInterceptor])),
    {
      provide: SOURCEEX_API_CONFIG,
      useValue: environment.api
    }
  ]
};

