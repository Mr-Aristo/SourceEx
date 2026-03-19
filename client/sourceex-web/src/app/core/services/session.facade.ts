import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { GenerateTokenRequest, CurrentUserResponse } from '../models/auth.models';
import { AuthApiService } from './auth-api.service';
import { getHttpErrorMessage } from './http-error-message';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class SessionFacade {
  private readonly authApiService = inject(AuthApiService);
  private readonly tokenStorage = inject(TokenStorageService);

  readonly accessToken = signal<string | null>(this.tokenStorage.readAccessToken());
  readonly currentUser = signal<CurrentUserResponse | null>(null);
  readonly isBusy = signal(false);
  readonly lastError = signal<string | null>(null);

  readonly isAuthenticated = computed(() => !!this.accessToken());

  constructor() {
    if (this.accessToken()) {
      this.loadCurrentUser();
    }
  }

  issueDeveloperToken(request: GenerateTokenRequest): void {
    this.isBusy.set(true);
    this.lastError.set(null);

    this.authApiService.issueToken(request)
      .pipe(finalize(() => this.isBusy.set(false)))
      .subscribe({
        next: (response) => {
          this.tokenStorage.storeAccessToken(response.accessToken);
          this.accessToken.set(response.accessToken);
          this.loadCurrentUser();
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

  clearSession(): void {
    this.tokenStorage.clearAccessToken();
    this.accessToken.set(null);
    this.currentUser.set(null);
    this.lastError.set(null);
  }
}

