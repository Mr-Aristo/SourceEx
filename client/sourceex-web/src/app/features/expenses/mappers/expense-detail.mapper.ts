import { ExpenseResponse } from '../../../core/models/expense.models';
import { ExpenseDetailViewModel } from '../models/expense-detail-view-model';

export function mapExpenseResponseToDetailViewModel(expense: ExpenseResponse): ExpenseDetailViewModel {
  const amountLabel = new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency: expense.currency,
    maximumFractionDigits: 2
  }).format(expense.amount);

  const createdAtLabel = new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(new Date(expense.createdAt));

  return {
    id: expense.id,
    title: expense.description || 'Expense request',
    amountLabel,
    ownerLabel: `${expense.employeeId} / ${expense.departmentId}`,
    statusLabel: expense.status,
    createdAtLabel,
    raw: expense
  };
}

