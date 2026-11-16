import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslationKey, translate } from '../../i18n/i18n';
import { TranslatePipe } from '../../i18n/t.pipe';
import {
  Observable,
  Subscription,
  catchError,
  debounceTime,
  forkJoin,
  map,
  of,
  switchMap,
} from 'rxjs';
import { minimumAmount, stepsProblem, toAllocationLines } from '../allocation';
import { AllocationApi } from '../allocation-api';
import { AllocationInput, FundAmount, SplitPreview } from '../allocation-models';
import {
  AllocationSteps,
  StepDraft,
  StepFund,
  newStep,
  stepProblemText,
  stepsFromLines,
} from '../allocation-steps/allocation-steps';
import { BudgetService } from '../budget';
import { httpErrorMessage } from '../http-error';
import { LedgerService } from '../ledger';
import { AllocationTemplateDTO, BudgetDTO, LedgerAllocationTemplateDTO } from '../models';
import { formatMoney, hasAtMostTwoDecimals } from '../money';

export type SplitMode = 'none' | 'template' | 'ledger' | 'custom';

export interface SplitPrefill {
  mode: SplitMode | 'auto';
  lines?: readonly FundAmount[];
  touched?: boolean;
}

export interface SplitResult {
  allocation?: AllocationInput;
  error?: string;
}

interface PreviewQuery {
  ledgerId: string;
  amount: number;
  currency: string;
  allocation: AllocationInput;
}

interface PreviewState {
  key: string;
  result: SplitPreview | null;
  error: string;
}

interface TemplateOption {
  template: AllocationTemplateDTO;
  label: string;
  usable: boolean;
}

interface TemplateGroup {
  budget: BudgetDTO;
  options: TemplateOption[];
}

interface LedgerTemplateOption {
  template: LedgerAllocationTemplateDTO;
  label: string;
  usable: boolean;
}

/** A budget of the chosen ledger template, with the templates its share can be split by. */
interface BudgetPick {
  budgetId: string;
  budgetName: string;
  options: TemplateOption[];
  templateId: string;
}

const PREVIEW_DEBOUNCE_MS = 300;

let splitSeq = 0;

const isPositive = (value: number | null): value is number =>
  typeof value === 'number' && Number.isFinite(value) && value > 0;

const keyOf = (query: PreviewQuery | null): string => (query ? JSON.stringify(query) : '');

@Component({
  selector: 'app-allocation-split',
  imports: [AllocationSteps, FormsModule, TranslatePipe],
  templateUrl: './allocation-split.html',
  styleUrl: './allocation-split.scss',
})
export class AllocationSplit {
  private readonly budgetsApi = inject(BudgetService);
  private readonly api = inject(AllocationApi);
  private readonly ledgers = inject(LedgerService);
  private readonly destroyRef = inject(DestroyRef);

  readonly budgets = input<readonly BudgetDTO[]>([]);
  readonly amount = input<number | null>(null);
  readonly currency = input('EUR');
  readonly allowNone = input(false);
  readonly heading = input('');
  readonly disabled = input(false);
  readonly prefill = input<SplitPrefill | null>(null);

  protected readonly headingId = `split-title-${++splitSeq}`;
  protected readonly formatMoney = formatMoney;

  private readonly _mode = signal<SplitMode>('none');
  readonly mode = this._mode.asReadonly();
  private readonly _touched = signal(false);
  readonly touched = this._touched.asReadonly();

  protected readonly templateId = signal('');
  protected readonly steps = signal<StepDraft[]>([]);
  protected readonly templates = signal<Record<string, AllocationTemplateDTO[]> | null>(null);
  protected readonly ledgerTemplates = signal<LedgerAllocationTemplateDTO[] | null>(null);
  protected readonly ledgerTemplateId = signal('');
  /** The picked template of each budget of the ledger template, by budget id. */
  private readonly budgetPicks = signal<Record<string, string>>({});

  private readonly preview = signal<PreviewState | null>(null);
  private pending: SplitPrefill | null = null;
  private templateRequest?: Subscription;
  private ledgerTemplateRequest?: Subscription;

