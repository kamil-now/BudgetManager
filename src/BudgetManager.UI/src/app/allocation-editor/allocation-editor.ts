import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, Subscription, concatMap, from, tap } from 'rxjs';
import { translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { minimumAmount, stepsProblem, toAllocationLines } from '../shared/allocation';
import {
  AllocationSteps,
  StepDraft,
  StepFund,
  stepProblemText,
  stepsFromLines,
} from '../shared/allocation-steps/allocation-steps';
import { BudgetService } from '../shared/budget';
import { httpErrorMessage } from '../shared/http-error';
import { LedgerService } from '../shared/ledger';
import { Modal } from '../shared/modal';
import { AllocationTemplateDTO, BudgetDTO } from '../shared/models';
import { currencyOptions, formatMoney } from '../shared/money';
import { PreferencesService } from '../shared/preferences';

type LoadState = 'loading' | 'ready' | 'error';

const maxNameLength = 100;

interface Draft {
  key: string;
  id: string | null;
  name: string;
  currency: string;
  steps: StepDraft[];
  saved: string | null;
}

let draftSeq = 0;

@Component({
  selector: 'app-allocation-editor',
  imports: [AllocationSteps, FormsModule, Modal, TranslatePipe],
  templateUrl: './allocation-editor.html',
  styleUrl: './allocation-editor.scss',
})
export class AllocationEditor {
  private readonly budgets = inject(BudgetService);
  private readonly ledgers = inject(LedgerService);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly prefs = inject(PreferencesService);

  readonly budget = input<BudgetDTO | null>(null);
  readonly closed = output<void>();
  readonly saved = output<void>();

  protected readonly maxNameLength = maxNameLength;
  protected readonly formatMoney = formatMoney;
  protected readonly loadState = signal<LoadState>('loading');
  protected readonly loadError = signal('');
  protected readonly submitting = signal(false);
  protected readonly removing = signal(false);
  protected readonly confirmingRemove = signal(false);
  protected readonly error = signal('');

  protected readonly drafts = signal<Draft[]>([]);
  protected readonly selectedKey = signal<string | null>(null);
  protected readonly sampleIncome = signal<number | null>(null);

  protected readonly selected = computed(
    () => this.drafts().find((d) => d.key === this.selectedKey()) ?? null,
  );

  protected readonly funds = computed<StepFund[]>(() =>
    (this.budget()?.funds ?? []).map((f) => ({ id: f.id, label: f.name })),
  );

  protected readonly currencies = computed(() => currencyOptions(this.prefs.favoriteCurrencies()));

  private request?: Subscription;

  constructor() {
    effect(() => {
      const budget = this.budget();
      untracked(() => this.reset(budget));
    });
    inject(DestroyRef).onDestroy(() => this.request?.unsubscribe());
  }

  protected retry(): void {
    const budget = this.budget();
    if (budget) this.load(budget);
  }

  protected minimumOf(draft: Draft): number | null {
    return minimumAmount(toAllocationLines(draft.steps));
  }

  protected select(key: string): void {
    this.selectedKey.set(key);
    this.sampleIncome.set(null);
    this.confirmingRemove.set(false);
    this.error.set('');
  }

  protected addTemplate(): void {
    const draft: Draft = {
      key: `new-${++draftSeq}`,
      id: null,
      name: '',
      currency: this.prefs.defaultCurrency(),
      steps: [],
      saved: null,
    };
    this.drafts.update((list) => [...list, draft]);
    this.select(draft.key);
    this.focusAfterRender('#ae-name');
  }

  protected patch(key: string, patch: Partial<Draft>): void {
    this.drafts.update((list) => list.map((d) => (d.key === key ? { ...d, ...patch } : d)));
  }

  protected problem(draft: Draft): string {
    if (!draft.name.trim()) return translate('templates.nameRequired');
    return stepProblemText(stepsProblem(draft.steps));
  }

  protected askRemove(): void {
    const draft = this.selected();
    if (!draft) return;
    this.error.set('');
    if (draft.id === null) {
      this.dropDraft(draft.key);
      return;
    }
    this.confirmingRemove.set(true);
    this.focusAfterRender('#ae-confirm-yes');
  }

  protected cancelRemove(): void {
    this.confirmingRemove.set(false);
    this.focusAfterRender('#ae-remove');
  }

  protected remove(): void {
    const budget = this.budget();
    const draft = this.selected();
    if (!budget || !draft?.id || this.removing()) return;
    this.removing.set(true);
    this.error.set('');
    this.request = this.budgets.deleteAllocationTemplate(budget.id, draft.id).subscribe({
      next: () => {
        this.removing.set(false);
        this.dropDraft(draft.key);
        this.ledgers.requestReload();
      },
      error: (response: HttpErrorResponse) => {
        this.removing.set(false);
        this.error.set(
          httpErrorMessage(response, {
            invalid: translate('templates.removeInvalid'),
            gone: translate('templates.removeGone'),
            fallback: translate('templates.removeFallback'),
          }),
        );
      },
    });
  }

  protected save(): void {
    const budget = this.budget();
    if (!budget || this.loadState() !== 'ready' || this.submitting() || this.removing()) return;
    const changed = this.drafts().filter(isDirty);
    if (changed.length === 0) {
      this.closed.emit();
      return;
    }
    const invalid = changed.find((d) => this.problem(d));
    if (invalid) {
      this.select(invalid.key);
      this.error.set(`${label(invalid)}: ${this.problem(invalid)}`);
      return;
    }

    this.error.set('');
    this.confirmingRemove.set(false);
    this.submitting.set(true);
    let current = changed[0];
    let savedAny = false;
    this.request = from(changed)
      .pipe(
        concatMap((draft) => {
          current = draft;
          const name = draft.name.trim();
          const lines = toAllocationLines(draft.steps);
          const { currency } = draft;
          const create = this.budgets
            .createAllocationTemplate(budget.id, { name, currency, lines })
            .pipe(tap((id) => this.patch(draft.key, { id })));
          const request: Observable<unknown> = draft.id
            ? this.budgets.updateAllocationTemplate(budget.id, draft.id, { name, lines })
            : create;
          return request.pipe(
            tap(() => {
              savedAny = true;
              this.patch(draft.key, { saved: snapshot({ ...draft, name }) });
            }),
          );
        }),
      )
      .subscribe({
        complete: () => {
          this.submitting.set(false);
          this.ledgers.requestReload();
          this.saved.emit();
        },
        error: (response: HttpErrorResponse) => {
          this.submitting.set(false);
          if (savedAny) this.ledgers.requestReload();
          this.select(current.key);
          const message = httpErrorMessage(response, {
            invalid: translate('templates.linesInvalid'),
            gone: translate('templates.budgetGone'),
            conflict: translate('templates.nameTaken'),
            fallback: translate('templates.saveFallback'),
          });
          this.error.set(`${label(current)}: ${message}`);
        },
      });
  }

  private reset(budget: BudgetDTO | null): void {
    this.request?.unsubscribe();
    this.submitting.set(false);
    this.removing.set(false);
    this.confirmingRemove.set(false);
    this.error.set('');
    this.drafts.set([]);
    this.selectedKey.set(null);
    this.sampleIncome.set(null);
    if (budget) this.load(budget);
  }

  private load(budget: BudgetDTO): void {
    this.request?.unsubscribe();
    this.loadState.set('loading');
    this.loadError.set('');
    this.request = this.budgets.getAllocationTemplates(budget.id).subscribe({
      next: (templates) => {
        const drafts = templates.map(draftFrom);
        this.drafts.set(drafts);
        this.selectedKey.set(drafts[0]?.key ?? null);
        this.loadState.set('ready');
      },
      error: (response: HttpErrorResponse) => {
        this.loadState.set('error');
        this.loadError.set(
          httpErrorMessage(response, {
            invalid: translate('templates.loadError'),
            gone: translate('templates.budgetGone'),
            fallback: translate('templates.loadError'),
          }),
        );
      },
    });
  }

  private dropDraft(key: string): void {
    this.drafts.update((list) => list.filter((d) => d.key !== key));
    this.selectedKey.set(this.drafts()[0]?.key ?? null);
    this.sampleIncome.set(null);
    this.confirmingRemove.set(false);
    this.focusAfterRender('.tpl-list button, #ae-new');
  }

  private focusAfterRender(selector: string): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>(selector)?.focus(),
      { injector: this.injector },
    );
  }
}

function draftFrom(template: AllocationTemplateDTO): Draft {
  const draft: Draft = {
    key: template.id,
    id: template.id,
    name: template.name,
    currency: template.currency,
    steps: stepsFromLines(template.lines),
    saved: null,
  };
  draft.saved = snapshot(draft);
  return draft;
}

function snapshot(draft: Draft): string {
  return JSON.stringify({ name: draft.name.trim(), lines: toAllocationLines(draft.steps) });
}

function isDirty(draft: Draft): boolean {
  return draft.saved === null || snapshot(draft) !== draft.saved;
}

function label(draft: Draft): string {
  return draft.name.trim() || draft.currency;
}
