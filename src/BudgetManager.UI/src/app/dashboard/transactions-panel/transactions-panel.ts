import {
  Component,
  DestroyRef,
  ElementRef,
  afterNextRender,
  afterRenderEffect,
  computed,
  effect,
  inject,
  model,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { EMPTY, Observable, catchError, debounceTime, map, of, switchMap } from 'rxjs';
import { TranslationKey, translate } from '../../i18n/i18n';
import { TranslatePipe } from '../../i18n/t.pipe';
import { FeedFilters, FeedItemDTO, LedgerFeedPageDTO } from '../../shared/feed-models';
import { LedgerService } from '../../shared/ledger';
import { MenuButton, MenuEntry } from '../../shared/menu-button';
import { UiActions } from '../../shared/ui-actions';
import { UiState } from '../../shared/ui-state';
import {
  COMPACT_WIDTH,
  DEFAULT_PERIOD,
  FEED_PAGE_SIZE,
  FeedRow,
  PERIOD_OPTIONS,
  Period,
  RowAction,
  RowMetrics,
  Scope,
  TYPE_OPTIONS,
  TypeFilter,
  displayRows,
  feedFilters,
  isValidRange,
  metricsFor,
  pagesIn,
  periodRange,
  rowOffsets,
  scopeLabel,
  toAllocationCopy,
  toAllocationEdit,
  toTransaction,
  toTransactionCopy,
  toTransferCopy,
  toTransferRef,
  windowBlocks,
  windowRange,
} from '../dashboard-data';

export type LoadStatus = 'loading' | 'ready' | 'error';

const OVERSCAN_PX = 360;
const PREFETCH_PX = 900;
const SEARCH_DEBOUNCE_MS = 300;

const actionLabels: Record<RowAction, TranslationKey> = {
  edit: 'tx.action.edit',
  copy: 'tx.action.copy',
  allocate: 'tx.action.allocate',
  delete: 'tx.action.delete',
};

interface FeedQuery {
  key: string;
  ledgerId: string;
  filters: FeedFilters;
}

interface PageEntry {
  items: FeedItemDTO[];
  generation: number;
}

interface PageRequest {
  query: FeedQuery;
  page: number;
  generation: number;
}

interface PageResult {
  request: PageRequest;
  page: LedgerFeedPageDTO | null;
}

const asString = (raw: unknown): string | undefined => (typeof raw === 'string' ? raw : undefined);

const sameRequest = (a: PageRequest | null, b: PageRequest | null): boolean =>
  a === b ||
  (!!a && !!b && a.query.key === b.query.key && a.page === b.page && a.generation === b.generation);

@Component({
  selector: 'app-transactions-panel',
  imports: [FormsModule, MenuButton, TranslatePipe],
  templateUrl: './transactions-panel.html',
  styleUrl: './transactions-panel.scss',
})
export class TransactionsPanel {
  private readonly ui = inject(UiActions);
  private readonly ledgers = inject(LedgerService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly destroyRef = inject(DestroyRef);

  readonly scope = model<Scope | null>(null);

  protected readonly typeOptions = TYPE_OPTIONS;
  protected readonly periodOptions = PERIOD_OPTIONS;
  protected readonly scopeLabel = scopeLabel;

  private readonly uiState = inject(UiState);
  protected readonly period = this.uiState.persisted<Period>(
    'tx.period',
    DEFAULT_PERIOD,
    (raw) => PERIOD_OPTIONS.find((o) => o.value === raw)?.value,
  );
  protected readonly customFrom = this.uiState.persisted('tx.from', '', asString);
  protected readonly customTo = this.uiState.persisted('tx.to', '', asString);
  protected readonly type = this.uiState.persisted<TypeFilter>(
    'tx.type',
    'all',
    (raw) => TYPE_OPTIONS.find((o) => o.value === raw)?.value,
  );
  protected readonly searchText = this.uiState.persisted('tx.search', '', asString);
  private readonly search = signal(this.searchText().trim());
  protected readonly showDays = this.uiState.persisted('tx.showDays', true, (raw) =>
    typeof raw === 'boolean' ? raw : undefined,
  );

  private readonly range = computed(() =>
    periodRange(this.period(), new Date(), { from: this.customFrom(), to: this.customTo() }),
  );
  protected readonly rangeError = computed(() =>
    isValidRange(this.range()) ? '' : translate('tx.rangeError'),
  );

  private readonly query = computed<FeedQuery | null>(
    () => {
      const ledgerId = this.ledgers.currentLedgerId();
      const range = this.range();
      if (!ledgerId || !isValidRange(range)) return null;
      const filters = feedFilters({
        range,
        type: this.type(),
        search: this.search(),
        scope: this.scope(),
      });
      return { key: JSON.stringify([ledgerId, filters]), ledgerId, filters };
    },
    { equal: (a, b) => a?.key === b?.key },
  );

  private readonly pages = signal<ReadonlyMap<number, PageEntry>>(new Map());
  protected readonly total = signal<number | null>(null);
  private readonly generation = signal(0);
  protected readonly status = signal<LoadStatus>('loading');
  protected readonly pageError = signal(false);
  private readonly expandedSet = computed(() =>
    this.uiState.persistedSet(`tx.expanded.${this.ledgers.currentLedgerId() ?? ''}`),
  );
  private readonly expanded = computed(() => this.expandedSet()());

  private readonly scroller = viewChild<ElementRef<HTMLElement>>('scroller');
  private readonly scrollTop = signal(0);
  private readonly viewport = signal(600);
  private loadedKey: string | null | undefined;

  private readonly width = signal(0);
  protected readonly actionLabels = actionLabels;
  protected readonly compact = computed(() => this.width() > 0 && this.width() < COMPACT_WIDTH);
  protected readonly metrics = computed(() => metricsFor(this.width()));
  private firstVisible = 0;
  private appliedMetrics: RowMetrics | null = null;

  private readonly rows = computed(() => {
    const items = new Map([...this.pages()].map(([page, entry]) => [page, entry.items]));
    return displayRows(
      items,
      this.total() ?? 0,
      FEED_PAGE_SIZE,
      this.expanded(),
      this.metrics(),
      this.showDays(),
    );
  });
  private readonly offsets = computed(() => rowOffsets(this.rows()));

  protected readonly view = computed(() => {
    const rows = this.rows();
    const offsets = this.offsets();
    const top = this.scrollTop();
    const { start, end } = windowRange(
      offsets,
      top - OVERSCAN_PX,
      top + this.viewport() + OVERSCAN_PX,
    );
    return {
      before: offsets[start],
      after: offsets[rows.length] - offsets[end],
      blocks: windowBlocks(rows, start, end),
    };
  });

  private readonly wanted = computed<PageRequest | null>(
    () => {
      const query = this.query();
      if (!query) return null;
      const generation = this.generation();
      if (this.total() === null) return { query, page: 0, generation };
      const rows = this.rows();
      const top = this.scrollTop();
      const band = windowRange(
        this.offsets(),
        top - PREFETCH_PX,
        top + this.viewport() + PREFETCH_PX,
      );
      const pages = this.pages();
      const page = pagesIn(rows, band.start, band.end).find(
        (p) => (pages.get(p)?.generation ?? -1) < generation,
      );
      return page === undefined ? null : { query, page, generation };
    },
    { equal: sameRequest },
  );

  private readonly menus = new WeakMap<FeedRow, MenuEntry[]>();

  protected readonly pristine = computed(
    () =>
      this.type() === 'all' &&
      !this.searchText() &&
      !this.scope() &&
      this.period() === DEFAULT_PERIOD,
  );

  constructor() {
    toObservable(this.searchText)
      .pipe(debounceTime(SEARCH_DEBOUNCE_MS), takeUntilDestroyed())
      .subscribe((text) => this.search.set(text.trim()));

    effect(() => {
      const key = this.query()?.key ?? null;
      this.ledgers.reloadTick();
      untracked(() => {
        if (key === this.loadedKey) {
          this.generation.update((n) => n + 1);
          return;
        }
        this.loadedKey = key;
        this.reset();
      });
    });

    toObservable(this.wanted)
      .pipe(
        switchMap((request) => (request ? this.fetch(request) : EMPTY)),
        takeUntilDestroyed(),
      )
      .subscribe((result) => this.receive(result));

    afterRenderEffect((onCleanup) => {
      const el = this.scroller()?.nativeElement;
      if (!el) return;
      const observer = new ResizeObserver(() => this.viewport.set(el.clientHeight));
      observer.observe(el);
      onCleanup(() => observer.disconnect());
    });

    afterNextRender(() => {
      const el = this.host.nativeElement;
      this.width.set(el.clientWidth);
      const observer = new ResizeObserver(() => this.width.set(el.clientWidth));
      observer.observe(el);
      this.destroyRef.onDestroy(() => observer.disconnect());
    });

    effect(() => {
      const metrics = this.metrics();
      const offsets = this.offsets();
      untracked(() => {
        const changed = this.appliedMetrics !== null && this.appliedMetrics !== metrics;
        this.appliedMetrics = metrics;
        if (!changed) return;
        const top = offsets[Math.min(this.firstVisible, offsets.length - 1)] ?? 0;
        this.scrollTop.set(top);
        const el = this.scroller()?.nativeElement;
        if (el) el.scrollTop = top;
      });
    });
  }

  protected onScroll(event: Event): void {
    const top = (event.target as HTMLElement).scrollTop;
    this.scrollTop.set(top);
    this.firstVisible = windowRange(this.offsets(), top, top + 1).start;
  }

  protected retry(): void {
    this.pageError.set(false);
    if (this.total() === null) this.status.set('loading');
    this.generation.update((n) => n + 1);
  }

  protected clear(): void {
    this.type.set('all');
    this.searchText.set('');
    this.search.set('');
    this.scope.set(null);
    this.period.set(DEFAULT_PERIOD);
    this.customFrom.set('');
    this.customTo.set('');
  }

  protected showAllTime(): void {
    this.period.set('all');
  }

  protected newTransaction(): void {
    this.ui.open({ kind: 'transaction', initialKind: 'expense' });
  }

  protected toggle(row: FeedRow): void {
    this.uiState.toggle(this.expandedSet(), row.key);
  }

  protected menuOf(row: FeedRow): MenuEntry[] {
    let menu = this.menus.get(row);
    if (!menu) {
      menu = row.actions.map(
        (id): MenuEntry => ({ type: 'item', id, label: translate(actionLabels[id]) }),
      );
      this.menus.set(row, menu);
    }
    return menu;
  }

  protected onAction(row: FeedRow, action: string): void {
    switch (action as RowAction) {
      case 'edit':
        return this.edit(row);
      case 'copy':
        return this.copy(row);
      case 'allocate':
        return this.ui.open({
          kind: 'transaction',
          initialKind: 'income',
          transaction: toTransaction(row.item),
          startAllocating: true,
        });
      case 'delete':
        return this.ui.open({ kind: 'confirmDelete', item: row.item });
    }
  }

  protected edit(row: FeedRow): void {
    switch (row.kind) {
      case 'income':
      case 'expense':
        return this.ui.open({
          kind: 'transaction',
          initialKind: row.kind,
          transaction: toTransaction(row.item),
        });
      case 'transfer':
      case 'exchange':
        return this.ui.open({ kind: row.kind, transfer: toTransferRef(row.item) });
      case 'allocation':
        return this.ui.open({ kind: 'allocate', allocation: toAllocationEdit(row.item) });
    }
  }

  private copy(row: FeedRow): void {
    switch (row.kind) {
      case 'income':
      case 'expense':
        return this.ui.open({
          kind: 'transaction',
          initialKind: row.kind,
          copy: toTransactionCopy(row.item),
        });
      case 'transfer':
      case 'exchange':
        return this.ui.open({ kind: row.kind, copy: toTransferCopy(row.item) });
      case 'allocation':
        return this.ui.open({ kind: 'allocate', copy: toAllocationCopy(row.item) });
    }
  }

  private reset(): void {
    this.pages.set(new Map());
    this.total.set(null);
    this.status.set('loading');
    this.pageError.set(false);
    this.scrollTop.set(0);
    this.firstVisible = 0;
    const el = this.scroller()?.nativeElement;
    if (el) el.scrollTop = 0;
  }

  private fetch(request: PageRequest): Observable<PageResult> {
    const { query, page } = request;
    return this.ledgers
      .getFeed(query.ledgerId, query.filters, page * FEED_PAGE_SIZE, FEED_PAGE_SIZE)
      .pipe(
        map((result): PageResult => ({ request, page: result })),
        catchError(() => of<PageResult>({ request, page: null })),
      );
  }

  private receive({ request, page }: PageResult): void {
    if (request.query.key !== this.query()?.key) return;
    if (!page) {
      if (this.total() === null) this.status.set('error');
      else this.pageError.set(true);
      return;
    }
    const pageCount = Math.ceil(page.total / FEED_PAGE_SIZE);
    this.pages.update((pages) => {
      const next = new Map([...pages].filter(([p]) => p < pageCount));
      next.set(request.page, { items: page.items, generation: request.generation });
      return next;
    });
    this.total.set(page.total);
    this.status.set('ready');
  }
}
