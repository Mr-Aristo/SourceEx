import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'sx-home-page',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel">
      <p class="status">Client foundation only</p>
      <h2>Frontend scope is intentionally narrow for now</h2>
      <p class="muted">
        This Angular app is separated from the backend solution so we can establish routes, contracts,
        API services, and state flow without changing the existing .NET projects.
      </p>

      <div class="inline-actions">
        <a routerLink="/auth">Open auth workspace</a>
        <a routerLink="/expenses/new">Open expense creation workspace</a>
      </div>
    </section>

    <section class="panel">
      <h3>How to think about this shell</h3>
      <p>
        Each page is a placeholder, not a final screen. The important part for now is the integration path:
      </p>
      <pre><code>route -> facade -> API service -> SourceEx.API endpoint -> typed response -> UI view model</code></pre>
    </section>

    <section class="panel">
      <h3>What will change later</h3>
      <p class="muted">
        Final forms, tables, cards, drawers, and visual hierarchy are intentionally postponed until product
        design becomes stable. The current shell exists to reduce future rework.
      </p>

      <!--
        Future dashboard idea:
        - summary cards for pending / approved / rejected expenses
        - manager approval queue
        - policy risk review stream
        - audit timeline shortcuts
      -->
    </section>
  `
})
export class HomePageComponent {}