  protected readonly modes = computed(() => {
    const all: { value: SplitMode; label: TranslationKey }[] = [
      { value: 'none', label: 'split.none' },
      { value: 'template', label: 'split.template' },
      { value: 'ledger', label: 'split.ledger' },
      { value: 'custom', label: 'split.custom' },
    ];
    return all.filter(
      (m) =>
        (m.value !== 'none' || this.allowNone()) &&
        (m.value !== 'ledger' || this.budgets().length > 1),
    );
  });

  protected readonly amountReady = computed(() => {
    const amount = this.amount();
    return isPositive(amount) && hasAtMostTwoDecimals(amount);
  });

  protected readonly groups = computed<TemplateGroup[]>(() => {
    const all = this.templates();
    if (!all) return [];
    const currency = this.currency();
    const amount = this.amountReady() ? Math.round((this.amount() as number) * 100) : null;
    return this.budgets()
      .map((budget) => ({
        budget,
        options: (all[budget.id] ?? [])
          .filter((t) => t.currency === currency && t.lines.length > 0)
          .map((template): TemplateOption => {
            const min = formatMoney(template.minimumAmount, currency);
            const usable = amount === null || Math.round(template.minimumAmount * 100) <= amount;
            const label = !usable
              ? translate('split.needsAtLeast', { name: template.name, amount: min })
              : template.minimumAmount > 0
                ? translate('split.templateOption', { name: template.name, amount: min })
                : template.name;
            return { template, label, usable };
          }),
      }))
      .filter((g) => g.options.length > 0);
  });

  protected readonly options = computed(() => this.groups().flatMap((g) => g.options));

  protected readonly ledgerOptions = computed<LedgerTemplateOption[]>(() => {
    const currency = this.currency();
    const amount = this.amountReady() ? Math.round((this.amount() as number) * 100) : null;
    return (this.ledgerTemplates() ?? [])
      .filter((t) => t.currency === currency && t.lines.length > 0)
      .map((template) => {
        const min = formatMoney(template.minimumAmount, currency);
        const usable = amount === null || Math.round(template.minimumAmount * 100) <= amount;
        const label = !usable
          ? translate('split.needsAtLeast', { name: template.name, amount: min })
          : template.minimumAmount > 0
            ? translate('split.templateOption', { name: template.name, amount: min })
            : template.name;
        return { template, label, usable };
      });
  });

  protected readonly selectedLedgerTemplate = computed(
    () => this.ledgerOptions().find((o) => o.template.id === this.ledgerTemplateId())?.template,
  );

  protected readonly budgetPickRows = computed<BudgetPick[]>(() => {
    const template = this.selectedLedgerTemplate();
    if (!template) return [];
    const all = this.templates() ?? {};
    const currency = this.currency();
    const picks = this.budgetPicks();
    const budgets = new Map<string, string>();
    for (const line of template.lines) {
      if (!budgets.has(line.budgetId)) budgets.set(line.budgetId, line.budgetName);
    }
    return [...budgets].map(([budgetId, budgetName]) => {
      const options = (all[budgetId] ?? [])
        .filter((t) => t.currency === currency && t.lines.length > 0)
        .map((t): TemplateOption => ({
          template: t,
          label:
            t.minimumAmount > 0
              ? translate('split.templateOption', { name: t.name, amount: formatMoney(t.minimumAmount, currency) })
              : t.name,
          usable: true,
        }));
      const picked = options.find((o) => o.template.id === picks[budgetId]) ?? options[0];
      return { budgetId, budgetName, options, templateId: picked?.template.id ?? '' };
    });
  });

  protected readonly selectedTemplate = computed(
    () => this.options().find((o) => o.template.id === this.templateId())?.template,
  );

  protected readonly funds = computed<StepFund[]>(() =>
    this.budgets().flatMap((b) => b.funds.map((f) => ({ id: f.id, label: f.name, group: b.name }))),
  );

  protected readonly customProblem = computed(() => {
    const problem = stepProblemText(stepsProblem(this.steps()));
    if (problem) return problem;
    const minimum = minimumAmount(toAllocationLines(this.steps()));
    const amount = this.amount();
    if (this.amountReady() && minimum !== null && Math.round(minimum * 100) > Math.round((amount as number) * 100)) {
      return translate('steps.belowMinimum', { amount: formatMoney(minimum, this.currency()) });
    }
    return '';
  });

