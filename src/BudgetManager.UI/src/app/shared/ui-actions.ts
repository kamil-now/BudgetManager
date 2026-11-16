import { Injectable, signal } from '@angular/core';
import {
  AllocationCopy,
  AllocationEdit,
  FeedItemDTO,
  TransactionCopy,
  TransferCopy,
  TransferRef,
} from './feed-models';
import { AccountDTO, AccountTransactionDTO, BudgetDTO } from './models';

export type TransactionKind = 'income' | 'expense';

export type ActiveDialog =
  | {
    kind: 'transaction';
    initialKind: TransactionKind;
    transaction?: AccountTransactionDTO;
    copy?: TransactionCopy;
    startAllocating?: boolean;
  }
  | { kind: 'transfer'; transfer?: TransferRef; copy?: TransferCopy }
  | { kind: 'exchange'; transfer?: TransferRef; copy?: TransferCopy }
  | { kind: 'confirmDelete'; item: FeedItemDTO }
  | { kind: 'account'; account?: AccountDTO }
  | { kind: 'budget'; budget?: BudgetDTO }
  | { kind: 'templates'; budget: BudgetDTO }
  | { kind: 'ledgerTemplates'; budgets: readonly BudgetDTO[] }
  | {
    kind: 'allocate';
    currency?: string;
    income?: AccountTransactionDTO;
    allocation?: AllocationEdit;
    copy?: AllocationCopy;
  };

export type DialogKind = ActiveDialog['kind'];
export type DialogOf<K extends DialogKind> = Extract<ActiveDialog, { kind: K }>;

@Injectable({ providedIn: 'root' })
export class UiActions {
  private readonly _active = signal<ActiveDialog | null>(null);
  readonly active = this._active.asReadonly();

  open(dialog: ActiveDialog): void {
    this._active.set(dialog);
  }

  close(): void {
    this._active.set(null);
  }

  of<K extends DialogKind>(kind: K): DialogOf<K> | null {
    const active = this._active();
    return active?.kind === kind ? (active as DialogOf<K>) : null;
  }
}
