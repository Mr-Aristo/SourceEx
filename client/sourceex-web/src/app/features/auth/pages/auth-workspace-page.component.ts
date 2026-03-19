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
        <h2>Local JWT integration</h2>
        <p class="muted">
          The backend exposes a development-friendly token endpoint. This page keeps the UI minimal and proves
          the client-to-API authentication path before the final login experience is designed.
        </p>
      </div>

      <div class="inline-actions">
        <button type="button" (click)="issueEmployeeToken()">Issue employee token</button>
        <button type="button" (click)="issueApproverToken()">Issue approver token</button>
        <button type="button" (click)="refreshProfile()">Load current user</button>
        <button type="button" (click)="clearSession()">Clear token</button>
      </div>
    </section>

    <section class="panel">
      <h3>Future UI notes</h3>
      <p class="muted">
        Replace the buttons above with a real login or identity selection flow.
      </p>

      <!--
        Planned mapping:
        - form fields -> GenerateTokenRequest
          userId       -> employee or approver identity
          departmentId -> claim used by expense workflows
          roles[]      -> role badges or hidden environment-based presets
        - token response -> SessionFacade
        - SessionFacade currentUser -> top-bar identity chip / side navigation
      -->
    </section>

    <section class="panel">
      <h3>Current session state</h3>
      <p class="muted">Useful while the real interface is still undecided.</p>
      <pre><code>{{ session.currentUser() | json }}</code></pre>
    </section>

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

  protected issueEmployeeToken(): void {
    this.session.issueDeveloperToken({
      userId: 'employee-001',
      departmentId: 'finance',
      roles: ['employee']
    });
  }

  protected issueApproverToken(): void {
    this.session.issueDeveloperToken({
      userId: 'manager-001',
      departmentId: 'finance',
      roles: ['manager']
    });
  }

  protected refreshProfile(): void {
    this.session.loadCurrentUser();
  }

  protected clearSession(): void {
    this.session.clearSession();
  }
}

