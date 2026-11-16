import { TranslationKey, currentLocale, translate } from '../i18n/i18n';
import { dayEndIso, formatDate, toIso } from '../shared/date';
import {
  AllocationCopy,
  AllocationEdit,
  FeedAllocationDTO,
  FeedFilters,
  FeedItemDTO,
  FeedItemType,
  FeedItemTypeName,
  TransactionCopy,
  TransferCopy,
  TransferRef,
} from '../shared/feed-models';
import { AccountTransactionDTO, LedgerDTO, Money } from '../shared/models';
import { MoneyLine, formatAmount, formatMoney, signedMoney } from '../shared/money';

export type TxKind = 'income' | 'expense' | 'transfer' | 'exchange' | 'allocation';

export type TypeFilter = 'all' | 'income' | 'expense' | 'transfer' | 'exchange' | 'funds';

export type Period = 'last-7' | 'last-30' | 'last-90' | 'last-365' | 'all' | 'custom';

export const TYPE_OPTIONS: readonly { value: TypeFilter; label: TranslationKey }[] = [
  { value: 'all', label: 'type.all' },
  { value: 'income', label: 'common.income' },
  { value: 'expense', label: 'common.expense' },
  { value: 'transfer', label: 'type.transfers' },
  { value: 'exchange', label: 'type.exchanges' },
  { value: 'funds', label: 'type.funds' },
];

export const PERIOD_OPTIONS: readonly { value: Period; label: TranslationKey }[] = [
  { value: 'last-7', label: 'period.last7' },
  { value: 'last-30', label: 'period.last30' },
  { value: 'last-90', label: 'period.last90' },
  { value: 'last-365', label: 'period.last365' },
  { value: 'all', label: 'period.all' },
  { value: 'custom', label: 'period.custom' },
];

export const DEFAULT_PERIOD: Period = 'all';

export const FEED_PAGE_SIZE = 50;
export const COMPACT_WIDTH = 600;

export interface RowMetrics {
  row: number;
  day: number;
}
export const WIDE_METRICS: RowMetrics = { row: 44, day: 22 };
export const COMPACT_METRICS: RowMetrics = { row: 44, day: 22 };

export function metricsFor(width: number): RowMetrics {
  return width > 0 && width < COMPACT_WIDTH ? COMPACT_METRICS : WIDE_METRICS;
}

export type Scope =
  | { kind: 'account'; id: string; name: string }
  | { kind: 'budget'; id: string; name: string }
  | { kind: 'fund'; id: string; name: string; budgetName: string };

export function scopeLabel(scope: Scope): string {
  switch (scope.kind) {
    case 'account':
      return translate('scope.account', { name: scope.name });
    case 'budget':
      return translate('scope.budget', { name: scope.name });
    case 'fund':
      return translate('scope.fund', { budget: scope.budgetName, name: scope.name });
  }
}

export function isScope(scope: Scope | null, kind: Scope['kind'], id: string): boolean {
  return scope !== null && scope.kind === kind && scope.id === id;
}

const typeNames: Record<TypeFilter, FeedItemTypeName[] | undefined> = {
  all: undefined,
  income: ['Income'],
  expense: ['Expense'],
  transfer: ['Transfer'],
  exchange: ['Exchange'],
  funds: ['Allocation'],
};

export interface FeedQueryInput {
  range: DateRange;
  type: TypeFilter;
  search: string;
  scope: Scope | null;
}

export function feedFilters({ range, type, search, scope }: FeedQueryInput): FeedFilters {
  const filters: FeedFilters = {};
  if (range.from) filters.from = range.from;
  if (range.to) filters.to = range.to;
  const types = typeNames[type];
  if (types) filters.types = types;
  const text = search.trim();
  if (text) filters.search = text;
  if (scope?.kind === 'account') filters.accountId = scope.id;
  if (scope?.kind === 'budget') filters.budgetId = scope.id;
  if (scope?.kind === 'fund') filters.fundId = scope.id;
  return filters;
}

export type RowAction = 'edit' | 'copy' | 'allocate' | 'delete';

export interface FeedRow {
  key: string;
  item: FeedItemDTO;
  kind: TxKind;
  title: string;
  symbol: string;
  symClass: string;
  kindLabel: string;
  amountNumber: string;
  currency: string;
  amountClass: string;
  meta: string;
  date: string;
  notAllocated?: string;
  opening: boolean;
  editable: boolean;
  actions: RowAction[];
  net?: Money;
}

const typeKinds: Record<number, TxKind> = {
  [FeedItemType.Income]: 'income',
  [FeedItemType.Expense]: 'expense',
  [FeedItemType.Transfer]: 'transfer',
  [FeedItemType.Exchange]: 'exchange',
  [FeedItemType.Allocation]: 'allocation',
};

