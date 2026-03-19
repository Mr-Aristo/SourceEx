import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';
import {
  AuthTokenResponse,
  IdentityUserResponse,
  LoginRequest,
  RegisterUserRequest
} from '../models/auth.models';
import { AuthApiService } from './auth-api.service';
import { getHttpErrorMessage } from './http-error-message';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class SessionFacade {
  private readonly authApiService = inject(AuthApiService);
  private readonly tokenStorage = inject(TokenStorageService);

  readonly accessToken = signal<string | null>(this.tokenStorage.readAccessToken());
  readonly refreshToken = signal<string | null>(this.tokenStorage.readRefreshToken());
  readonly currentUser = signal<IdentityUserResponse | null>(null);
  readonly isBusy = signal(false);
  readonly lastError = signal<string | null>(null);
  readonly lastSuccessMessage = signal<string | null>(null);

  readonly isAuthenticated = computed(() => !!this.accessToken());

  constructor() {
    if (this.accessToken()) {
      this.loadCurrentUser();
    }
  }

  register(request: RegisterUserRequest): void {
    this.isBusy.set(true);
    this.lastError.set(null);
    this.lastSuccessMessage.set(null);

    this.authApiService.register(request)
      .pipe(finalize(() => this.isBusy.set(false)))
      .subscribe({
        next: (response) => {
          this.applyAuthResponse(response);
          this.lastSuccessMessage.set('Identity account registered and signed in.');
        },
        error: (error: unknown) => {
          this.lastError.set(getHttpErrorMessage(error));
        }
      });
  }

  login(request: LoginRequest): void {
    this.isBusy.set(true);
    this.lastError.set(null);
    this.lastSuccessMessage.set(null);

    this.authApiService.login(request)
      .pipe(finalize(() => this.isBusy.set(false)))
      .subscribe({
        next: (response) => {
          this.applyAuthResponse(response);
          this.lastSuccessMessage.set('Login completed successfully.');
        },
        error: (error: unknown) => {
          this.lastError.set(getHttpErrorMessage(error));
        }
      });
  }

  loadCurrentUser(): void {
    if (!this.accessToken()) {
      this.currentUser.set(null);
      return;
    }

    this.isBusy.set(true);
    this.lastError.set(null);

    this.authApiService.getCurrentUser()
      .pipe(finalize(() => this.isBusy.set(false)))
      .subscribe({
        next: (user) => this.currentUser.set(user),
        error: (error: unknown) => {
          this.lastError.set(getHttpErrorMessage(error));
        }
      });
  }

  refreshSession(): void {
    const refreshToken = this.refreshToken();

    if (!refreshToken) {
      this.lastError.set('There is no refresh token stored in the browser.');
      return;
    }

    this.isBusy.set(true);
    this.lastError.set(null);
    this.lastSuccessMessage.set(null);

    this.authApiService.refreshToken({ refreshToken })
      .pipe(finalize(() => this.isBusy.set(false)))
      .subscribe({
        next: (response) => {
          this.applyAuthResponse(response);
          this.lastSuccessMessage.set('Access token refreshed.');
        },
        error: (error: unknown) => {
          this.lastError.set(getHttpErrorMessage(error));
        }
      });
  }

  clearSession(): void {
    const refreshToken = this.refreshToken();

    if (refreshToken) {
      this.authApiService.logout({ refreshToken }).subscribe({
        error: () => {
        }
      });
    }

    this.tokenStorage.clearAll();
    this.accessToken.set(null);
    this.refreshToken.set(null);
    this.currentUser.set(null);
    this.lastError.set(null);
    this.lastSuccessMessage.set(null);
  }

  private applyAuthResponse(response: AuthTokenResponse): void {
    this.tokenStorage.storeAccessToken(response.accessToken);
    this.tokenStorage.storeRefreshToken(response.refreshToken);
    this.accessToken.set(response.accessToken);
    this.refreshToken.set(response.refreshToken);
    this.currentUser.set(response.user);
  }
}
