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
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Observable, Subscription } from 'rxjs';
import { translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { AccountService, TransactionBody } from '../shared/account';
import { AllocationInput, FundAmount } from '../shared/allocation-models';
import { AllocationSplit, SplitPrefill } from '../shared/allocation-split/allocation-split';
import { calendarDate, toIso, today } from '../shared/date';
import { httpErrorMessage } from '../shared/http-error';
import { LedgerService } from '../shared/ledger';
import { LedgerStore } from '../shared/ledger-store';
import { Modal } from '../shared/modal';
import { AccountTransactionDTO, Money } from '../shared/models';
import { currencyOptions, hasAtMostTwoDecimals } from '../shared/money';
import { PreferencesService } from '../shared/preferences';
import { TransactionKind } from '../shared/ui-actions';

type Kind = TransactionKind;

export interface CopySource {
  accountId: string;
  value: Money;
  title?: string | null;
  fundId?: string | null;
  allocations?: readonly FundAmount[] | null;
}

const isPositiveAmount = (amount: number | null): amount is number =>
  typeof amount === 'number' && Number.isFinite(amount) && amount > 0;

function isInitialBalance(tx: AccountTransactionDTO): boolean {
  const flag = (tx as { isInitialBalance?: boolean }).isInitialBalance;
  return flag ?? new Date(tx.date).getUTCFullYear() < 1000;
}

@Component({
  selector: 'app-new-transaction',
  imports: [AllocationSplit, FormsModule, Modal, TranslatePipe],
  templateUrl: './new-transaction.html',
  styleUrl: './new-transaction.scss',
})
export class NewTransaction {
  private readonly accountsApi = inject(AccountService);
  private readonly ledgers = inject(LedgerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly prefs = inject(PreferencesService);
  protected readonly store = inject(LedgerStore);

  readonly open = input(false);
  readonly initialKind = input<Kind>('expense');
  readonly transaction = input<AccountTransactionDTO | null>(null);
  readonly copyOf = input<CopySource | null>(null);
  readonly startAllocating = input(false);
  readonly closed = output<void>();
  readonly saved = output<void>();

  private readonly split = viewChild(AllocationSplit);
  private detailRequest?: Subscription;

  protected readonly kind = signal<Kind>('expense');
  protected readonly submitting = signal(false);
  protected readonly error = signal('');

  protected readonly amount = signal<number | null>(null);
  protected readonly currency = signal('EUR');

  protected readonly initialBalance = signal(false);
  private readonly hadAllocations = signal(false);
  protected readonly allocLoading = signal(false);
  protected readonly allocError = signal('');
  protected readonly prefill = signal<SplitPrefill | null>(null);

  protected accountId = '';
  protected date = today();
  protected title = '';
  protected fundId: string | null = null;

  protected readonly currencyOptions = computed(() =>
    currencyOptions(this.prefs.favoriteCurrencies(), [this.currency()]),
  );

  protected get isEdit(): boolean {
    return this.transaction() !== null;
  }

  constructor() {
    effect(() => {
      if (this.open()) {
        const tx = this.transaction();
        const copy = this.copyOf();
        untracked(() => this.reset(tx, tx ? null : copy));
      }
    });
    effect(() => {
      const accounts = this.store.accounts();
      untracked(() => {
        if (this.open() && !this.accountId) this.accountId = accounts[0]?.id ?? '';
      });
    });
  }

  protected setCurrency(currency: string): void {
    this.currency.set(currency);
  }

  protected submit(): void {
    if (this.submitting()) return;
    const amount = this.amount();
    if (!this.accountId) return this.error.set(translate('error.chooseAccount'));
    if (!isPositiveAmount(amount)) return this.error.set(translate('error.amountPositive'));
    if (!hasAtMostTwoDecimals(amount)) return this.error.set(translate('error.twoDecimals'));
    if (!this.date) return this.error.set(translate('error.chooseDate'));
    if (this.allocLoading()) return this.error.set(translate('txd.allocationsLoading'));

    const allocation = this.buildAllocation();
    if (allocation.error) return this.error.set(allocation.error);

    const body: TransactionBody = {
      value: {
        amount: amount * (this.kind() === 'expense' ? -1 : 1),
        currency: this.currency(),
      },
      date: toIso(this.date),
      title: this.title.trim() || null,
      ...(allocation.value ? { allocation: allocation.value } : {}),
      ...(this.kind() === 'expense' && !this.initialBalance() && this.fundId
        ? { fundId: this.fundId }
        : {}),
    };
    const tx = this.transaction();

    this.error.set('');
    this.submitting.set(true);
    const request: Observable<unknown> = tx
      ? this.accountsApi.updateTransaction(tx.accountId, tx.id, body)
      : this.accountsApi.createTransaction(this.accountId, body);
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
            invalid: translate('error.invalidDetails'),
            gone: translate('error.accountGone'),
            fallback: translate('txd.fallback'),
          }),
        );
      },
    });
  }

  private buildAllocation(): { value?: AllocationInput; error?: string } {
    const clear = this.isEdit && this.hadAllocations() ? { lines: [] } : undefined;
    if (this.kind() === 'expense') return { value: clear };
    const split = this.split();
    if (this.initialBalance() || !split || (this.isEdit && !split.touched())) return {};
    const result = split.build();
    if (result.error) return { error: result.error };
    return { value: result.allocation ?? clear };
  }

  private reset(tx: AccountTransactionDTO | null, copy: CopySource | null): void {
    const source = tx ?? copy;
    this.error.set('');
    this.submitting.set(false);
    this.fundId = source?.fundId ?? null;
    this.kind.set(source ? (source.value.amount < 0 ? 'expense' : 'income') : this.initialKind());
    this.amount.set(source ? Math.abs(source.value.amount) : null);
    this.currency.set(source?.value.currency ?? this.prefs.defaultCurrency());
    this.date = tx ? calendarDate(tx.date) : today();
    this.title = source?.title ?? '';
    this.accountId = source?.accountId ?? this.store.accounts()[0]?.id ?? '';

    this.initialBalance.set(!!tx && isInitialBalance(tx));
    this.hadAllocations.set(false);
    this.allocLoading.set(false);
    this.allocError.set('');
    const copied = copy && copy.value.amount > 0 ? (copy.allocations ?? []) : [];
    if (tx && this.startAllocating()) this.prefill.set({ mode: 'auto', touched: true });
    else if (copied.length > 0) this.prefill.set({ mode: 'custom', lines: copied });
    else this.prefill.set(null);
    this.detailRequest?.unsubscribe();
    if (tx && tx.value.amount > 0 && !this.initialBalance()) {
      if (tx.allocations) this.applyAllocations(tx.allocations);
      else this.loadAllocations(tx);
    }
  }

  private applyAllocations(allocations: readonly FundAmount[]): void {
    this.hadAllocations.set(allocations.length > 0);
    if (this.split()?.touched() && !this.startAllocating()) return;
    if (this.startAllocating()) {
      this.prefill.set({ mode: 'auto', lines: allocations, touched: true });
    } else if (allocations.length > 0) {
      this.prefill.set({ mode: 'custom', lines: allocations });
    }
  }

  private loadAllocations(tx: AccountTransactionDTO): void {
    this.allocLoading.set(true);
    this.detailRequest = this.accountsApi
      .getTransaction(tx.accountId, tx.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (detail) => {
          this.allocLoading.set(false);
          this.applyAllocations(detail.allocations ?? []);
        },
        error: () => {
          this.allocLoading.set(false);
          this.allocError.set(translate('txd.loadAllocationsError'));
        },
      });
  }
}
