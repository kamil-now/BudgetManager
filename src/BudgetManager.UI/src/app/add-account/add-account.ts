import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  ElementRef,
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
import { translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { AccountService } from '../shared/account';
import { httpErrorMessage } from '../shared/http-error';
import { LedgerService } from '../shared/ledger';
import { Modal } from '../shared/modal';
import { AccountDTO } from '../shared/models';
import { hasAtMostTwoDecimals } from '../shared/money';
import { PreferencesService } from '../shared/preferences';

@Component({
  selector: 'app-add-account',
  imports: [FormsModule, Modal, TranslatePipe],
  templateUrl: './add-account.html',
  styleUrl: './add-account.scss',
})
export class AddAccount {
  private readonly accountsApi = inject(AccountService);
  private readonly ledgers = inject(LedgerService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly prefs = inject(PreferencesService);

  readonly open = input(false);
  /** The account to rename; null adds a new one. */
  readonly account = input<AccountDTO | null>(null);
  readonly closed = output<void>();
  readonly saved = output<void>();

  private readonly amountInput = viewChild<ElementRef<HTMLInputElement>>('aaAmount');

  protected readonly currencyOptions = this.prefs.currencyOptions;
  protected readonly submitting = signal(false);
  protected readonly error = signal('');

  protected name = '';
  protected amount: number | null = null;
  protected currency = 'EUR';

  constructor() {
    effect(() => {
      if (this.open()) untracked(() => this.reset());
    });
  }

  protected submit(): void {
    if (this.submitting()) return;
    const editing = this.account();
    if (editing) return this.rename(editing);

    const ledgerId = this.ledgers.currentLedgerId();
    const name = this.name.trim();
    if (!ledgerId) return this.error.set(translate('error.noLedger'));
    if (!name) return this.error.set(translate('account.nameRequired'));
    const amountInput = this.amountInput()?.nativeElement;
    if (amountInput?.validity.badInput || (this.amount !== null && !Number.isFinite(this.amount))) {
      return this.error.set(translate('account.invalidBalance'));
    }
    if (!hasAtMostTwoDecimals(this.amount)) return this.error.set(translate('error.twoDecimals'));

    this.error.set('');
    this.submitting.set(true);
    this.accountsApi
      .createAccount({
        ledgerId,
        name,
        initialBalance: {
          amount: this.amount ?? 0,
          currency: this.currency,
        },
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.done(),
        error: (response: HttpErrorResponse) => {
          this.submitting.set(false);
          this.error.set(
            httpErrorMessage(response, {
              invalid: translate('error.invalidDetails'),
              gone: translate('error.ledgerGone'),
              fallback: translate('account.fallback'),
            }),
          );
        },
      });
  }

  private rename(account: AccountDTO): void {
    const name = this.name.trim();
    if (!name) return this.error.set(translate('account.nameRequired'));

    this.error.set('');
    this.submitting.set(true);
    this.accountsApi
      .updateAccount(account.id, name)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => this.done(),
        error: (response: HttpErrorResponse) => {
          this.submitting.set(false);
          this.error.set(
            httpErrorMessage(response, {
              invalid: translate('error.invalidDetails'),
              gone: translate('account.gone'),
              conflict: translate('account.conflict'),
              fallback: translate('account.saveFallback'),
            }),
          );
        },
      });
  }

  private done(): void {
    this.submitting.set(false);
    this.ledgers.requestReload();
    this.saved.emit();
  }

  private reset(): void {
    this.error.set('');
    this.submitting.set(false);
    this.currency = this.prefs.defaultCurrency();
    this.name = this.account()?.name ?? '';
    this.amount = null;
  }
}
