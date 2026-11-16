import { Component, effect, inject, signal, untracked } from '@angular/core';
import { TranslationKey } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { LedgerStore } from '../shared/ledger-store';
import { LedgerDTO } from '../shared/models';
import { PageStatus } from '../shared/page-status';
import { UiState } from '../shared/ui-state';
import { Viewport } from '../shared/viewport';
import { BalanceCard } from '../shell/balance-card/balance-card';
import { AccountsPanel } from './accounts-panel/accounts-panel';
import { BudgetsPanel } from './budget-panel/budgets-panel';
import { Scope } from './dashboard-data';
import { TransactionsPanel } from './transactions-panel/transactions-panel';

@Component({
  selector: 'app-dashboard',
  imports: [AccountsPanel, BalanceCard, BudgetsPanel, PageStatus, TransactionsPanel, TranslatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard {
  protected readonly store = inject(LedgerStore);
  protected readonly viewport = inject(Viewport);

  protected readonly scope = signal<Scope | null>(null);

  protected readonly tabs: { id: Tab; label: TranslationKey }[] = [
    { id: 'accounts', label: 'accounts.title' },
    { id: 'budgets', label: 'budgets.title' },
    { id: 'transactions', label: 'tx.title' },
  ];
  protected readonly tab = signal<Tab>('accounts');

  private readonly uiState = inject(UiState);
  private restoredFor: string | null = null;

  constructor() {
    effect(() => {
      const ledger = this.store.ledger();
      const scope = this.scope();
      if (!ledger) return;
      untracked(() => {
        const key = `scope.${ledger.id}`;
        if (this.restoredFor !== ledger.id) {
          this.restoredFor = ledger.id;
          const saved = this.uiState.read<Scope>(key);
          this.scope.set(saved && scopeExists(ledger, saved) ? saved : null);
        } else if (!scopeExists(ledger, scope)) {
          this.scope.set(null);
        } else {
          this.uiState.write(key, scope);
        }
      });
    });
  }

  protected retry(): void {
    this.store.retry();
  }
}

type Tab = 'accounts' | 'budgets' | 'transactions';

function scopeExists(ledger: LedgerDTO, scope: Scope | null): boolean {
  if (!scope) return true;
  switch (scope.kind) {
    case 'account':
      return ledger.accounts.some((a) => a.id === scope.id);
    case 'budget':
      return ledger.budgets.some((b) => b.id === scope.id);
    case 'fund':
      return ledger.budgets.some((b) => b.funds.some((f) => f.id === scope.id));
  }
}
