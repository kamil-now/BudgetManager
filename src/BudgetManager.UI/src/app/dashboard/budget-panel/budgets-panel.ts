import { Component, inject, input, output } from '@angular/core';
import { TranslatePipe } from '../../i18n/t.pipe';
import { BudgetDTO } from '../../shared/models';
import { UiActions } from '../../shared/ui-actions';
import { Scope } from '../dashboard-data';
import { BudgetCard } from '../budget-card/budget-card';

@Component({
  selector: 'app-budgets-panel',
  imports: [BudgetCard, TranslatePipe],
  templateUrl: './budgets-panel.html',
  styleUrl: './budgets-panel.scss',
})
export class BudgetsPanel {
  private readonly ui = inject(UiActions);

  readonly budgets = input.required<BudgetDTO[]>();
  readonly scope = input<Scope | null>(null);
  readonly scopeChange = output<Scope | null>();

  protected newBudget(): void {
    this.ui.open({ kind: 'budget' });
  }

  protected openLedgerTemplates(): void {
    this.ui.open({ kind: 'ledgerTemplates', budgets: this.budgets() });
  }
}
