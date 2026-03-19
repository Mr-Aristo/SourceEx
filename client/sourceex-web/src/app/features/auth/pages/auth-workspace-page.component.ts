import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { JsonPipe } from '@angular/common';
import { SessionFacade } from '../../../core/services/session.facade';

@Component({
  selector: 'sx-auth-workspace-page',
  standalone: true,
  imports: [JsonPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel stack">
      <div>
        <p class="status">Auth workspace</p>
        <h2>Identity module integration</h2>
        <p class="muted">
          SourceEx now uses a separate identity module for login, password hashing, refresh tokens,
          and role-aware accounts. This page keeps the UI minimal while the final product interface is still undefined.
        </p>
      </div>

      <div class="inline-actions">
        <button type="button" (click)="loginAsEmployee()">Login as seeded employee</button>
        <button type="button" (click)="loginAsManager()">Login as seeded manager</button>
        <button type="button" (click)="registerSampleEmployee()">Register sample employee</button>
        <button type="button" (click)="refreshSession()">Refresh session</button>
        <button type="button" (click)="refreshProfile()">Load current user</button>
        <button type="button" (click)="clearSession()">Clear token</button>
      </div>
    </section>

    <section class="panel">
      <h3>Future UI notes</h3>
      <p class="muted">
        Replace the buttons above with real login, registration, forgot-password, and profile flows.
      </p>

      <!--
        Planned mapping:
        - registration form -> RegisterUserRequest
        - login form -> LoginRequest
        - refresh token lifecycle -> SessionFacade + TokenStorageService
        - SessionFacade currentUser -> top-bar identity chip / side navigation
      -->
    </section>

    <section class="panel">
      <h3>Seeded local accounts</h3>
      <p class="muted">
        Default local password:
        <span class="mono">Passw0rd!</span>
      </p>
      <pre><code>{{ seededAccounts | json }}</code></pre>
    </section>

    <section class="panel">
      <h3>Current session state</h3>
      <p class="muted">Useful while the real interface is still undecided.</p>
      <pre><code>{{ session.currentUser() | json }}</code></pre>
    </section>

    @if (session.lastSuccessMessage()) {
      <section class="panel">
        <h3>Last success</h3>
        <p>{{ session.lastSuccessMessage() }}</p>
      </section>
    }

    @if (session.lastError()) {
      <section class="panel">
        <h3>Last error</h3>
        <p>{{ session.lastError() }}</p>
      </section>
    }
  `
})
export class AuthWorkspacePageComponent {
  protected readonly session = inject(SessionFacade);
  protected readonly seededAccounts = [
    { userNameOrEmail: 'employee-001', password: 'Passw0rd!', role: 'employee', departmentId: 'operations' },
    { userNameOrEmail: 'manager-001', password: 'Passw0rd!', role: 'manager', departmentId: 'operations' },
    { userNameOrEmail: 'finance-001', password: 'Passw0rd!', role: 'finance', departmentId: 'finance' },
    { userNameOrEmail: 'admin-001', password: 'Passw0rd!', role: 'admin', departmentId: 'operations' }
  ];

  protected loginAsEmployee(): void {
    this.session.login({
      userNameOrEmail: 'employee-001',
      password: 'Passw0rd!'
    });
  }

  protected loginAsManager(): void {
    this.session.login({
      userNameOrEmail: 'manager-001',
      password: 'Passw0rd!'
    });
  }

  protected registerSampleEmployee(): void {
    this.session.register({
      userName: 'employee-demo',
      email: 'employee-demo@sourceex.local',
      password: 'Passw0rd!',
      displayName: 'Employee Demo',
      departmentId: 'operations'
    });
  }

  protected refreshProfile(): void {
    this.session.loadCurrentUser();
  }

  protected refreshSession(): void {
    this.session.refreshSession();
  }

  protected clearSession(): void {
    this.session.clearSession();
  }
}
