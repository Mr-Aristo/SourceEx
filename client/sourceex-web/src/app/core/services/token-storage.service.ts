import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private static readonly AccessTokenStorageKey = 'sourceex.access_token';
  private static readonly RefreshTokenStorageKey = 'sourceex.refresh_token';

  readAccessToken(): string | null {
    return localStorage.getItem(TokenStorageService.AccessTokenStorageKey);
  }

  storeAccessToken(accessToken: string): void {
    localStorage.setItem(TokenStorageService.AccessTokenStorageKey, accessToken);
  }

  readRefreshToken(): string | null {
    return localStorage.getItem(TokenStorageService.RefreshTokenStorageKey);
  }

  storeRefreshToken(refreshToken: string): void {
    localStorage.setItem(TokenStorageService.RefreshTokenStorageKey, refreshToken);
  }

  clearAccessToken(): void {
    localStorage.removeItem(TokenStorageService.AccessTokenStorageKey);
  }

  clearRefreshToken(): void {
    localStorage.removeItem(TokenStorageService.RefreshTokenStorageKey);
  }

  clearAll(): void {
    this.clearAccessToken();
    this.clearRefreshToken();
  }
}
