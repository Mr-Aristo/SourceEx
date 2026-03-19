import { inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';
import { CreateExpenseRequest } from '../../../core/models/expense.models';
import { ExpenseApiService } from '../../../core/services/expense-api.service';
import { getHttpErrorMessage } from '../../../core/services/http-error-message';
import { mapExpenseResponseToDetailViewModel } from '../mappers/expense-detail.mapper';
import { ExpenseDetailViewModel } from '../models/expense-detail-view-model';

@Injectable({ providedIn: 'root' })
export class ExpenseFacade {
  private readonly expenseApiService = inject(ExpenseApiService);

  readonly selectedExpense = signal<ExpenseDetailViewModel | null>(null);
  readonly lastCreatedExpenseId = signal<string | null>(null);
  readonly isBusy = signal(false);
  readonly lastError = signal<string | null>(null);
  readonly lastSuccessMessage = signal<string | null>(null);

  createExpense(request: CreateExpenseRequest): void {
    this.isBusy.set(true);
    this.lastError.set(null);
    this.lastSuccessMessage.set(null);

    this.expenseApiService.createExpense(request)
      .pipe(finalize(() => this.isBusy.set(false)))
      .subscribe({
        next: (response) => {
          this.lastCreatedExpenseId.set(response.expenseId);
          this.lastSuccessMessage.set('Expense created. Open the detail route to continue the flow.');
        },
        error: (error: unknown) => {
          this.lastError.set(getHttpErrorMessage(error));
        }
      });
  }

  createSampleExpense(): void {
    this.createExpense({
      amount: 2750,
      currency: 'USD',
      description: 'Conference travel reimbursement'
    });
  }

  loadExpense(expenseId: string): void {
    this.isBusy.set(true);
    this.lastError.set(null);

    this.expenseApiService.getExpenseById(expenseId)
      .pipe(finalize(() => this.isBusy.set(false)))
      .subscribe({
        next: (expense) => {
          this.selectedExpense.set(mapExpenseResponseToDetailViewModel(expense));
        },
        error: (error: unknown) => {
          this.lastError.set(getHttpErrorMessage(error));
        }
      });
  }

  approveExpense(expenseId: string): void {
    this.isBusy.set(true);
    this.lastError.set(null);
    this.lastSuccessMessage.set(null);

    this.expenseApiService.approveExpense(expenseId)
      .pipe(finalize(() => this.isBusy.set(false)))
      .subscribe({
        next: () => {
          this.lastSuccessMessage.set('Expense approval request completed.');
          this.loadExpense(expenseId);
        },
        error: (error: unknown) => {
          this.lastError.set(getHttpErrorMessage(error));
        }
      });
  }
}

