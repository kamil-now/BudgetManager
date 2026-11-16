import { TranslationKey, currentLanguage, translate } from '../i18n/i18n';
import {
  BudgetStatisticsDTO,
  FundFlowByCurrency,
  NamedStatisticsDTO,
  SummaryByCurrency,
} from '../shared/models';
import { amountClass, formatMoney, formatNumber, signedMoney } from '../shared/money';

export interface Side {
  value: number;
  text: string;
}

export interface ChartRow {
  label: string;
  sub?: string;
  income: Side | null;
  expense: Side | null;
}

export interface ChartLegend {
  in: TranslationKey;
  out: TranslationKey;
}

export interface Chart {
  title: string;
  axisTitle: string;
  mixed: boolean;
  rows: ChartRow[];
  ticks: { at: number; text: string }[];
  max: number;
  description: string;
  legend: ChartLegend;
}

export interface TableRow {
  currency: string;
  cells: string[];
  cls: string;
}

export interface Kpi {
  headline: string;
  amount: number;
  others: string;
}

export interface FundCell {
  label: TranslationKey;
  text: string;
  cls: string;
}

export interface FundRow {
  name: string;
  cells: FundCell[];
}

export interface FundGroup {
  budget: string;
  rows: FundRow[];
  total: FundRow[] | null;
}

export interface FundTable {
  groups: FundGroup[];
  all: FundRow[] | null;
}

export const INCOME_LEGEND: ChartLegend = { in: 'common.income', out: 'common.expense' };
export const FUND_LEGEND: ChartLegend = { in: 'stats.allocated', out: 'stats.spent' };

const TICK_COUNT = 3;
const DASH = '—';
const currencyNames = new Map<string, Intl.DisplayNames>();

export function currencyName(code: string): string | undefined {
  try {
    const language = currentLanguage();
    let names = currencyNames.get(language);
    if (!names) {
      names = new Intl.DisplayNames(language, { type: 'currency' });
      currencyNames.set(language, names);
    }
    const name = names.of(code);
    return name && name !== code ? name : undefined;
  } catch {
    return undefined;
  }
}

export function side(total: number | undefined, currency: string, sign: 1 | -1): Side | null {
  const value = Math.abs(total ?? 0);
  return value > 0 ? { value, text: formatMoney(sign * value, currency) } : null;
}

export function namedRows(items: NamedStatisticsDTO[]): ChartRow[] {
  return items.flatMap((item): ChartRow[] => {
    const currencies = [
      ...new Set([...Object.keys(item.income ?? {}), ...Object.keys(item.expense ?? {})]),
    ].sort();
    if (currencies.length === 0) return [{ label: item.name, income: null, expense: null }];
    return currencies.map((c) => ({
      label: item.name,
      sub: currencies.length > 1 ? c : undefined,
      income: side(item.income?.[c]?.total, c, 1),
      expense: side(item.expense?.[c]?.total, c, -1),
    }));
  });
}

export function fundChartRows(budgets: BudgetStatisticsDTO[]): ChartRow[] {
  return budgets.flatMap((budget) =>
    budget.funds.flatMap((fund): ChartRow[] => {
      const active = Object.entries(fund.flows)
        .filter(([, flow]) => flow.allocated !== 0 || flow.spent !== 0)
        .sort(([a], [b]) => a.localeCompare(b));
      return active.map(([currency, flow]) => ({
        label: fund.name,
        sub: active.length > 1 ? `${budget.name} · ${currency}` : budget.name,
        income: side(flow.allocated, currency, 1),
        expense: side(flow.spent, currency, -1),
      }));
    }),
  );
}

function flowCells(flow: FundFlowByCurrency[string], currency: string): FundCell[] {
  return [
    { label: 'stats.allocated', text: signedMoney(flow.allocated, currency), cls: amountClass(flow.allocated) },
    { label: 'stats.spent', text: signedMoney(flow.spent, currency), cls: amountClass(flow.spent) },
    { label: 'stats.netChange', text: signedMoney(flow.netChange, currency), cls: amountClass(flow.netChange) },
    {
      label: 'stats.balance',
      text: formatMoney(flow.ending, currency),
      cls: flow.ending < 0 ? 'amount--out' : 'amount--neutral',
    },
  ];
}

