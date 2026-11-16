import { InitialAllocation } from './allocation-models';

export interface Money {
  amount: number;
  currency: string;
}

export type Balance = Record<string, number>;

export interface FundDTO {
  id: string;
  balance: Balance;
  name: string;
}

export interface BudgetDTO {
  id: string;
  balance: Balance;
  name: string;
  funds: FundDTO[];
}

export interface AccountDTO {
  id: string;
  balance: Balance;
  name: string;
}

export interface LedgerDTO {
  id: string;
  balance: Balance;
  name: string;
  budgets: BudgetDTO[];
  accounts: AccountDTO[];
}

export interface LedgerListItem {
  id: string;
  name: string;
  balance?: Balance | null;
}

export interface AccountTransactionDTO {
  id: string;
  accountId: string;
  accountName: string;
  value: Money;
  received?: Money | null;
  date: string;
  targetAccountId?: string | null;
  targetAccountName?: string | null;
  title?: string | null;
  allocations?: FundAmountDTO[] | null;
  fundId?: string | null;
}

export interface FundAmountDTO {
  fundId: string;
  amount: number;
}

export interface StatisticsFilters {
  includeInitialBalance?: boolean;
  from?: string;
  to?: string;
}

export interface Summary {
  total: number;
  average: number;
  min: number;
  max: number;
}

export type SummaryByCurrency = Record<string, Summary>;

export interface NamedStatisticsDTO {
  name: string;
  income: SummaryByCurrency;
  expense: SummaryByCurrency;
}

export interface FundFlowDTO {
  allocated: number;
  spent: number;
  netChange: number;
  ending: number;
}

export type FundFlowByCurrency = Record<string, FundFlowDTO>;

export interface FundStatisticsDTO {
  id: string;
  name: string;
  flows: FundFlowByCurrency;
}

export interface BudgetStatisticsDTO {
  id: string;
  name: string;
  totals: FundFlowByCurrency;
  funds: FundStatisticsDTO[];
}

export interface LedgerStatisticsDTO {
  from?: string | null;
  to?: string | null;
  totalIncome: SummaryByCurrency;
  totalExpense: SummaryByCurrency;
  accounts: NamedStatisticsDTO[];
  fundTotals: FundFlowByCurrency;
  budgets: BudgetStatisticsDTO[];
}

export interface AllocationTemplateDTO {
  id: string;
  name: string;
  currency: string;
  minimumAmount: number;
  lines: AllocationTemplateLineDTO[];
}

export interface AllocationTemplateLineDTO {
  fundId: string;
  fundName: string;
  sequence: number;
  type: number;
  amount?: number | null;
  percent?: number | null;
}

export interface AllocationTemplateLineInput {
  fundId: string;
  sequence: number;
  type: number;
  amount?: number | null;
  percent?: number | null;
}

export interface CreateAllocationTemplateBody {
  name: string;
  currency: string;
  lines: AllocationTemplateLineInput[];
}

export interface UpdateAllocationTemplateBody {
  name: string;
  lines: AllocationTemplateLineInput[];
}

export interface LedgerAllocationTemplateDTO {
  id: string;
  name: string;
  currency: string;
  minimumAmount: number;
  lines: LedgerAllocationTemplateLineDTO[];
}

export interface LedgerAllocationTemplateLineDTO {
  budgetId: string;
  budgetName: string;
  sequence: number;
  type: number;
  amount?: number | null;
  percent?: number | null;
}

export interface LedgerAllocationLineInput {
  budgetId: string;
  sequence: number;
  type: number;
  amount?: number | null;
  percent?: number | null;
}

export interface CreateLedgerAllocationTemplateBody {
  name: string;
  currency: string;
  lines: LedgerAllocationLineInput[];
}

export interface UpdateLedgerAllocationTemplateBody {
  name: string;
  lines: LedgerAllocationLineInput[];
}

export interface CreateFundInput {
  name: string;
}

export interface NewTemplateLineInput {
  fund: string;
  sequence: number;
  type: number;
  amount?: number | null;
  percent?: number | null;
}

export interface NewAllocationTemplateInput {
  name: string;
  currency: string;
  lines: NewTemplateLineInput[];
}

export interface CreateBudgetInput {
  name: string;
  funds: CreateFundInput[];
  allocationTemplates?: NewAllocationTemplateInput[];
}

export interface CreateLedgerCommand {
  name: string;
  budgets: CreateBudgetInput[];
  accounts: { initialBalance: Money; name: string }[];
  initialAllocations?: InitialAllocation[];
}
