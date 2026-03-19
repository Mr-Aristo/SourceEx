import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { buildVersionedApiUrl, injectSourceExApiConfig } from '../config/api.config';
import { AccessTokenResponse, CurrentUserResponse, GenerateTokenRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiConfig = injectSourceExApiConfig();

  issueToken(request: GenerateTokenRequest): Observable<AccessTokenResponse> {
    return this.httpClient.post<AccessTokenResponse>(
      buildVersionedApiUrl(this.apiConfig, 'auth/token'),
      request);
  }

  getCurrentUser(): Observable<CurrentUserResponse> {
    return this.httpClient.get<CurrentUserResponse>(
      buildVersionedApiUrl(this.apiConfig, 'auth/me'));
  }
}

