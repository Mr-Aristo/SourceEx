import { ExpenseResponse } from '../../../core/models/expense.models';

export interface ExpenseDetailViewModel {
  id: string;
  title: string;
  amountLabel: string;
  ownerLabel: string;
  statusLabel: string;
  createdAtLabel: string;
  raw: ExpenseResponse;
}

