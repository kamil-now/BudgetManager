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
import { Observable } from 'rxjs';
import { translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { NewTransaction } from '../new-transaction/new-transaction';
import { AllocationApi } from '../shared/allocation-api';
import {
  AllocationSplit,
  SplitPrefill,
  SplitResult,
} from '../shared/allocation-split/allocation-split';
import { calendarDate, toIso, today } from '../shared/date';
import { httpErrorMessage } from '../shared/http-error';
import { LedgerService } from '../shared/ledger';
import { LedgerStore } from '../shared/ledger-store';
import { Modal } from '../shared/modal';
import { formatMoney, hasAtMostTwoDecimals } from '../shared/money';
import { PreferencesService } from '../shared/preferences';
import { DialogOf } from '../shared/ui-actions';

const cents = (amount: number): number => Math.round(amount * 100);

const autoPrefill: SplitPrefill = { mode: 'auto' };

@Component({
  selector: 'app-allocate-funds',
  imports: [AllocationSplit, FormsModule, Modal, NewTransaction, TranslatePipe],
  templateUrl: './allocate-funds.html',
  styleUrl: './allocate-funds.scss',
})
export class AllocateFunds {
  private readonly api = inject(AllocationApi);
  private readonly ledgers = inject(LedgerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly prefs = inject(PreferencesService);
  protected readonly store = inject(LedgerStore);

  readonly dialog = input<DialogOf<'allocate'> | null>(null);
  readonly closed = output<void>();
  readonly saved = output<void>();

  private readonly split = viewChild(AllocationSplit);

  protected readonly formatMoney = formatMoney;

  protected readonly currency = signal('EUR');
  protected readonly amount = signal<number | null>(null);
  protected readonly submitting = signal(false);
  protected readonly error = signal('');
  protected readonly prefill = signal<SplitPrefill>(autoPrefill);
  protected date = today();
  protected title = '';

  protected readonly income = computed(() => this.dialog()?.income ?? null);
  protected readonly editing = computed(() => this.dialog()?.allocation ?? null);

  protected readonly unallocated = computed(() => this.availableIn(this.currency()));

  protected readonly currencies = computed(() => {
    const open = this.store.totals()
      .filter((t) => t.unallocated > 0)
      .map((t) => t.currency);
    const all = open.includes(this.currency()) ? open : [this.currency(), ...open];
    const favorites = this.prefs.favoriteCurrencies().filter((c) => all.includes(c));
    return [...favorites, ...all.filter((c) => !favorites.includes(c))];
  });

  constructor() {
    effect(() => {
      const dialog = this.dialog();
      untracked(() => {
        if (dialog && !dialog.income) this.reset(dialog);
      });
    });
  }

  protected setCurrency(currency: string): void {
    this.currency.set(currency);
    this.amount.set(this.defaultAmount(currency));
  }

  protected submit(): void {
    if (this.submitting()) return;
    const id = this.ledgers.currentLedgerId();
    const currency = this.currency();
    const left = this.unallocated();
    const amount = this.amount();
    const edited = this.editing();
    if (!id) return this.error.set(translate('allocate.chooseLedger'));
    if (left <= 0) return this.error.set(translate('allocate.nothing', { currency }));
    if (typeof amount !== 'number' || !Number.isFinite(amount) || amount <= 0) {
      return this.error.set(translate('error.amountPositive'));
    }
    if (!hasAtMostTwoDecimals(amount)) return this.error.set(translate('error.twoDecimals'));
    if (cents(amount) > cents(left)) {
      return this.error.set(translate('allocate.atMost', { amount: formatMoney(left, currency) }));
    }
    if (!this.date) return this.error.set(translate('error.chooseDate'));
    const split: SplitResult = this.split()?.build() ?? {};
    if (split.error) return this.error.set(split.error);
    if (!split.allocation) return this.error.set(translate('allocate.chooseSplit'));

    this.error.set('');
    this.submitting.set(true);
    const title = this.title.trim() || null;
    const { allocation } = split;
    const date = edited?.initial ? edited.date : toIso(this.date);
    let request: Observable<unknown>;
    if (edited) {
      request = this.api.updateAllocation(id, edited.id, { currency, amount, allocation, date, title });
    } else {
      request = this.api.allocate(id, { currency, allocation, amount, date, title });
    }
    request
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          this.ledgers.requestReload();
          this.saved.emit();
        },
        error: (response: HttpErrorResponse) => {
          this.submitting.set(false);
          this.error.set(
            httpErrorMessage(response, {
              invalid: translate('allocate.invalid'),
              gone: translate('error.fundGone'),
              fallback: translate('allocate.fallback'),
            }),
          );
        },
      });
  }

  private reset(dialog: DialogOf<'allocate'>): void {
    const source = dialog.allocation ?? dialog.copy;
    const first = this.store.totals().find((t) => t.unallocated > 0)?.currency;
    const chosen = source?.currency ?? dialog.currency ?? first ?? this.prefs.defaultCurrency();
    this.currency.set(chosen);
    this.amount.set(source ? source.amount : this.defaultAmount(chosen));
    this.date = dialog.allocation ? calendarDate(dialog.allocation.date) : today();
    this.title = source?.title ?? '';
    this.prefill.set(source ? { mode: 'custom', lines: source.lines } : autoPrefill);
    this.error.set('');
    this.submitting.set(false);
  }

  private defaultAmount(currency: string): number | null {
    const left = this.availableIn(currency);
    return left > 0 ? left : null;
  }

  private availableIn(currency: string): number {
    const unallocated = this.store.totals().find((t) => t.currency === currency)?.unallocated ?? 0;
    const edited = this.editing();
    const own = edited && edited.currency === currency ? edited.amount : 0;
    return (cents(unallocated) + cents(own)) / 100;
  }
}
