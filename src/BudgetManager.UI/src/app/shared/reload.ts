import { effect, untracked } from '@angular/core';
import { LedgerService } from './ledger';

export function reloadOnTick(ledgers: LedgerService, reload: () => void): void {
  let initial = true;
  effect(() => {
    ledgers.reloadTick();
    if (initial) {
      initial = false;
      return;
    }
    untracked(reload);
  });
}
