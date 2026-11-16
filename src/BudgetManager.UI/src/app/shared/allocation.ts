import { AllocationLineInput, FundAmount } from './allocation-models';

export type AllocationKind = 'Fixed' | 'Percent';

/**
 * How the API serializes `AllocationType` (no `JsonStringEnumConverter` is configured), so the forms
 * send the numeric value: Fixed = 0, Percent = 1.
 */
export const allocationType: Record<AllocationKind, number> = { Fixed: 0, Percent: 1 };

export interface AllocationRule {
  fundId: string;
  kind: AllocationKind;
  value: number | null;
}

export interface AllocationRuleLine {
  fundId: string;
  sequence: number;
  type: number;
  amount?: number | null;
  percent?: number | null;
}

/** Percents in hundredths of a percent: 100% is 10 000. */
const WHOLE = 10_000n;
/** The server's `Constants.MaxMoneyAmount` in cents. */
const MAX_CENTS = 999_999_999_999_999_999n;

const cents = (value: number | null | undefined): bigint =>
  BigInt(Number.isFinite(value) ? Math.round((value ?? 0) * 100) : 0);
const hundredths = (percent: number | null | undefined): bigint => cents(percent);
const toAmount = (value: bigint): number => Number(value) / 100;

export function toAllocationLines(rules: readonly AllocationRule[]): AllocationLineInput[] {
  return rules.map((r, i) => ({
    fundId: r.fundId,
    sequence: i,
    type: allocationType[r.kind],
    amount: r.kind === 'Fixed' ? r.value : null,
    percent: r.kind === 'Percent' ? r.value : null,
  }));
}

export function pipelineOrder(lines: readonly AllocationRuleLine[]): number[] {
  return lines.map((_, i) => i).sort((a, b) => lines[a].sequence - lines[b].sequence);
}

function minimumCents(lines: readonly AllocationRuleLine[]): bigint | null {
  let need = 0n;
  for (const i of pipelineOrder(lines).reverse()) {
    const line = lines[i];
    if (line.type === allocationType.Fixed) {
      need += cents(line.amount);
    } else if (need > 0n) {
      const p = hundredths(line.percent);
      if (p >= WHOLE) return null;
      need = ((need - 1n) * WHOLE) / (WHOLE - p) + 1n;
    }
  }
  return need;
}

/**
 * Client mirror of the server's minimum (Domain `AllocationCalculator.Minimum`): an income below it
 * is refused. `null` when the steps cannot be funded at all. Exact in integer cents: 50% then Fixed
 * 100 gives 199.99, 33.33% then Fixed 100 gives 149.98.
 */
export function minimumAmount(lines: readonly AllocationRuleLine[]): number | null {
  const need = minimumCents(lines);
  return need === null ? null : toAmount(need);
}

export interface PipelineRow {
  index: number;
  fundId: string;
  amount: number;
  left: number;
  short: number;
}

export interface PipelineRun {
  minimum: number | null;
  enough: boolean;
  rows: PipelineRow[];
  unallocated: number;
}

export function runPipeline(income: number, lines: readonly AllocationRuleLine[]): PipelineRun {
  const need = minimumCents(lines);
  let remaining = BigInt(Math.max(0, Math.round((Number.isFinite(income) ? income : 0) * 100)));
  const enough = need !== null && remaining >= need;
  const rows = pipelineOrder(lines).map((i): PipelineRow => {
    const line = lines[i];
    const want =
      line.type === allocationType.Fixed
        ? cents(line.amount)
        : (remaining * hundredths(line.percent)) / WHOLE;
    const take = want < remaining ? want : remaining;
    remaining -= take;
    return {
      index: i,
      fundId: line.fundId,
      amount: toAmount(take),
      left: toAmount(remaining),
      short: toAmount(want - take),
    };
  });
  return {
    minimum: need === null ? null : toAmount(need),
    enough,
    rows,
    unallocated: toAmount(remaining),
  };
}

export function previewAllocation(
  income: number,
  lines: readonly AllocationRuleLine[],
): { lines: FundAmount[]; unallocated: number } | null {
  const run = runPipeline(income, lines);
  if (!run.enough || !(income > 0)) return null;
  return {
    lines: run.rows.filter((r) => r.amount > 0).map((r) => ({ fundId: r.fundId, amount: r.amount })),
    unallocated: run.unallocated,
  };
}

export type StepProblem =
  | ''
  | 'empty'
  | 'fund'
  | 'value'
  | 'decimals'
  | 'percentRange'
  | 'hundredNotLast'
  | 'tooLarge';

const hasTwoDecimals = (value: number): boolean => Math.abs(value * 100 - Math.round(value * 100)) < 1e-6;

export function stepsProblem(rules: readonly AllocationRule[]): StepProblem {
  if (rules.length === 0) return 'empty';
  if (rules.some((r) => !r.fundId)) return 'fund';
  for (const r of rules) {
    if (r.value === null || !Number.isFinite(r.value)) return 'value';
    if (r.kind === 'Fixed' && r.value <= 0) return 'value';
    if (r.kind === 'Percent' && (r.value <= 0 || r.value > 100)) return 'percentRange';
    if (!hasTwoDecimals(r.value)) return 'decimals';
  }
  if (rules.slice(0, -1).some((r) => r.kind === 'Percent' && r.value === 100)) {
    return 'hundredNotLast';
  }
  const need = minimumCents(toAllocationLines(rules));
  return need === null || need > MAX_CENTS ? 'tooLarge' : '';
}
