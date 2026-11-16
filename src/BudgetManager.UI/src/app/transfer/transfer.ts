import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Observable, Subscription } from 'rxjs';
import { translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { AccountService, TransferBody } from '../shared/account';
import { calendarDate, toIso, today } from '../shared/date';
import { TransferCopy, TransferRef } from '../shared/feed-models';
import { httpErrorMessage } from '../shared/http-error';
import { LedgerService } from '../shared/ledger';
import { Modal } from '../shared/modal';
import { AccountDTO } from '../shared/models';
import { currencyOptions, formatMoney, hasAtMostTwoDecimals } from '../shared/money';
import { PreferencesService } from '../shared/preferences';

@Component({
  selector: 'app-transfer',
  imports: [FormsModule, Modal, TranslatePipe],
  templateUrl: './transfer.html',
  styleUrl: './transfer.scss',
})
export class Transfer {
  private readonly accountsApi = inject(AccountService);
  private readonly ledgers = inject(LedgerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly prefs = inject(PreferencesService);

  readonly open = input(false);
  readonly accounts = input<AccountDTO[]>([]);
  readonly transfer = input<TransferRef | null>(null);
  readonly copy = input<TransferCopy | null>(null);
  readonly closed = output<void>();
  readonly saved = output<void>();

  protected readonly submitting = signal(false);
  protected readonly loading = signal(false);
  protected readonly error = signal('');

  protected readonly fromId = signal('');
  protected readonly toId = signal('');
  protected readonly amount = signal<number | null>(null);
  protected readonly currency = signal('EUR');
  protected readonly currencyOptions = computed(() =>
    currencyOptions(this.prefs.favoriteCurrencies(), [this.currency()]),
  );

  protected date = today();
  protected title = '';
  private loadRequest?: Subscription;

  protected readonly isEdit = computed(() => this.transfer() !== null);

  protected readonly summary = computed(() => {
    const amount = this.amount();
    const name = (id: string) => this.accounts().find((a) => a.id === id)?.name ?? '…';
    return {
      amount: formatMoney(amount !== null && amount > 0 ? amount : 0, this.currency()),
      from: name(this.fromId()),
      to: name(this.toId()),
    };
  });

  constructor() {
    effect(() => {
      if (!this.open()) return;
      const transfer = this.transfer();
      const copy = this.copy();
      untracked(() => this.reset(transfer, copy));
    });
  }

  protected submit(): void {
    if (this.submitting()) return;
    const amount = this.amount();
    if (this.loading()) return this.error.set(translate('transfer.stillLoading'));
    if (!this.fromId() || !this.toId()) return this.error.set(translate('transfer.chooseBoth'));
    if (this.fromId() === this.toId()) {
      return this.error.set(translate('transfer.different'));
    }
    if (amount === null || !Number.isFinite(amount) || amount <= 0) {
      return this.error.set(translate('error.amountPositive'));
    }
    if (!hasAtMostTwoDecimals(amount)) return this.error.set(translate('error.twoDecimals'));
    if (!this.date) return this.error.set(translate('error.chooseDate'));

    const body: TransferBody = {
      targetAccountId: this.toId(),
      value: { amount, currency: this.currency() },
      date: toIso(this.date),
      title: this.title.trim() || null,
    };
    const edited = this.transfer();
    const request: Observable<unknown> = edited
      ? this.accountsApi.updateTransfer(this.fromId(), edited.id, body)
      : this.accountsApi.createTransfer(this.fromId(), body);

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
            invalid: translate('transfer.invalid'),
            gone: translate('error.accountGone'),
            fallback: translate('transfer.fallback'),
          }),
        );
      },
    });
  }

  private reset(transfer: TransferRef | null, copy: TransferCopy | null): void {
    const [first, second] = this.accounts();
    this.loadRequest?.unsubscribe();
    this.error.set('');
    this.submitting.set(false);
    this.loading.set(false);
    this.fromId.set(first?.id ?? '');
    this.toId.set(second?.id ?? '');
    this.setMoney(null, this.prefs.defaultCurrency());
    this.date = today();
    this.title = '';
    if (copy) {
      this.fill(copy.accountId, copy.targetAccountId ?? '', copy.value.amount, copy.value.currency);
      this.title = copy.title ?? '';
    }
    if (transfer) this.load(transfer);
  }

  private load(ref: TransferRef): void {
    this.loading.set(true);
    this.loadRequest = this.accountsApi
      .getTransfer(ref.accountId, ref.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (t) => {
          this.loading.set(false);
          this.fill(t.accountId, t.targetAccountId ?? '', t.value.amount, t.value.currency);
          this.date = calendarDate(t.date);
          this.title = t.title ?? '';
        },
        error: () => {
          this.loading.set(false);
          this.error.set(translate('transfer.loadError'));
        },
      });
  }

  private fill(fromId: string, toId: string, amount: number, currency: string): void {
    this.fromId.set(fromId);
    if (toId) this.toId.set(toId);
    this.setMoney(Math.abs(amount), currency);
  }

  private setMoney(amount: number | null, currency: string): void {
    this.amount.set(amount);
    this.currency.set(currency);
  }
}
