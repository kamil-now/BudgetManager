import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { translate } from '../../i18n/i18n';
import { TranslatePipe } from '../../i18n/t.pipe';
import { AccountService } from '../../shared/account';
import { AllocationApi } from '../../shared/allocation-api';
import { FeedItemDTO } from '../../shared/feed-models';
import { httpErrorMessage } from '../../shared/http-error';
import { LedgerService } from '../../shared/ledger';
import { Modal } from '../../shared/modal';
import { formatMoney } from '../../shared/money';
import { feedRow } from '../dashboard-data';

@Component({
  selector: 'app-confirm-delete',
  imports: [Modal, TranslatePipe],
  templateUrl: './confirm-delete.html',
})
export class ConfirmDelete {
  private readonly accountsApi = inject(AccountService);
  private readonly ledgers = inject(LedgerService);
  private readonly allocations = inject(AllocationApi);
  private readonly destroyRef = inject(DestroyRef);

  readonly item = input<FeedItemDTO | null>(null);
  readonly closed = output<void>();
  readonly saved = output<void>();

  protected readonly formatMoney = formatMoney;
  protected readonly submitting = signal(false);
  protected readonly error = signal('');
  protected readonly row = computed(() => {
    const item = this.item();
    return item ? feedRow(item) : null;
  });

  constructor() {
    effect(() => {
      this.item();
      this.submitting.set(false);
      this.error.set('');
    });
  }

  protected confirm(): void {
    const row = this.row();
    const ledgerId = this.ledgers.currentLedgerId();
    if (!row || this.submitting()) return;
    const { item } = row;
    let request: Observable<void>;
    switch (row.kind) {
      case 'income':
      case 'expense':
        request = this.accountsApi.deleteTransaction(item.accountId ?? '', item.id);
        break;
      case 'transfer':
      case 'exchange':
        request = this.accountsApi.deleteTransfer(item.accountId ?? '', item.id);
        break;
      default:
        if (!ledgerId) return;
        request = this.allocations.deleteAllocation(ledgerId, item.id);
    }

    this.error.set('');
    this.submitting.set(true);
    request.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.submitting.set(false);
        this.ledgers.requestReload();
        this.saved.emit();
      },
      error: (response: HttpErrorResponse) => {
        this.submitting.set(false);
        this.error.set(
          httpErrorMessage(response, {
            invalid: translate('delete.invalid'),
            gone: translate('delete.gone'),
            fallback: translate('delete.fallback'),
          }),
        );
      },
    });
  }
}
