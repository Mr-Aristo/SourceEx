import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs';
import { ExpenseFacade } from '../data-access/expense.facade';

@Component({
  selector: 'sx-expense-approve-page',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel stack">
      <div>
        <p class="status">Approval workspace</p>
        <h2>Approve expense flow</h2>
        <p class="muted">
          This route is intentionally small. It protects the approval integration path before the manager UI,
          comments panel, or policy badges are designed.
        </p>
      </div>

      <div class="inline-actions">
        <button type="button" (click)="approveExpense()" [disabled]="!currentExpenseId()">Approve expense</button>

        @if (currentExpenseId()) {
          <a [routerLink]="['/expenses', currentExpenseId()]">Back to detail workspace</a>
        }
      </div>

      <!--
        Planned UI mapping:
        - approval summary panel pulls from ExpenseDetailViewModel
        - policy risk badges will later consume ExpenseRiskAssessedIntegrationEvent projections
        - manager comments / justification can be added when the backend contract supports it
        - approve action remains ExpenseFacade.approveExpense(expenseId)
      -->
    </section>

    @if (expenseFacade.lastSuccessMessage()) {
      <section class="panel">
        <h3>Last success</h3>
        <p>{{ expenseFacade.lastSuccessMessage() }}</p>
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
export class ExpenseApprovePageComponent {
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

  protected approveExpense(): void {
    const expenseId = this.currentExpenseId();

    if (!expenseId) {
      return;
    }

    this.expenseFacade.approveExpense(expenseId);
  }
}
