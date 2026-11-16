import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal, untracked } from '@angular/core';
import { Subscription } from 'rxjs';
import { CurrencyTotal, currencyTotals } from '../dashboard/dashboard-data';
import { LedgerService } from './ledger';
import { AccountDTO, BudgetDTO, LedgerDTO } from './models';
import { PageState } from './page-status';

@Injectable({ providedIn: 'root' })
export class LedgerStore {
  private readonly ledgers = inject(LedgerService);

  private readonly _ledger = signal<LedgerDTO | null>(null);
  private readonly _status = signal<PageState>('loading');
  private request?: Subscription;

  readonly ledger = this._ledger.asReadonly();
  readonly status = this._status.asReadonly();
  readonly accounts = computed<AccountDTO[]>(() => this._ledger()?.accounts ?? []);
  readonly budgets = computed<BudgetDTO[]>(() => this._ledger()?.budgets ?? []);
  readonly totals = computed<CurrencyTotal[]>(() => {
    const ledger = this._ledger();
    return ledger ? currencyTotals(ledger) : [];
  });

  constructor() {
    effect(() => {
      const id = this.ledgers.currentLedgerId();
      this.ledgers.reloadTick();
      untracked(() => this.load(id, true));
    });
  }

  retry(): void {
    this.load(this.ledgers.currentLedgerId(), false);
  }

  private load(id: string | null, keepOnScreen: boolean): void {
    this.request?.unsubscribe();
    if (!id) {
      this._ledger.set(null);
      this._status.set('no-ledger');
      return;
    }
    const quiet = keepOnScreen && this._status() === 'ready' && this._ledger()?.id === id;
    if (!quiet) {
      if (this._ledger()?.id !== id) this._ledger.set(null);
      this._status.set('loading');
    }
    this.request = this.ledgers.getLedger(id).subscribe({
      next: (ledger) => {
        this.ledgers.currentLedgerName.set(ledger.name);
        this._ledger.set(ledger);
        this._status.set('ready');
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 404 || error.status === 403) {
          this.ledgers.clearCurrentLedger();
          this.ledgers.refreshLedgers();
          this._ledger.set(null);
          this._status.set('no-ledger');
          return;
        }
        if (!quiet) this._status.set('error');
      },
    });
  }
}
