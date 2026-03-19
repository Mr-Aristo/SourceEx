import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ExpenseFacade } from '../data-access/expense.facade';

@Component({
  selector: 'sx-expense-create-page',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="panel stack">
      <div>
        <p class="status">Expense creation workspace</p>
        <h2>Create expense flow</h2>
        <p class="muted">
          The final page will likely become a multi-field form. For now, this route only proves that the
          Angular client can issue the same create request the API expects.
        </p>
      </div>

      <div class="inline-actions">
        <button type="button" (click)="createSampleExpense()">Create sample expense</button>
      </div>

      <!--
        Planned UI mapping:
        - amount input      -> CreateExpenseRequest.amount
        - currency selector -> CreateExpenseRequest.currency
        - description field -> CreateExpenseRequest.description
        - submit action     -> ExpenseFacade.createExpense(request)
        - success state     -> navigate to /expenses/:expenseId
      -->
    </section>

    @if (expenseFacade.lastCreatedExpenseId()) {
      <section class="panel">
        <h3>Last created expense</h3>
        <p class="mono">{{ expenseFacade.lastCreatedExpenseId() }}</p>
        <div class="inline-actions">
          <a [routerLink]="['/expenses', expenseFacade.lastCreatedExpenseId()]">Open expense detail</a>
          <a [routerLink]="['/expenses', expenseFacade.lastCreatedExpenseId(), 'approve']">Open approval workspace</a>
        </div>
      </section>
    }

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
export class ExpenseCreatePageComponent {
  protected readonly expenseFacade = inject(ExpenseFacade);

  protected createSampleExpense(): void {
    this.expenseFacade.createSampleExpense();
  }
}

