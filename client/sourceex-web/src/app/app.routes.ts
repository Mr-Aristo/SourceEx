import { Routes } from '@angular/router';
import { authenticatedGuard } from './core/guards/authenticated.guard';
import { AuthWorkspacePageComponent } from './features/auth/pages/auth-workspace-page.component';
import { ExpenseApprovePageComponent } from './features/expenses/pages/expense-approve-page.component';
import { ExpenseCreatePageComponent } from './features/expenses/pages/expense-create-page.component';
import { ExpenseDetailPageComponent } from './features/expenses/pages/expense-detail-page.component';
import { HomePageComponent } from './features/home/pages/home-page.component';

export const appRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: HomePageComponent
  },
  {
    path: 'auth',
    component: AuthWorkspacePageComponent
  },
  {
    path: 'expenses/new',
    component: ExpenseCreatePageComponent,
    canActivate: [authenticatedGuard]
  },
  {
    path: 'expenses/:expenseId',
    component: ExpenseDetailPageComponent,
    canActivate: [authenticatedGuard]
  },
  {
    path: 'expenses/:expenseId/approve',
    component: ExpenseApprovePageComponent,
    canActivate: [authenticatedGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];

