import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AllocationTemplateDTO,
  CreateAllocationTemplateBody,
  CreateFundInput,
  NewAllocationTemplateInput,
  UpdateAllocationTemplateBody,
} from './models';

export interface CreateBudgetCommand {
  ledgerId: string;
  name: string;
  funds: CreateFundInput[];
  allocationTemplates?: NewAllocationTemplateInput[];
}

/** A fund with an id is renamed, one without is added, and a fund of the budget left out is removed. */
export interface UpdateBudgetBody {
  name: string;
  funds: { id?: string | null; name: string }[];
}

@Injectable({ providedIn: 'root' })
export class BudgetService {
  private readonly http = inject(HttpClient);

  createBudget(command: CreateBudgetCommand): Observable<string> {
    return this.http.post<string>('/api/budgets', command);
  }

  updateBudget(budgetId: string, body: UpdateBudgetBody): Observable<void> {
    return this.http.put<void>(`/api/budgets/${budgetId}`, body);
  }

  getAllocationTemplates(budgetId: string): Observable<AllocationTemplateDTO[]> {
    return this.http.get<AllocationTemplateDTO[]>(`/api/budgets/${budgetId}/allocationTemplates`);
  }

  createAllocationTemplate(
    budgetId: string,
    body: CreateAllocationTemplateBody,
  ): Observable<string> {
    return this.http.post<string>(`/api/budgets/${budgetId}/allocationTemplates`, {
      budgetId,
      ...body,
    });
  }

  updateAllocationTemplate(
    budgetId: string,
    templateId: string,
    body: UpdateAllocationTemplateBody,
  ): Observable<void> {
    return this.http.put<void>(`/api/budgets/${budgetId}/allocationTemplates/${templateId}`, {
      budgetId,
      templateId,
      ...body,
    });
  }

  deleteAllocationTemplate(budgetId: string, templateId: string): Observable<void> {
    return this.http.delete<void>(`/api/budgets/${budgetId}/allocationTemplates/${templateId}`);
  }
}
