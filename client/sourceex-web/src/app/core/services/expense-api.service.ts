import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { buildVersionedApiUrl, injectSourceExApiConfig } from '../config/api.config';
import { CreateExpenseRequest, CreatedExpenseResponse, ExpenseResponse } from '../models/expense.models';

@Injectable({ providedIn: 'root' })
export class ExpenseApiService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiConfig = injectSourceExApiConfig();

  createExpense(request: CreateExpenseRequest): Observable<CreatedExpenseResponse> {
    return this.httpClient.post<CreatedExpenseResponse>(
      buildVersionedApiUrl(this.apiConfig, 'expenses'),
      request);
  }

  getExpenseById(expenseId: string): Observable<ExpenseResponse> {
    return this.httpClient.get<ExpenseResponse>(
      buildVersionedApiUrl(this.apiConfig, `expenses/${expenseId}`));
  }

  approveExpense(expenseId: string): Observable<void> {
    return this.httpClient.post<void>(
      buildVersionedApiUrl(this.apiConfig, `expenses/${expenseId}/approve`),
      {});
  }
}