const kindInfo: Record<TxKind, { label: TranslationKey; symbol: string; symClass: string }> = {
  income: { label: 'common.income', symbol: '+', symClass: 'sym--in' },
  expense: { label: 'common.expense', symbol: '−', symClass: 'sym--out' },
  transfer: { label: 'kind.transfer', symbol: '→', symClass: 'sym--transfer' },
  exchange: { label: 'kind.exchange', symbol: '⇄', symClass: 'sym--neutral' },
  allocation: { label: 'kind.allocation', symbol: '↓', symClass: 'sym--info' },
};

const amountClasses: Record<TxKind, string> = {
  income: 'amount--in',
  expense: 'amount--out',
  transfer: 'amount--transfer',
  exchange: 'amount--neutral',
  allocation: 'amount--info',
};

export function isOpening(item: FeedItemDTO): boolean {
  return item.isInitialBalance || new Date(item.date).getUTCFullYear() < 1000;
}

export function feedRow(item: FeedItemDTO): FeedRow {
  const kind = typeKinds[item.type] ?? 'expense';
  const info = kindInfo[kind];
  const { currency } = item.value;
  const magnitude = Math.abs(item.value.amount);
  const opening = isOpening(item);
  const initialBalance = opening && kind !== 'allocation';
  const initialAllocation = opening && kind === 'allocation';

  let { symbol, symClass } = info;
  let kindLabel = translate(info.label);
  let fallback = kindLabel;
  if (initialBalance) {
    symbol = '=';
    symClass = 'sym--neutral';
    kindLabel = fallback = translate('feed.initialBalance');
  } else if (initialAllocation) {
    kindLabel = fallback = translate('feed.initialAllocation');
  } else if (kind === 'exchange') {
    fallback = translate('feed.currencyExchange');
  }

  let net: Money | undefined;
  if (kind === 'income' || kind === 'expense') {
    net = { amount: kind === 'expense' ? -magnitude : magnitude, currency };
  }
  const notAllocated = kind === 'income' && !opening ? amountLeft(item) : undefined;

  return {
    key: `${kind}:${item.id}`,
    item,
    kind,
    title: item.title || fallback,
    symbol,
    symClass,
    kindLabel,
    amountNumber: formatAmount(magnitude),
    currency,
    amountClass: amountClasses[kind],
    meta: metaOf(item, kind),
    date: opening ? '' : formatDate(item.date),
    notAllocated,
    opening,
    editable: true,
    actions: rowActions(notAllocated !== undefined),
    net,
  };
}

function budgetNames(lines: readonly FeedAllocationDTO[]): string {
  return [...new Set(lines.map((a) => a.budgetName))].join(', ');
}

function metaOf(item: FeedItemDTO, kind: TxKind): string {
  const account = item.accountName ?? '';
  const target =
    item.targetAccountId && item.targetAccountId !== item.accountId
      ? (item.targetAccountName ?? '')
      : '';
  const join = (...parts: string[]) => parts.filter(Boolean).join(' · ');
  switch (kind) {
    case 'income':
      return join(budgetNames(item.allocations), account);
    case 'expense':
      return join(item.fundName ? `${item.budgetName ?? ''} / ${item.fundName}` : '', account);
    case 'transfer':
      return target ? `${account} → ${target}` : account;
    case 'exchange': {
      const received = item.received
        ? formatMoney(Math.abs(item.received.amount), item.received.currency)
        : '';
      return join(target ? `${account} → ${target}` : account, received);
    }
    case 'allocation': {
      const lines = item.allocations;
      if (lines.length === 1) return `${lines[0].budgetName} / ${lines[0].fundName}`;
      if (lines.length > 1) return budgetNames(lines);
      return item.budgetName && item.fundName ? `${item.budgetName} / ${item.fundName}` : '';
    }
  }
}

function amountLeft(item: FeedItemDTO): string | undefined {
  const amount = toCents(item.value.amount);
  const allocated = toCents(item.allocated);
  if (allocated >= amount) return undefined;
  const left = formatMoney((amount - Math.max(allocated, 0)) / 100, item.value.currency);
  return translate('tx.notAllocated', { amount: left });
}

function rowActions(allocatable: boolean): RowAction[] {
  return allocatable ? ['edit', 'copy', 'allocate', 'delete'] : ['edit', 'copy', 'delete'];
}

export function toTransaction(item: FeedItemDTO): AccountTransactionDTO {
  return {
    id: item.id,
    accountId: item.accountId ?? '',
    accountName: item.accountName ?? '',
    value: item.value,
    date: item.date,
    title: item.title ?? null,
    allocations: item.allocations.map((a) => ({ fundId: a.fundId, amount: a.amount.amount })),
    fundId: item.fundId ?? null,
  };
}

export function toTransactionCopy(item: FeedItemDTO): TransactionCopy {
  const { id: _, date: __, ...copy } = toTransaction(item);
  return isOpening(item) ? { ...copy, title: null } : copy;
}

