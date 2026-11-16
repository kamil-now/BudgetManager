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
import { AccountService, CurrencyExchangeBody } from '../shared/account';
import { calendarDate, toIso, today } from '../shared/date';
import { TransferCopy, TransferRef } from '../shared/feed-models';
import { httpErrorMessage } from '../shared/http-error';
import { LedgerService } from '../shared/ledger';
import { Modal } from '../shared/modal';
import { AccountDTO, Money } from '../shared/models';
import { currencyOptions, formatNumber, hasAtMostTwoDecimals } from '../shared/money';
import { PreferencesService } from '../shared/preferences';

@Component({
  selector: 'app-exchange',
  imports: [FormsModule, Modal, TranslatePipe],
  templateUrl: './exchange.html',
  styleUrl: './exchange.scss',
})
export class Exchange {
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

  protected readonly accountId = signal('');
  protected readonly targetId = signal('');
  protected readonly targets = computed(() =>
    this.accounts().filter((a) => a.id !== this.accountId()),
  );
  protected readonly sellAmount = signal<number | null>(null);
  protected readonly sellCurrency = signal('EUR');
  protected readonly buyAmount = signal<number | null>(null);
  protected readonly buyCurrency = signal('USD');
  protected readonly currencyOptions = computed(() =>
    currencyOptions(this.prefs.favoriteCurrencies(), [this.sellCurrency(), this.buyCurrency()]),
  );

  protected date = today();
  protected title = '';
  private loadRequest?: Subscription;

  protected readonly isEdit = computed(() => this.transfer() !== null);
  protected readonly sameCurrency = computed(() => this.sellCurrency() === this.buyCurrency());

  protected readonly rate = computed(() => {
    const sell = this.sellAmount();
    const buy = this.buyAmount();
    if (!isPositive(sell) || !isPositive(buy)) return translate('exchange.rateHint');
    const rate = formatNumber(buy / sell, 4, 4, false);
    return `1 ${this.sellCurrency()} = ${rate} ${this.buyCurrency()}`;
  });

  constructor() {
    effect(() => {
      if (!this.open()) return;
      const transfer = this.transfer();
      const copy = this.copy();
      untracked(() => this.reset(transfer, copy));
    });
  }

  protected setAccount(id: string): void {
    this.accountId.set(id);
    if (this.targetId() === id) this.targetId.set('');
  }

  protected submit(): void {
    if (this.submitting()) return;
    const sell = this.sellAmount();
    const buy = this.buyAmount();
    const target = this.targetId();
    if (this.loading()) return this.error.set(translate('exchange.stillLoading'));
    if (!this.accountId()) return this.error.set(translate('error.chooseAccount'));
    if (target && target === this.accountId()) {
      return this.error.set(translate('exchange.differentTarget'));
    }
    if (this.sameCurrency()) return; // the template already shows the inline message
    if (!isPositive(sell) || !isPositive(buy)) {
      return this.error.set(translate('exchange.bothAmounts'));
    }
    if (!hasAtMostTwoDecimals(sell) || !hasAtMostTwoDecimals(buy)) {
      return this.error.set(translate('error.twoDecimals'));
    }
    if (!this.date) return this.error.set(translate('error.chooseDate'));

    const body: CurrencyExchangeBody = {
      targetAccountId: target || null,
      sell: { amount: sell, currency: this.sellCurrency() },
      buy: { amount: buy, currency: this.buyCurrency() },
      date: toIso(this.date),
      title: this.title.trim() || null,
    };
    const edited = this.transfer();
    const request: Observable<unknown> = edited
      ? this.accountsApi.updateCurrencyExchange(this.accountId(), edited.id, body)
      : this.accountsApi.createCurrencyExchange(this.accountId(), body);

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
            invalid: translate('exchange.invalid'),
            gone: translate('error.accountGone'),
            fallback: translate('exchange.fallback'),
          }),
        );
      },
    });
  }

  private reset(transfer: TransferRef | null, copy: TransferCopy | null): void {
    const [first] = this.accounts();
    this.loadRequest?.unsubscribe();
    this.error.set('');
    this.submitting.set(false);
    this.loading.set(false);
    this.accountId.set(first?.id ?? '');
    this.targetId.set('');
    this.sellAmount.set(null);
    this.buyAmount.set(null);
    const sell = this.prefs.defaultCurrency();
    const buy = [...this.prefs.favoriteCurrencies(), 'USD', 'EUR'].find((c) => c !== sell) ?? 'USD';
    this.setCurrencies(sell, buy);
    this.date = today();
    this.title = '';
    if (copy) {
      this.fill(copy.accountId, copy.targetAccountId ?? '', copy.value, copy.received ?? null);
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
          this.fill(t.accountId, t.targetAccountId ?? '', t.value, t.received ?? null);
          this.date = calendarDate(t.date);
          this.title = t.title ?? '';
        },
        error: () => {
          this.loading.set(false);
          this.error.set(translate('exchange.loadError'));
        },
      });
  }

  private fill(accountId: string, targetId: string, sold: Money, bought: Money | null): void {
    this.accountId.set(accountId);
    this.targetId.set(targetId && targetId !== accountId ? targetId : '');
    this.sellAmount.set(Math.abs(sold.amount));
    this.buyAmount.set(bought ? Math.abs(bought.amount) : null);
    this.setCurrencies(sold.currency, bought?.currency ?? this.buyCurrency());
  }

  private setCurrencies(sell: string, buy: string): void {
    this.sellCurrency.set(sell);
    this.buyCurrency.set(buy);
  }
}

function isPositive(value: number | null): value is number {
  return value !== null && Number.isFinite(value) && value > 0;
}
