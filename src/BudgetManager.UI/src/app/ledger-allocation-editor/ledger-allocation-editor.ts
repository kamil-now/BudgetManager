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
import { httpErrorMessage } from '../shared/http-error';
import { LedgerService } from '../shared/ledger';
import { Modal } from '../shared/modal';
import { BudgetDTO, LedgerAllocationLineInput, LedgerAllocationTemplateDTO } from '../shared/models';
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

/**
 * Edits the ledger's allocation templates, which split an income between budgets. The steps reuse the budget
 * template editor's steps, with budgets in place of funds.
 */
@Component({
  selector: 'app-ledger-allocation-editor',
  imports: [AllocationSteps, FormsModule, Modal, TranslatePipe],
  templateUrl: './ledger-allocation-editor.html',
  styleUrl: '../allocation-editor/allocation-editor.scss',
})
export class LedgerAllocationEditor {
  private readonly ledgers = inject(LedgerService);
  private readonly injector = inject(Injector);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly prefs = inject(PreferencesService);

  /** The ledger's budgets; null while the editor is closed. */
  readonly budgets = input<readonly BudgetDTO[] | null>(null);
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

  protected readonly targets = computed<StepFund[]>(() =>
    (this.budgets() ?? []).map((b) => ({ id: b.id, label: b.name })),
  );

  protected readonly currencies = computed(() => currencyOptions(this.prefs.favoriteCurrencies()));

  private request?: Subscription;

  constructor() {
    effect(() => {
      const budgets = this.budgets();
      untracked(() => this.reset(budgets));
    });
    inject(DestroyRef).onDestroy(() => this.request?.unsubscribe());
  }

  protected retry(): void {
    this.load();
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
    this.focusAfterRender('#lae-name');
  }

  protected patch(key: string, patch: Partial<Draft>): void {
    this.drafts.update((list) => list.map((d) => (d.key === key ? { ...d, ...patch } : d)));
  }

  protected problem(draft: Draft): string {
    if (!draft.name.trim()) return translate('templates.nameRequired');
    return stepProblemText(stepsProblem(draft.steps), 'budget');
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
    this.focusAfterRender('#lae-confirm-yes');
  }

  protected cancelRemove(): void {
    this.confirmingRemove.set(false);
    this.focusAfterRender('#lae-remove');
  }

  protected remove(): void {
    const ledgerId = this.ledgers.currentLedgerId();
    const draft = this.selected();
    if (!ledgerId || !draft?.id || this.removing()) return;
    this.removing.set(true);
    this.error.set('');
    this.request = this.ledgers.deleteAllocationTemplate(ledgerId, draft.id).subscribe({
      next: () => {
        this.removing.set(false);
        this.dropDraft(draft.key);
      },
      error: (response: HttpErrorResponse) => {
        this.removing.set(false);
        this.error.set(
          httpErrorMessage(response, {
            invalid: translate('templates.removeInvalid'),
            gone: translate('ledgerTemplates.removeGone'),
            fallback: translate('templates.removeFallback'),
          }),
        );
      },
    });
  }

  protected save(): void {
    const ledgerId = this.ledgers.currentLedgerId();
    if (!ledgerId || this.loadState() !== 'ready' || this.submitting() || this.removing()) return;
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
    this.request = from(changed)
      .pipe(
        concatMap((draft) => {
          current = draft;
          const name = draft.name.trim();
          const lines = toLedgerLines(draft.steps);
          const { currency } = draft;
          const create = this.ledgers
            .createAllocationTemplate(ledgerId, { name, currency, lines })
            .pipe(tap((id) => this.patch(draft.key, { id })));
          const request: Observable<unknown> = draft.id
            ? this.ledgers.updateAllocationTemplate(ledgerId, draft.id, { name, lines })
            : create;
          return request.pipe(tap(() => this.patch(draft.key, { saved: snapshot({ ...draft, name }) })));
        }),
      )
      .subscribe({
        complete: () => {
          this.submitting.set(false);
          this.saved.emit();
        },
        error: (response: HttpErrorResponse) => {
          this.submitting.set(false);
          this.select(current.key);
          const message = httpErrorMessage(response, {
            invalid: translate('templates.linesInvalid'),
            gone: translate('ledgerTemplates.ledgerGone'),
            conflict: translate('ledgerTemplates.nameTaken'),
            fallback: translate('templates.saveFallback'),
          });
          this.error.set(`${label(current)}: ${message}`);
        },
      });
  }

  private reset(budgets: readonly BudgetDTO[] | null): void {
    this.request?.unsubscribe();
    this.submitting.set(false);
    this.removing.set(false);
    this.confirmingRemove.set(false);
    this.error.set('');
    this.drafts.set([]);
    this.selectedKey.set(null);
    this.sampleIncome.set(null);
    if (budgets) this.load();
  }

  private load(): void {
    const ledgerId = this.ledgers.currentLedgerId();
    if (!ledgerId) return;
    this.request?.unsubscribe();
    this.loadState.set('loading');
    this.loadError.set('');
    this.request = this.ledgers.getAllocationTemplates(ledgerId).subscribe({
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
            gone: translate('ledgerTemplates.ledgerGone'),
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
    this.focusAfterRender('.tpl-list button, #lae-new');
  }

  private focusAfterRender(selector: string): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>(selector)?.focus(),
      { injector: this.injector },
    );
  }
}

function toLedgerLines(steps: readonly StepDraft[]): LedgerAllocationLineInput[] {
  return toAllocationLines(steps).map(({ fundId, ...line }) => ({ budgetId: fundId, ...line }));
}

function draftFrom(template: LedgerAllocationTemplateDTO): Draft {
  const draft: Draft = {
    key: template.id,
    id: template.id,
    name: template.name,
    currency: template.currency,
    steps: stepsFromLines(template.lines.map((l) => ({ ...l, fundId: l.budgetId }))),
    saved: null,
  };
  draft.saved = snapshot(draft);
  return draft;
}

function snapshot(draft: Draft): string {
  return JSON.stringify({ name: draft.name.trim(), lines: toLedgerLines(draft.steps) });
}

function isDirty(draft: Draft): boolean {
  return draft.saved === null || snapshot(draft) !== draft.saved;
}

function label(draft: Draft): string {
  return draft.name.trim() || draft.currency;
}
