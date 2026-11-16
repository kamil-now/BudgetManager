import { FundAmountDTO, Money } from './models';

/** `FeedItemDTO.type` as the API serializes it (no `JsonStringEnumConverter`, so a number). */
export const FeedItemType = {
  Income: 0,
  Expense: 1,
  Transfer: 2,
  Exchange: 3,
  Allocation: 4,
} as const;

export type FeedItemTypeName = keyof typeof FeedItemType;
export type FeedItemTypeCode = (typeof FeedItemType)[FeedItemTypeName];

export interface FeedAllocationDTO {
  id: string;
  fundId: string;
  fundName: string;
  budgetId: string;
  budgetName: string;
  amount: Money;
}

export interface FeedItemDTO {
  id: string;
  type: FeedItemTypeCode;
  date: string;
  title?: string | null;
  accountId?: string | null;
  accountName?: string | null;
  targetAccountId?: string | null;
  targetAccountName?: string | null;
  budgetId?: string | null;
  budgetName?: string | null;
  fundId?: string | null;
  fundName?: string | null;
  value: Money;
  received?: Money | null;
  /** True for any opening row (initial balance or initial allocation). */
  isInitialBalance: boolean;
  allocated: number;
  allocations: FeedAllocationDTO[];
}

export interface LedgerFeedPageDTO {
  items: FeedItemDTO[];
  total: number;
  offset: number;
  limit: number;
}

export interface FeedFilters {
  from?: string;
  to?: string;
  types?: FeedItemTypeName[];
  search?: string;
  accountId?: string;
  budgetId?: string;
  fundId?: string;
}

export interface TransactionCopy {
  accountId: string;
  accountName?: string | null;
  value: Money;
  title?: string | null;
  allocations?: FundAmountDTO[] | null;
  fundId?: string | null;
}

export interface AllocationEdit {
  id: string;
  currency: string;
  amount: number;
  lines: FundAmountDTO[];
  date: string;
  title: string | null;
  initial: boolean;
}

export type AllocationCopy = Omit<AllocationEdit, 'id' | 'date' | 'initial'>;

export interface TransferRef {
  id: string;
  accountId: string;
}

export interface TransferCopy {
  accountId: string;
  targetAccountId?: string | null;
  value: Money;
  received?: Money | null;
  title?: string | null;
}
