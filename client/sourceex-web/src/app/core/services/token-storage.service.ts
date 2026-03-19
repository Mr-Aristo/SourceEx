import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private static readonly AccessTokenStorageKey = 'sourceex.access_token';

  readAccessToken(): string | null {
    return localStorage.getItem(TokenStorageService.AccessTokenStorageKey);
  }

  storeAccessToken(accessToken: string): void {
    localStorage.setItem(TokenStorageService.AccessTokenStorageKey, accessToken);
  }

  clearAccessToken(): void {
    localStorage.removeItem(TokenStorageService.AccessTokenStorageKey);
  }
}

