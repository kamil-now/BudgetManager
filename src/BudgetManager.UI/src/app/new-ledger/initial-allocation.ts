import {
  AllocationKind,
  AllocationRuleLine,
  allocationType,
  minimumAmount,
  previewAllocation,
} from '../shared/allocation';
import { InitialAllocation } from '../shared/allocation-models';

export interface PlanFund {
  name: string;
}

export interface PlanStep {
  fund: number;
  kind: AllocationKind;
  value: number | null;
}

export interface PlanTemplate {
  name: string;
  currency: string;
  steps: readonly PlanStep[];
}

export interface PlanBudget {
  name: string;
  funds: readonly PlanFund[];
  templates: readonly PlanTemplate[];
}

export interface PlanAccount {
  amount: number | null;
  currency: string;
}

export type FundAmounts = Record<string, number | null>;

export interface PlanSource {
  budget: number;
  template: number;
}

export interface AllocationPlan {
  currency: string;
  total: number;
  source: PlanSource | null;
  seed: string;
  amounts: FundAmounts;
}

export interface TemplateOption {
  budget: number;
  template: number;
  label: string;
  minimum: number | null;
  usable: boolean;
}

export const fundKey = (budget: number, fund: number): string => `${budget}:${fund}`;

const cents = (amount: number | null | undefined): number => Math.round((amount ?? 0) * 100);

export function accountTotals(accounts: readonly PlanAccount[]): { currency: string; total: number }[] {
  const totals = new Map<string, number>();
  for (const a of accounts) totals.set(a.currency, (totals.get(a.currency) ?? 0) + cents(a.amount));
  return [...totals]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([currency, total]) => ({ currency, total: total / 100 }));
}

function linesOf(budget: number, template: PlanTemplate): AllocationRuleLine[] {
  return template.steps.map((s, i) => ({
    fundId: fundKey(budget, s.fund),
    sequence: i,
    type: allocationType[s.kind],
    amount: s.kind === 'Fixed' ? s.value : null,
    percent: s.kind === 'Percent' ? s.value : null,
  }));
}

export function templateOptions(
  budgets: readonly PlanBudget[],
  currency: string,
  total: number,
): TemplateOption[] {
  const options: TemplateOption[] = [];
  budgets.forEach((budget, b) =>
    budget.templates.forEach((template, t) => {
      if (template.currency !== currency || template.steps.length === 0) return;
      const minimum = minimumAmount(linesOf(b, template));
      options.push({
        budget: b,
        template: t,
        label: `${budget.name.trim()} / ${template.name.trim()}`,
        minimum,
        usable: minimum !== null && total > 0 && cents(total) >= cents(minimum),
      });
    }),
  );
  return options;
}

export function prefill(
  total: number,
  budgets: readonly PlanBudget[],
  source: PlanSource | null,
): FundAmounts {
  const template = source ? budgets[source.budget]?.templates[source.template] : undefined;
  if (!source || !template || total <= 0) return {};
  const result = previewAllocation(total, linesOf(source.budget, template));
  const amounts: FundAmounts = {};
  for (const line of result?.lines ?? []) {
    amounts[line.fundId] = (cents(amounts[line.fundId]) + cents(line.amount)) / 100;
  }
  return amounts;
}

export function leftover(total: number, amounts: FundAmounts): number {
  const allocated = Object.values(amounts).reduce((sum: number, a) => sum + cents(a), 0);
  return (cents(total) - allocated) / 100;
}

export function seedOf(
  total: number,
  budgets: readonly PlanBudget[],
  source: PlanSource | null,
): string {
  const steps = source ? (budgets[source.budget]?.templates[source.template]?.steps ?? []) : [];
  return JSON.stringify({ total, source, steps });
}

export function planAllocations(
  budgets: readonly PlanBudget[],
  accounts: readonly PlanAccount[],
  previous: readonly AllocationPlan[],
): AllocationPlan[] {
  return accountTotals(accounts).map(({ currency, total }) => {
    const before = previous.find((p) => p.currency === currency);
    const options = templateOptions(budgets, currency, total);
    const kept = options.find(
      (o) => o.usable && o.budget === before?.source?.budget && o.template === before?.source?.template,
    );
    const chosen = kept ?? options.find((o) => o.usable);
    const source = chosen ? { budget: chosen.budget, template: chosen.template } : null;
    const seed = seedOf(total, budgets, source);
    const amounts =
      before && before.seed === seed
        ? keepExisting(before.amounts, budgets)
        : prefill(total, budgets, source);
    return { currency, total, source, seed, amounts };
  });
}

export function refill(
  plan: AllocationPlan,
  budgets: readonly PlanBudget[],
  source: PlanSource,
): AllocationPlan {
  return {
    ...plan,
    source,
    seed: seedOf(plan.total, budgets, source),
    amounts: prefill(plan.total, budgets, source),
  };
}

export function toInitialAllocations(
  budgets: readonly PlanBudget[],
  plans: readonly AllocationPlan[],
): InitialAllocation[] {
  const result: InitialAllocation[] = [];
  for (const plan of plans) {
    if (plan.total <= 0) continue;
    budgets.forEach((budget, b) =>
      budget.funds.forEach((fund, f) => {
        const amount = plan.amounts[fundKey(b, f)] ?? 0;
        if (cents(amount) <= 0) return;
        result.push({
          budget: budget.name.trim(),
          fund: fund.name.trim(),
          value: { amount: cents(amount) / 100, currency: plan.currency },
        });
      }),
    );
  }
  return result;
}

function keepExisting(amounts: FundAmounts, budgets: readonly PlanBudget[]): FundAmounts {
  const kept: FundAmounts = {};
  budgets.forEach((budget, b) =>
    budget.funds.forEach((_, f) => {
      const key = fundKey(b, f);
      if (key in amounts) kept[key] = amounts[key];
    }),
  );
  return kept;
}
