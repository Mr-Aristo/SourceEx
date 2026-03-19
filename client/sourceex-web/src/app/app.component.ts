import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { SOURCEEX_API_CONFIG } from './core/config/api.config';

@Component({
  selector: 'sx-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="app-shell">
      <header class="app-header">
        <div>
          <p class="muted">Angular client shell for the SourceEx API</p>
          <h1>SourceEx Web</h1>
          <p class="muted">
            API base URL:
            <span class="mono">{{ apiConfig.baseUrl || 'same-origin via Angular proxy' }}</span>
            |
            Version segment:
            <span class="mono">{{ apiConfig.apiVersion }}</span>
          </p>
        </div>

        <nav class="app-nav">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Overview</a>
          <a routerLink="/auth" routerLinkActive="active">Auth</a>
          <a routerLink="/expenses/new" routerLinkActive="active">Create Expense</a>
        </nav>
      </header>

      <!--
        The real visual design is intentionally deferred.
        For now, the router hosts workflow-specific placeholder pages so the team can stabilize:
        1. models
        2. API service integration
        3. state/facade flow
        4. route boundaries
      -->
      <router-outlet />
    </div>
  `
})
export class AppComponent {
  protected readonly apiConfig = inject(SOURCEEX_API_CONFIG);
}
