import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { buildVersionedApiUrl, injectSourceExApiConfig } from '../config/api.config';
import {
  AuthTokenResponse,
  IdentityUserResponse,
  LoginRequest,
  LogoutRequest,
  RefreshTokenRequest,
  RegisterUserRequest
} from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiConfig = injectSourceExApiConfig();

  register(request: RegisterUserRequest): Observable<AuthTokenResponse> {
    return this.httpClient.post<AuthTokenResponse>(
      buildVersionedApiUrl(this.apiConfig, 'identity/auth/register'),
      request);
  }

  login(request: LoginRequest): Observable<AuthTokenResponse> {
    return this.httpClient.post<AuthTokenResponse>(
      buildVersionedApiUrl(this.apiConfig, 'identity/auth/login'),
      request);
  }

  refreshToken(request: RefreshTokenRequest): Observable<AuthTokenResponse> {
    return this.httpClient.post<AuthTokenResponse>(
      buildVersionedApiUrl(this.apiConfig, 'identity/auth/refresh'),
      request);
  }

  logout(request: LogoutRequest): Observable<void> {
    return this.httpClient.post<void>(
      buildVersionedApiUrl(this.apiConfig, 'identity/auth/logout'),
      request);
  }

  getCurrentUser(): Observable<IdentityUserResponse> {
    return this.httpClient.get<IdentityUserResponse>(
      buildVersionedApiUrl(this.apiConfig, 'identity/auth/me'));
  }
}
