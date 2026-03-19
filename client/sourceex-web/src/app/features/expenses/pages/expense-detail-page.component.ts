import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { JsonPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs';
import { ExpenseFacade } from '../data-access/expense.facade';

@Component({
  selector: 'sx-expense-detail-page',
  standalone: true,
  imports: [JsonPipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel stack">
      <div>
        <p class="status">Expense detail workspace</p>
        <h2>Expense detail route</h2>
        <p class="muted">
          This page is where the future UI will project an API expense model into cards, status chips,
          timelines, and approval actions.
        </p>
      </div>

      <div class="inline-actions">
        <button type="button" (click)="reload()">Reload expense</button>

        @if (currentExpenseId()) {
          <a [routerLink]="['/expenses', currentExpenseId(), 'approve']">Go to approval workspace</a>
        }
      </div>

      <!--
        Planned UI mapping:
        ExpenseResponse -> ExpenseDetailViewModel
        - description -> title area
        - amount/currency -> finance summary card
        - status -> workflow badge
        - employeeId/departmentId -> ownership row
        - createdAt -> timeline / metadata section
      -->
    </section>

    @if (expenseFacade.selectedExpense(); as expense) {
      <section class="panel">
        <h3>{{ expense.title }}</h3>
        <p><strong>Amount:</strong> {{ expense.amountLabel }}</p>
        <p><strong>Owner:</strong> {{ expense.ownerLabel }}</p>
        <p><strong>Status:</strong> {{ expense.statusLabel }}</p>
        <p><strong>Created at:</strong> {{ expense.createdAtLabel }}</p>
      </section>

      <section class="panel">
        <h3>Raw API payload</h3>
        <pre><code>{{ expense.raw | json }}</code></pre>
      </section>
    }

    @if (expenseFacade.lastError()) {
      <section class="panel">
        <h3>Last error</h3>
        <p>{{ expenseFacade.lastError() }}</p>
      </section>
    }
  `
})
export class ExpenseDetailPageComponent {
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly expenseFacade = inject(ExpenseFacade);
  protected readonly currentExpenseId = signal<string | null>(null);

  constructor() {
    this.activatedRoute.paramMap
      .pipe(
        map((params) => params.get('expenseId')),
        filter((expenseId): expenseId is string => !!expenseId),
        takeUntilDestroyed(this.destroyRef))
      .subscribe((expenseId) => {
        this.currentExpenseId.set(expenseId);
        this.expenseFacade.loadExpense(expenseId);
      });
  }

  protected reload(): void {
    const expenseId = this.currentExpenseId();

    if (!expenseId) {
      return;
    }

    this.expenseFacade.loadExpense(expenseId);
  }
}
