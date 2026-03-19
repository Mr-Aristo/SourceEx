export interface CreateExpenseRequest {
  amount: number;
  currency: string;
  description: string;
}

export interface CreatedExpenseResponse {
  expenseId: string;
}

export interface ExpenseResponse {
  id: string;
  employeeId: string;
  departmentId: string;
  amount: number;
  currency: string;
  description: string;
  status: string;
  createdAt: string;
}

