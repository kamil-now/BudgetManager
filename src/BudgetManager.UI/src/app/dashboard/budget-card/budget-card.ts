import { Component, computed, inject, input, output } from '@angular/core';
import { translate } from '../../i18n/i18n';
import { TranslatePipe } from '../../i18n/t.pipe';
import { MenuButton, MenuEntry } from '../../shared/menu-button';
import { BudgetDTO, FundDTO } from '../../shared/models';
import { balanceLines } from '../../shared/money';
import { UiActions } from '../../shared/ui-actions';
import { UiState } from '../../shared/ui-state';
import { Scope, isScope } from '../dashboard-data';

@Component({
  selector: 'app-budget-card',
  imports: [MenuButton, TranslatePipe],
  templateUrl: './budget-card.html',
  styleUrl: './budget-card.scss',
  host: { class: 'budget' },
})
export class BudgetCard {
  private readonly ui = inject(UiActions);

  readonly budget = input.required<BudgetDTO>();
  readonly scope = input<Scope | null>(null);
  readonly scopeChange = output<Scope | null>();

  protected readonly balanceLines = balanceLines;

  private readonly uiState = inject(UiState);
  private readonly open = this.uiState.persistedSet('budgets.expanded');

  protected readonly expanded = computed(() => this.open().has(this.budget().id));

  protected toggleExpanded(): void {
    this.uiState.toggle(this.open, this.budget().id);
  }

  protected isBudgetScoped(budget: BudgetDTO): boolean {
    return isScope(this.scope(), 'budget', budget.id);
  }

  protected isFundScoped(fund: FundDTO): boolean {
    return isScope(this.scope(), 'fund', fund.id);
  }

  protected toggleBudget(budget: BudgetDTO): void {
    this.scopeChange.emit(
      this.isBudgetScoped(budget) ? null : { kind: 'budget', id: budget.id, name: budget.name },
    );
  }

  protected toggleFund(budget: BudgetDTO, fund: FundDTO): void {
    this.scopeChange.emit(
      this.isFundScoped(fund)
        ? null
        : { kind: 'fund', id: fund.id, name: fund.name, budgetName: budget.name },
    );
  }

  protected readonly menu = computed((): MenuEntry[] => [
    { type: 'item', id: 'edit', label: translate('budget.edit') },
    { type: 'item', id: 'templates', label: translate('budgets.templates') },
  ]);

  protected onAction(action: string): void {
    const budget = this.budget();
    if (action === 'edit') this.ui.open({ kind: 'budget', budget });
    if (action === 'templates') this.ui.open({ kind: 'templates', budget });
  }
}
