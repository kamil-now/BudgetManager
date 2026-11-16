import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { TranslationKey, translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { dayEndIso, formatDate, toIso } from '../shared/date';
import { LedgerService } from '../shared/ledger';
import { LedgerStatisticsDTO, StatisticsFilters } from '../shared/models';
import { amountClass } from '../shared/money';
import { PageStatus } from '../shared/page-status';
import { reloadOnTick } from '../shared/reload';
import { UiState } from '../shared/ui-state';
import {
  Chart,
  FUND_LEGEND,
  chart,
  currencyName,
  fundChartRows,
  fundTable,
  kpi,
  namedRows,
  side,
  tableRows,
} from './statistics-data';

@Component({
  selector: 'app-statistics',
  imports: [FormsModule, PageStatus, TranslatePipe],
  templateUrl: './statistics.html',
  styleUrl: './statistics.scss',
})
export class Statistics implements OnInit, OnDestroy {
  private readonly ledgers = inject(LedgerService);

  protected readonly state = signal<'loading' | 'ready' | 'error' | 'no-ledger'>('loading');
  protected readonly data = signal<LedgerStatisticsDTO | null>(null);
  protected readonly refreshing = signal(false);
  protected readonly refreshFailed = signal(false);
  private pending?: Subscription;

  protected readonly amountClass = amountClass;
  protected readonly statLabels: readonly TranslationKey[] = [
    'stats.total',
    'stats.average',
    'stats.min',
    'stats.max',
  ];
  protected readonly fundLabels: readonly TranslationKey[] = [
    'stats.allocated',
    'stats.spent',
    'stats.netChange',
    'stats.balance',
  ];
  private readonly uiState = inject(UiState);
  private readonly saved = this.uiState.read<{ from?: unknown; to?: unknown; initial?: unknown }>(
    'stats.filters',
  );
  protected from = typeof this.saved?.from === 'string' ? this.saved.from : '';
  protected to = typeof this.saved?.to === 'string' ? this.saved.to : '';
  protected readonly includeInitial = signal(this.saved?.initial === true);
  protected readonly shownInitial = signal(false);
  protected readonly shownTo = signal('');

  protected readonly income = computed(() => this.data()?.totalIncome ?? {});
  protected readonly expense = computed(() => this.data()?.totalExpense ?? {});
  protected readonly balanceAsOf = computed(() => {
    const to = this.shownTo();
    return to ? translate('stats.balanceAsOf', { date: formatDate(toIso(to)) }) : translate('stats.balanceCurrent');
  });

  private readonly currencies = computed(() =>
    [...new Set([...Object.keys(this.income()), ...Object.keys(this.expense())])].sort(),
  );

  protected readonly kpis = computed(() => {
    const currencies = this.currencies();
    const income = this.income();
    const expense = this.expense();
    const fundTotals = this.data()?.fundTotals ?? {};
    const net = (c: string) => (income[c]?.total ?? 0) - Math.abs(expense[c]?.total ?? 0);
    return {
      income: kpi(currencies, (c) => income[c]?.total ?? 0),
      expense: kpi(currencies, (c) => -Math.abs(expense[c]?.total ?? 0)),
      net: kpi(currencies, net),
      allocated: kpi(Object.keys(fundTotals).sort(), (c) => fundTotals[c]?.allocated ?? 0),
    };
  });

  protected readonly charts = computed<Chart[]>(() => {
    const data = this.data();
    if (!data) return [];
    const byCurrency = this.currencies().map((c) => ({
      label: c,
      sub: currencyName(c),
      income: side(this.income()[c]?.total, c, 1),
      expense: side(this.expense()[c]?.total, c, -1),
    }));
    const nominal = translate('stats.nominal');
    return [
      chart(translate('stats.byCurrency'), translate('stats.byCurrencyAxis'), false, byCurrency),
      chart(translate('stats.byAccount'), nominal, true, namedRows(data.accounts)),
      chart(
        translate('stats.byFund'),
        nominal,
        true,
        fundChartRows(data.budgets),
        FUND_LEGEND,
        'stats.describeFund',
      ),
    ];
  });

  protected readonly fundTable = computed(() => {
    const data = this.data();
    return data ? fundTable(data.budgets, data.fundTotals) : { groups: [], all: null };
  });

  protected readonly tables = computed(() => [
    {
      title: 'common.income' as TranslationKey,
      empty: 'stats.noIncome' as TranslationKey,
      rows: tableRows(this.income(), 1),
    },
    {
      title: 'common.expense' as TranslationKey,
      empty: 'stats.noExpense' as TranslationKey,
      rows: tableRows(this.expense(), -1),
    },
  ]);

  constructor() {
    reloadOnTick(this.ledgers, () => {
      if (this.state() === 'ready') this.apply();
    });
  }

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.pending?.unsubscribe();
  }

  protected load(): void {
    this.state.set('loading');
    this.query(true);
  }

  protected apply(): void {
    this.refreshing.set(true);
    this.refreshFailed.set(false);
    this.query(false);
  }

  private query(initial: boolean): void {
    const id = this.ledgers.currentLedgerId();
    if (!id) {
      this.state.set('no-ledger');
      return;
    }
    this.pending?.unsubscribe();
    const includeInitialBalance = this.includeInitial();
    const from = this.from;
    const to = this.to;
    this.uiState.write('stats.filters', { from, to, initial: includeInitialBalance });
    this.pending = this.ledgers.getStatistics(id, this.filters(includeInitialBalance)).subscribe({
      next: (stats) => {
        this.data.set(stats);
        this.shownInitial.set(includeInitialBalance && !from);
        this.shownTo.set(to);
        this.refreshing.set(false);
        this.state.set('ready');
      },
      error: (error: HttpErrorResponse) => {
        this.refreshing.set(false);
        if (error.status === 404 || error.status === 403) {
          this.ledgers.clearCurrentLedger();
          this.state.set('no-ledger');
        } else if (initial) {
          this.state.set('error');
        } else {
          this.refreshFailed.set(true);
        }
      },
    });
  }

  private filters(includeInitialBalance: boolean): StatisticsFilters {
    return {
      includeInitialBalance: includeInitialBalance || undefined,
      from: this.from ? toIso(this.from) : undefined,
      to: this.to ? dayEndIso(this.to) : undefined,
    };
  }
}