export function toAllocationEdit(item: FeedItemDTO): AllocationEdit {
  return {
    id: item.id,
    currency: item.value.currency,
    amount: item.value.amount,
    lines: item.allocations.map((a) => ({ fundId: a.fundId, amount: a.amount.amount })),
    date: item.date,
    title: item.title ?? null,
    initial: isOpening(item),
  };
}

export function toAllocationCopy(item: FeedItemDTO): AllocationCopy {
  const { id: _, date: __, initial, ...copy } = toAllocationEdit(item);
  return initial ? { ...copy, title: null } : copy;
}

export function toTransferRef(item: FeedItemDTO): TransferRef {
  return { id: item.id, accountId: item.accountId ?? '' };
}

export function toTransferCopy(item: FeedItemDTO): TransferCopy {
  return {
    accountId: item.accountId ?? '',
    targetAccountId: item.targetAccountId ?? null,
    value: { amount: Math.abs(item.value.amount), currency: item.value.currency },
    received: item.received ?? null,
    title: item.title ?? null,
  };
}

const weekdays = new Map<string, Intl.DateTimeFormat>();

function weekdayOf(iso: string): string {
  const locale = currentLocale();
  let format = weekdays.get(locale);
  if (!format) {
    format = new Intl.DateTimeFormat(locale, { weekday: 'short', timeZone: 'UTC' });
    weekdays.set(locale, format);
  }
  return format.format(new Date(iso));
}

export function dayKeyOf(iso: string): string {
  const date = new Date(iso);
  return date.getUTCFullYear() < 1000 ? 'opening' : date.toISOString()
    .slice(0, 10);
}

export function dayLabel(iso: string): string {
  if (dayKeyOf(iso) === 'opening') return translate('feed.opening');
  return `${weekdayOf(iso)}, ${formatDate(iso)}`;
}

export interface DayRow {
  kind: 'day';
  key: string;
  height: number;
  label: string;
  net: MoneyLine[];
}

export interface ItemRow {
  kind: 'item';
  key: string;
  height: number;
  day: number;
  page: number;
  row: FeedRow;
  expanded: boolean;
}

export interface AllocationRow {
  kind: 'alloc';
  key: string;
  height: number;
  day: number;
  page: number;
  parent: FeedRow;
  allocation: FeedAllocationDTO;
  symbol: string;
  title: string;
  meta: string;
  amountNumber: string;
  currency: string;
}

export interface GapRow {
  kind: 'gap';
  key: string;
  height: number;
  day: -1;
  page: number;
  count: number;
}

export type DisplayRow = DayRow | ItemRow | AllocationRow | GapRow;

export function displayRows(
  pages: ReadonlyMap<number, readonly FeedItemDTO[]>,
  total: number,
  pageSize: number,
  expanded: ReadonlySet<string>,
  metrics: RowMetrics,
  withDays = true,
): DisplayRow[] {
  const rows: DisplayRow[] = [];
  const seen = new Set<string>();
  const dayKeys = new Map<string, number>();
  let dayIndex = -1;
  let dayKey: string | null = null;
  let cents: Record<string, number> = {};
  const closeDay = () => {
    if (dayIndex >= 0) (rows[dayIndex] as DayRow).net = netLines(cents);
    dayKey = null;
  };

  const pageCount = Math.ceil(Math.max(total, 0) / pageSize);
  for (let page = 0; page < pageCount; page++) {
    const count = Math.min(pageSize, total - page * pageSize);
    const items = pages.get(page);
    if (!items) {
      closeDay();
      rows.push({ kind: 'gap', key: `gap:${page}`, height: count * metrics.row, day: -1, page, count });
      continue;
    }
    for (const item of items.slice(0, count)) {
      const row = feedRow(item);
      if (seen.has(row.key)) continue;
      seen.add(row.key);
      const key = dayKeyOf(item.date);
      if (withDays && dayKey !== key) {
        closeDay();
        const n = dayKeys.get(key) ?? 0;
        dayKeys.set(key, n + 1);
        dayIndex = rows.length;
        dayKey = key;
        cents = {};
        rows.push({
          kind: 'day',
          key: n === 0 ? `day:${key}` : `day:${key}:${n}`,
          height: metrics.day,
          label: dayLabel(item.date),
          net: [],
        });
      }
      const index = withDays ? dayIndex : -1;
      if (row.net) cents[row.net.currency] = (cents[row.net.currency] ?? 0) + toCents(row.net.amount);
      const isExpanded = row.item.allocations.length > 0 && expanded.has(row.key);
      rows.push({
        kind: 'item',
        key: row.key,
        height: metrics.row,
        day: index,
        page,
        row,
        expanded: isExpanded,
      });
      if (!isExpanded) continue;
      for (const allocation of row.item.allocations) {
        rows.push({
          kind: 'alloc',
          key: `alloc:${allocation.id}`,
          height: metrics.row,
          day: index,
          page,
          parent: row,
          allocation,
          symbol: kindInfo.allocation.symbol,
          title: allocation.fundName,
          meta: allocation.budgetName,
          amountNumber: formatAmount(allocation.amount.amount),
          currency: allocation.amount.currency,
        });
      }
    }
  }
  closeDay();
  return rows;
}

