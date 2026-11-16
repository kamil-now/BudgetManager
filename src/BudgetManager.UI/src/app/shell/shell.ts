import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import {
  ActivatedRoute,
  NavigationEnd,
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { AddAccount } from '../add-account/add-account';
import { AllocateFunds } from '../allocate-funds/allocate-funds';
import { AllocationEditor } from '../allocation-editor/allocation-editor';
import { AuthService } from '../auth/auth';
import { SessionService } from '../auth/session';
import { ConfirmDelete } from '../dashboard/confirm-delete/confirm-delete';
import { Exchange } from '../exchange/exchange';
import { TranslationKey, translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { LedgerAllocationEditor } from '../ledger-allocation-editor/ledger-allocation-editor';
import { NewBudget } from '../new-budget/new-budget';
import { NewTransaction } from '../new-transaction/new-transaction';
import { LedgerService } from '../shared/ledger';
import { LedgerStore } from '../shared/ledger-store';
import { MenuButton, MenuEntry } from '../shared/menu-button';
import { balanceLines, sumBalances } from '../shared/money';
import { PreferencesService } from '../shared/preferences';
import { ActiveDialog, UiActions } from '../shared/ui-actions';
import { Viewport } from '../shared/viewport';
import { Transfer } from '../transfer/transfer';
import { BalanceCard } from './balance-card/balance-card';

const newMenuDialogs: Record<string, ActiveDialog> = {
  income: { kind: 'transaction', initialKind: 'income' },
  expense: { kind: 'transaction', initialKind: 'expense' },
  transfer: { kind: 'transfer' },
  exchange: { kind: 'exchange' },
  allocate: { kind: 'allocate' },
  account: { kind: 'account' },
  budget: { kind: 'budget' },
};

@Component({
  selector: 'app-shell',
  imports: [
    AddAccount,
    AllocateFunds,
    AllocationEditor,
    BalanceCard,
    ConfirmDelete,
    Exchange,
    LedgerAllocationEditor,
    MenuButton,
    NewBudget,
    NewTransaction,
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    TranslatePipe,
    Transfer,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly prefs = inject(PreferencesService);
  private readonly session = inject(SessionService);
  protected readonly ledger = inject(LedgerService);
  protected readonly store = inject(LedgerStore);
  protected readonly ui = inject(UiActions);
  protected readonly viewport = inject(Viewport);

  protected readonly user = this.auth.user;
  protected readonly initials = computed(() => initialsOf(this.user?.name ?? ''));
  protected readonly theme = this.prefs.theme;
  protected readonly themeLabel = computed(() =>
    translate('theme.current', { theme: translate(themeNames[this.theme()]) }),
  );
  protected readonly menuOpen = signal(false);

  protected readonly ledgers = computed(() => {
    const current = this.ledger.currentLedgerId();
    return (this.ledger.ledgerList() ?? []).map((l) => ({
      id: l.id,
      name: l.name,
      current: l.id === current,
      lines: balanceLines(l.balance),
    }));
  });

  protected readonly allLedgers = computed(() => {
    const list = this.ledger.ledgerList() ?? [];
    if (list.length < 2) return null;
    return { lines: balanceLines(sumBalances(list.map((l) => l.balance))) };
  });

  protected readonly newItems = computed<MenuEntry[]>(() => {
    const accounts = this.store.accounts().length;
    const noAccount = accounts === 0 ? translate('new.addAccountFirst') : null;
    const allocatable = this.store.totals().some((t) => t.unallocated > 0);
    return [
      { type: 'item', id: 'income', label: translate('common.income'), disabledReason: noAccount },
      { type: 'item', id: 'expense', label: translate('common.expense'), disabledReason: noAccount },
      {
        type: 'item',
        id: 'transfer',
        label: translate('new.transfer'),
        disabledReason: accounts < 2 ? translate('new.needsTwoAccounts') : null,
      },
      { type: 'item', id: 'exchange', label: translate('new.exchange'), disabledReason: noAccount },
      { type: 'separator' },
      {
        type: 'item',
        id: 'allocate',
        label: translate('new.allocate'),
        disabledReason: allocatable ? null : translate('new.nothingUnallocated'),
      },
      { type: 'separator' },
      { type: 'item', id: 'account', label: translate('common.account') },
      { type: 'item', id: 'budget', label: translate('common.budget') },
    ];
  });

  protected readonly title = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      startWith(null),
      map(() => {
        let route = this.route;
        while (route.firstChild) route = route.firstChild;
        /*
         * `snapshot` can be momentarily undefined for a child route that is still activating when the `startWith(null)` seed runs during shell construction; guard so change detection (and the router-outlet that follows in the template) is not aborted.
         * The title fills in on NavigationEnd.
         */
        return (route.snapshot?.data?.['title'] as TranslationKey | undefined) ?? null;
      }),
    ),
    { initialValue: null },
  );

  constructor() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe(() => this.menuOpen.set(false));

    effect(() => {
      this.ledger.reloadTick();
      untracked(() => this.ledger.refreshLedgers());
    });
  }

  protected onNew(id: string): void {
    const dialog = newMenuDialogs[id];
    if (dialog) this.ui.open(dialog);
  }

  protected pickLedger(id: string): void {
    this.menuOpen.set(false);
    if (id !== this.ledger.currentLedgerId()) {
      const name = this.ledger.ledgerList()?.find((l) => l.id === id)?.name ?? null;
      this.ledger.setCurrentLedgerId(id);
      this.ledger.currentLedgerName.set(name);
    }
    this.router.navigateByUrl('/dashboard');
  }

  protected toggleTheme(): void {
    this.prefs.cycleTheme();
  }

  protected signOut(): void {
    this.ui.close();
    this.session.signOut();
  }
}

const themeNames = {
  system: 'theme.system',
  light: 'theme.light',
  dark: 'theme.dark',
} as const satisfies Record<string, TranslationKey>;

function initialsOf(name: string): string {
  const parts = name.trim().split(/\s+/)
    .filter(Boolean);
  if (parts.length === 0) return '?';
  const letters =
    parts.length === 1 ? parts[0].slice(0, 2) : parts[0][0] + parts[parts.length - 1][0];
  return letters.toUpperCase();
}
