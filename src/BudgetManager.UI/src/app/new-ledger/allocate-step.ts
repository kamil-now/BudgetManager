import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { formatMoney } from '../shared/money';
import {
  AllocationPlan,
  PlanBudget,
  TemplateOption,
  fundKey,
  leftover,
  refill,
  templateOptions,
} from './initial-allocation';

@Component({
  selector: 'app-allocate-step',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './allocate-step.html',
  styleUrl: './allocate-step.scss',
})
export class AllocateStep {
  readonly budgets = input.required<readonly PlanBudget[]>();
  readonly plans = input.required<readonly AllocationPlan[]>();
  readonly changed = output<AllocationPlan[]>();

  protected readonly formatMoney = formatMoney;
  protected readonly key = fundKey;

  protected options(plan: AllocationPlan): TemplateOption[] {
    return templateOptions(this.budgets(), plan.currency, plan.total);
  }

  protected optionId(option: { budget: number; template: number }): string {
    return `${option.budget}:${option.template}`;
  }

  protected optionLabel(option: TemplateOption, plan: AllocationPlan): string {
    if (option.usable) return option.label;
    const amount = formatMoney(option.minimum ?? 0, plan.currency);
    return translate('split.needsAtLeast', { name: option.label, amount });
  }

  protected left(plan: AllocationPlan): number {
    return leftover(plan.total, plan.amounts);
  }

  protected allocated(plan: AllocationPlan): number {
    return (Math.round(plan.total * 100) - Math.round(this.left(plan) * 100)) / 100;
  }

  protected inputId(plan: AllocationPlan, budget: number, fund: number): string {
    return `ia-${plan.currency}-${budget}-${fund}`;
  }

  protected setAmount(plan: AllocationPlan, budget: number, fund: number, value: number | null): void {
    plan.amounts[fundKey(budget, fund)] = value;
    this.emit();
  }

  protected addRest(plan: AllocationPlan, budget: number, fund: number): void {
    const key = fundKey(budget, fund);
    const current = Math.round((plan.amounts[key] ?? 0) * 100);
    const left = Math.round(this.left(plan) * 100);
    plan.amounts[key] = Math.max(0, current + left) / 100;
    this.emit();
  }

  protected fillFrom(plan: AllocationPlan, id: string): void {
    const [budget, template] = id.split(':').map(Number);
    const next = refill(plan, this.budgets(), { budget, template });
    this.changed.emit(this.plans().map((p) => (p === plan ? next : p)));
  }

  private emit(): void {
    this.changed.emit([...this.plans()]);
  }
}