  private readonly allocation = computed<AllocationInput | null>(() => {
    switch (this._mode()) {
      case 'template': {
        const template = this.selectedTemplate();
        return template ? { templateId: template.id } : null;
      }
      case 'ledger':
        return this.ledgerAllocation();
      case 'custom':
        return this.customProblem() ? null : { lines: toAllocationLines(this.steps()) };
      default:
        return null;
    }
  });

  private readonly ledgerAllocation = computed<AllocationInput | null>(() => {
    const template = this.selectedLedgerTemplate();
    const rows = this.budgetPickRows();
    if (!template || rows.some((r) => !r.templateId)) return null;
    return {
      ledgerTemplate: { templateId: template.id, budgetTemplateIds: rows.map((r) => r.templateId) },
    };
  });

  private readonly query = computed<PreviewQuery | null>(
    () => {
      const ledgerId = this.ledgers.currentLedgerId();
      const amount = this.amount();
      const allocation = this.allocation();
      if (!ledgerId || !allocation || this.disabled() || !this.amountReady()) return null;
      return { ledgerId, amount: amount as number, currency: this.currency(), allocation };
    },
    { equal: (a, b) => keyOf(a) === keyOf(b) },
  );

  protected readonly previewView = computed(() => {
    const key = keyOf(this.query());
    if (!key) return null;
    const state = this.preview();
    if (!state || state.key !== key) return { loading: true, result: null, error: '' };
    return { loading: false, result: state.result, error: state.error };
  });

  protected readonly showBudget = computed(
    () => this.budgets().filter((b) => b.funds.length > 0).length > 1,
  );

  constructor() {
    effect(() => {
      const budgets = this.budgets();
      untracked(() => {
        this.loadTemplates(budgets);
        this.loadLedgerTemplates(budgets);
      });
    });
    effect(() => {
      this.ledgerOptions();
      untracked(() => this.pickLedgerTemplate());
    });
    effect(() => {
      const prefill = this.prefill();
      untracked(() => this.apply(prefill));
    });
    effect(() => {
      this.options();
      untracked(() => this.pickTemplate());
    });

    toObservable(this.query)
      .pipe(
        debounceTime(PREVIEW_DEBOUNCE_MS),
        switchMap((query) => (query ? this.fetchPreview(query) : of(null))),
        takeUntilDestroyed(),
      )
      .subscribe((state) => this.preview.set(state));
  }

  build(): SplitResult {
    switch (this._mode()) {
      case 'none':
        return {};
      case 'template': {
        const template = this.selectedTemplate();
        if (!template) {
          return { error: translate('split.chooseTemplate', { currency: this.currency() }) };
        }
        return { allocation: { templateId: template.id } };
      }
      case 'ledger': {
        if (!this.selectedLedgerTemplate()) {
          return { error: translate('split.chooseLedgerTemplate', { currency: this.currency() }) };
        }
        const missing = this.budgetPickRows().find((r) => !r.templateId);
        if (missing) {
          return {
            error: translate('split.budgetNeedsTemplate', { name: missing.budgetName, currency: this.currency() }),
          };
        }
        const allocation = this.ledgerAllocation();
        return allocation ? { allocation } : { error: translate('split.invalid') };
      }
      case 'custom': {
        const problem = this.customProblem();
        if (problem) return { error: problem };
        return { allocation: { lines: toAllocationLines(this.steps()) } };
      }
    }
  }

  protected setMode(mode: SplitMode): void {
    this.touch();
    this._mode.set(mode);
    if (mode === 'template') this.pickTemplate();
    if (mode === 'ledger') this.pickLedgerTemplate();
    if (mode === 'custom' && this.steps().length === 0) this.steps.set([this.firstStep()]);
  }

  protected pickOption(templateId: string): void {
    this.touch();
    this.templateId.set(templateId);
  }

  protected pickLedgerOption(templateId: string): void {
    this.touch();
    this.ledgerTemplateId.set(templateId);
  }