function netLines(cents: Record<string, number>): MoneyLine[] {
  return Object.entries(cents)
    .filter(([, c]) => c !== 0)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([currency, c]) => ({
      currency,
      amount: c / 100,
      number: formatAmount(c / 100),
      text: signedMoney(c / 100, currency),
    }));
}

export function rowOffsets(rows: readonly DisplayRow[]): number[] {
  const offsets = [0];
  for (const row of rows) offsets.push(offsets[offsets.length - 1] + row.height);
  return offsets;
}

export function windowRange(
  offsets: readonly number[],
  top: number,
  bottom: number,
): { start: number; end: number } {
  const count = offsets.length - 1;
  const start = firstIndex(count, (i) => offsets[i + 1] > top);
  const end = Math.max(start, firstIndex(count, (i) => offsets[i] >= bottom));
  return { start, end };
}

function firstIndex(count: number, test: (i: number) => boolean): number {
  let lo = 0;
  let hi = count;
  while (lo < hi) {
    const mid = (lo + hi) >> 1;
    if (test(mid)) hi = mid;
    else lo = mid + 1;
  }
  return lo;
}

export interface RowBlock {
  key: string;
  header: DayRow | null;
  borrowed: boolean;
  rows: (ItemRow | AllocationRow | GapRow)[];
}

export function windowBlocks(rows: readonly DisplayRow[], start: number, end: number): RowBlock[] {
  const blocks: (RowBlock & { day: number })[] = [];
  let current: (RowBlock & { day: number }) | null = null;
  for (let i = start; i < end; i++) {
    const row = rows[i];
    if (row.kind === 'day') {
      current = { key: row.key, header: row, borrowed: false, rows: [], day: i };
      blocks.push(current);
      continue;
    }
    if (!current || current.day !== row.day) {
      const header: DayRow | null = row.day >= 0 ? (rows[row.day] as DayRow) : null;
      current = { key: header?.key ?? row.key, header, borrowed: !!header, rows: [], day: row.day };
      blocks.push(current);
    }
    current.rows.push(row);
  }
  return blocks.map(({ day: _, ...block }) => block);
}

export function pagesIn(rows: readonly DisplayRow[], start: number, end: number): number[] {
  const pages: number[] = [];
  for (let i = start; i < end; i++) {
    const row = rows[i];
    if (row.kind !== 'day' && pages[pages.length - 1] !== row.page) pages.push(row.page);
  }
  return pages;
}

export interface CurrencyTotal {
  currency: string;
  total: number;
  inFunds: number;
  unallocated: number;
}

export function currencyTotals(ledger: LedgerDTO): CurrencyTotal[] {
  const balances = [ledger.balance, ...ledger.budgets.map((b) => b.balance)];
  const currencies = [...new Set(balances.flatMap((b) => Object.keys(b ?? {})))].sort();
  return currencies.map((currency) => {
    const total = toCents(ledger.balance?.[currency]);
    const inFunds = ledger.budgets.reduce((sum, b) => sum + toCents(b.balance?.[currency]), 0);
    return {
      currency,
      total: total / 100,
      inFunds: inFunds / 100,
      unallocated: (total - inFunds) / 100,
    };
  });
}

export interface DateRange {
  from?: string;
  to?: string;
}

const periodDays: Partial<Record<Period, number>> = {
  'last-7': 7,
  'last-30': 30,
  'last-90': 90,
  'last-365': 365,
};

export function periodRange(
  period: Period,
  now: Date = new Date(),
  custom: { from: string; to: string } = { from: '', to: '' },
): DateRange {
  if (period === 'all') return {};
  if (period === 'custom') {
    return {
      from: custom.from ? toIso(custom.from) : undefined,
      to: custom.to ? dayEndIso(custom.to) : undefined,
    };
  }
  const days = periodDays[period] ?? 1;
  const y = now.getFullYear();
  const m = now.getMonth();
  const d = now.getDate();
  return { from: toIso(day(y, m, d - (days - 1))), to: dayEndIso(day(y, m, d)) };
}

export function isValidRange(range: DateRange): boolean {
  return !range.from || !range.to || range.from <= range.to;
}

function day(year: number, month: number, date: number): string {
  return new Date(Date.UTC(year, month, date)).toISOString()
    .slice(0, 10);
}

function toCents(amount: number | undefined): number {
  return Math.round((amount ?? 0) * 100);
}