const emptyCells = (): FundCell[] =>
  (['stats.allocated', 'stats.spent', 'stats.netChange', 'stats.balance'] as const).map((label) => ({
    label,
    text: DASH,
    cls: 'amount--neutral',
  }));

function flowRows(name: string, flows: FundFlowByCurrency): FundRow[] {
  const currencies = Object.keys(flows).sort();
  if (currencies.length === 0) return [{ name, cells: emptyCells() }];
  return currencies.map((currency) => ({
    name: currencies.length > 1 ? `${name} · ${currency}` : name,
    cells: flowCells(flows[currency], currency),
  }));
}

export function fundTable(budgets: BudgetStatisticsDTO[], fundTotals: FundFlowByCurrency): FundTable {
  const withFunds = budgets.filter((budget) => budget.funds.length > 0);
  const groups = withFunds.map(
    (budget): FundGroup => ({
      budget: budget.name,
      rows: budget.funds.flatMap((fund) => flowRows(fund.name, fund.flows)),
      total:
        budget.funds.length > 1 ? flowRows(translate('stats.budgetTotal'), budget.totals) : null,
    }),
  );
  return {
    groups,
    all: groups.length > 1 ? flowRows(translate('stats.allFunds'), fundTotals) : null,
  };
}

export function chart(
  title: string,
  axisTitle: string,
  mixed: boolean,
  rows: ChartRow[],
  legend: ChartLegend = INCOME_LEGEND,
  describeKey: 'stats.describe' | 'stats.describeFund' = 'stats.describe',
): Chart {
  const peak = Math.max(0, ...rows.flatMap((r) => [r.income?.value ?? 0, r.expense?.value ?? 0]));
  const step = niceStep(peak / TICK_COUNT);
  const max = step * TICK_COUNT;
  const ticks = Array.from({ length: TICK_COUNT + 1 }, (_, i) => ({
    at: (i / TICK_COUNT) * 100,
    text: formatNumber(i * step),
  }));
  const none = translate('stats.noneLower');
  const description = rows
    .map((r) =>
      translate(describeKey, {
        label: `${r.label}${r.sub ? ` ${r.sub}` : ''}`,
        income: r.income?.text ?? none,
        expense: r.expense?.text ?? none,
      }),
    )
    .join('; ');
  return { title, axisTitle, mixed, rows, ticks, max, description, legend };
}

export function niceStep(raw: number): number {
  if (raw <= 0) return 1;
  const pow = Math.pow(10, Math.floor(Math.log10(raw)));
  const fraction = raw / pow;
  const nice = [1, 2, 2.5, 5, 10].find((n) => fraction <= n + 1e-9) ?? 10;
  return nice * pow;
}

export function tableRows(summaries: SummaryByCurrency, sign: 1 | -1): TableRow[] {
  return Object.entries(summaries)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([currency, s]) => ({
      currency,
      cls: sign > 0 ? 'amount--in' : 'amount--out',
      // Expenses are stored negative, so the API's min is the largest expense; swap to keep Min/Max by magnitude.
      cells: [s.total, s.average, sign > 0 ? s.min : s.max, sign > 0 ? s.max : s.min].map((v) =>
        formatMoney(sign * Math.abs(v), currency),
      ),
    }));
}

export function kpi(currencies: string[], valueOf: (c: string) => number): Kpi {
  const [head, ...rest] = currencies.map((c) => ({ c, v: valueOf(c) }));
  if (!head) return { headline: DASH, amount: 0, others: '' };
  return {
    headline: formatMoney(head.v, head.c),
    amount: head.v,
    others: rest.map((l) => signedMoney(l.v, l.c)).join(' · '),
  };
}