  protected pickBudgetTemplate(budgetId: string, templateId: string): void {
    this.touch();
    this.budgetPicks.update((picks) => ({ ...picks, [budgetId]: templateId }));
  }

  protected setSteps(steps: StepDraft[]): void {
    this.touch();
    this.steps.set(steps);
  }

  protected customize(): void {
    const template = this.selectedTemplate();
    if (!template) return;
    this.steps.set(stepsFromLines(template.lines));
    this.touch();
    this._mode.set('custom');
  }

  private touch(): void {
    this._touched.set(true);
    this.pending = null;
  }

  private firstStep(): StepDraft {
    return newStep(this.funds()[0]?.id ?? '');
  }

  private apply(prefill: SplitPrefill | null): void {
    this._touched.set(prefill?.touched ?? false);
    this.steps.set((prefill?.lines ?? []).map((a) => newStep(a.fundId, 'Fixed', a.amount)));
    const mode = prefill?.mode ?? (this.allowNone() ? 'none' : 'auto');
    this.pending = null;
    if (mode === 'auto') {
      if (this.templates() === null) {
        this.pending = { ...prefill, mode };
        this._mode.set('template');
        return;
      }
      this._mode.set(this.options().length > 0 ? 'template' : 'custom');
    } else {
      this._mode.set(mode);
    }
    if (this._mode() === 'custom' && this.steps().length === 0) {
      this.steps.set([this.firstStep()]);
    }
    this.pickTemplate();
  }

  private pickTemplate(): void {
    if (this._mode() !== 'template') return;
    const options = this.options();
    if (options.some((o) => o.template.id === this.templateId())) return;
    this.templateId.set(options.find((o) => o.usable)?.template.id ?? '');
  }

  private pickLedgerTemplate(): void {
    if (this._mode() !== 'ledger') return;
    const options = this.ledgerOptions();
    if (options.some((o) => o.template.id === this.ledgerTemplateId())) return;
    this.ledgerTemplateId.set(options.find((o) => o.usable)?.template.id ?? '');
  }

  private loadLedgerTemplates(budgets: readonly BudgetDTO[]): void {
    this.ledgerTemplateRequest?.unsubscribe();
    const ledgerId = this.ledgers.currentLedgerId();
    if (!ledgerId || budgets.length < 2) {
      this.ledgerTemplates.set([]);
      return;
    }
    this.ledgerTemplates.set(null);
    this.ledgerTemplateRequest = this.ledgers
      .getAllocationTemplates(ledgerId)
      .pipe(
        catchError(() => of<LedgerAllocationTemplateDTO[]>([])),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((list) => this.ledgerTemplates.set(list));
  }

  private loadTemplates(budgets: readonly BudgetDTO[]): void {
    this.templateRequest?.unsubscribe();
    if (budgets.length === 0) {
      this.templates.set({});
      this.afterTemplates();
      return;
    }
    this.templates.set(null);
    this.templateRequest = forkJoin(
      budgets.map((budget) =>
        this.budgetsApi.getAllocationTemplates(budget.id).pipe(
          map((list): [string, AllocationTemplateDTO[]] => [budget.id, list]),
          catchError(() => of<[string, AllocationTemplateDTO[]]>([budget.id, []])),
        ),
      ),
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((entries) => {
        this.templates.set(Object.fromEntries(entries));
        this.afterTemplates();
      });
  }

  private afterTemplates(): void {
    if (this.pending) this.apply(this.pending);
    else this.pickTemplate();
  }

  private fetchPreview(query: PreviewQuery): Observable<PreviewState> {
    const key = keyOf(query);
    const body = { amount: query.amount, currency: query.currency, allocation: query.allocation };
    return this.api.preview(query.ledgerId, body).pipe(
      map((result) => ({ key, result, error: '' })),
      catchError((response: HttpErrorResponse) =>
        of({
          key,
          result: null,
          error: httpErrorMessage(response, {
            invalid: translate('split.invalid'),
            gone: translate('error.fundGone'),
            fallback: translate('split.previewError'),
          }),
        }),
      ),
    );
  }
}
