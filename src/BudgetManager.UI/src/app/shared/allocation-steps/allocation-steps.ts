import {
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  inject,
  input,
  model,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { translate } from '../../i18n/i18n';
import { TranslatePipe } from '../../i18n/t.pipe';
import {
  AllocationKind,
  AllocationRule,
  StepProblem,
  allocationType,
  minimumAmount,
  runPipeline,
  stepsProblem,
  toAllocationLines,
} from '../allocation';
import { formatMoney } from '../money';

export interface StepFund {
  id: string;
  label: string;
  group?: string;
}

export interface StepDraft extends AllocationRule {
  key: number;
}

let draftSeq = 0;
let hostSeq = 0;

export function newStep(
  fundId = '',
  kind: AllocationKind = 'Fixed',
  value: number | null = null,
): StepDraft {
  return { key: ++draftSeq, fundId, kind, value };
}

export function stepsFromLines(
  lines: readonly {
    fundId: string;
    sequence: number;
    type: number;
    amount?: number | null;
    percent?: number | null;
  }[],
): StepDraft[] {
  return [...lines]
    .sort((a, b) => a.sequence - b.sequence)
    .map((l) => {
      const percent = l.type === allocationType.Percent;
      return newStep(l.fundId, percent ? 'Percent' : 'Fixed', (percent ? l.percent : l.amount) ?? null);
    });
}

export type StepTarget = 'fund' | 'budget';

export function stepProblemText(problem: StepProblem, target: StepTarget = 'fund'): string {
  switch (problem) {
    case 'empty':
      return translate('templates.atLeastOne');
    case 'fund':
      return translate(target === 'budget' ? 'split.budgetEveryLine' : 'split.fundEveryLine');
    case 'value':
      return translate('error.lineValue');
    case 'decimals':
      return translate('error.twoDecimalPlaces');
    case 'percentRange':
      return translate('steps.percentRange');
    case 'hundredNotLast':
      return translate('steps.hundredNotLast');
    case 'tooLarge':
      return translate('steps.tooLarge');
    default:
      return '';
  }
}

@Component({
  selector: 'app-allocation-steps',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './allocation-steps.html',
  styleUrl: './allocation-steps.scss',
})
export class AllocationSteps {
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly funds = input<readonly StepFund[]>([]);
  readonly currency = input('EUR');
  readonly steps = model<StepDraft[]>([]);
  readonly sample = input<number | null>(null);
  /** What the steps allocate to: the funds of a budget's template or the budgets of a ledger's template. */
  readonly target = input<StepTarget>('fund');

  protected readonly id = `steps-${++hostSeq}`;
  protected readonly formatMoney = formatMoney;

  protected readonly groups = computed(() => {
    const groups: { label: string | null; funds: StepFund[] }[] = [];
    for (const fund of this.funds()) {
      const label = fund.group ?? null;
      let group = groups.find((g) => g.label === label);
      if (!group) groups.push((group = { label, funds: [] }));
      group.funds.push(fund);
    }
    return groups;
  });

  private readonly lines = computed(() => toAllocationLines(this.steps()));
  protected readonly minimum = computed(() => minimumAmount(this.lines()));
  protected readonly problem = computed(() => stepProblemText(stepsProblem(this.steps()), this.target()));

  protected readonly run = computed(() => {
    const sample = this.sample();
    return runPipeline(sample !== null && sample > 0 ? sample : (this.minimum() ?? 0), this.lines());
  });

  protected readonly belowMinimum = computed(() => {
    const sample = this.sample();
    const minimum = this.minimum();
    return sample !== null && sample > 0 && minimum !== null && sample < minimum;
  });

  protected add(): void {
    const used = new Set(this.steps().map((s) => s.fundId));
    const funds = this.funds();
    const fund = funds.find((f) => !used.has(f.id)) ?? funds[0];
    this.steps.update((steps) => [...steps, newStep(fund?.id ?? '')]);
    this.focusAfterRender(`#${this.id}-${draftSeq}-fund`);
  }

  protected remove(key: number): void {
    this.steps.update((steps) => steps.filter((s) => s.key !== key));
    this.focusAfterRender(`#${this.id}-add`);
  }

  protected patch(key: number, patch: Partial<AllocationRule>): void {
    this.steps.update((steps) => steps.map((s) => (s.key === key ? { ...s, ...patch } : s)));
  }

  protected setKind(step: StepDraft, kind: AllocationKind): void {
    if (step.kind !== kind) this.patch(step.key, { kind, value: null });
  }

  protected move(index: number, delta: -1 | 1): void {
    const steps = [...this.steps()];
    const target = index + delta;
    if (target < 0 || target >= steps.length) return;
    [steps[index], steps[target]] = [steps[target], steps[index]];
    this.steps.set(steps);
    const prefix = `#${this.id}-${steps[target].key}`;
    const [same, other] = delta < 0 ? ['up', 'down'] : ['down', 'up'];
    afterNextRender(
      () => {
        const root = this.host.nativeElement;
        const button = root.querySelector<HTMLButtonElement>(`${prefix}-${same}`);
        const fallback = root.querySelector<HTMLElement>(`${prefix}-${other}`);
        (button && !button.disabled ? button : fallback)?.focus();
      },
      { injector: this.injector },
    );
  }

  private focusAfterRender(selector: string): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>(selector)?.focus(),
      { injector: this.injector },
    );
  }
}
