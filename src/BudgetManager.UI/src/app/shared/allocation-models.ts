import { Money } from './models';

export interface FundAmount {
  fundId: string;
  amount: number;
}

export interface AllocationLineInput {
  fundId: string;
  sequence: number;
  type: number;
  amount?: number | null;
  percent?: number | null;
}

export interface LedgerAllocationInput {
  templateId: string;
  budgetTemplateIds: string[];
}

export interface AllocationInput {
  templateId?: string | null;
  lines?: AllocationLineInput[] | null;
  ledgerTemplate?: LedgerAllocationInput | null;
}

export interface AllocationPreviewRequest {
  amount: number;
  currency: string;
  allocation: AllocationInput;
}

export interface SplitPreviewLine {
  fundId: string;
  fundName: string;
  budgetName?: string | null;
  amount: number;
}

export interface SplitPreview {
  lines: SplitPreviewLine[];
  unallocated: number;
}

export interface AllocateFundsBody {
  currency: string;
  allocation: AllocationInput;
  amount?: number | null;
  date?: string | null;
  title?: string | null;
}

export interface UpdateAllocationBody {
  currency: string;
  amount: number;
  allocation: AllocationInput;
  date: string;
  title?: string | null;
}

export interface InitialAllocation {
  budget: string;
  fund: string;
  value: Money;
}
