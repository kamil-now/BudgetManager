import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  ElementRef,
  Injector,
  afterNextRender,
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
import { BudgetService, CreateBudgetCommand } from '../shared/budget';
import { httpErrorMessage } from '../shared/http-error';
import { LedgerService } from '../shared/ledger';
import { Modal } from '../shared/modal';
import { BudgetDTO, FundDTO } from '../shared/models';

const maxNameLength = 100;

interface FundForm {
  /** Stable id so ngModel control names survive a fund being removed from the middle. */
  id: number;
  name: string;
  /** The saved fund this row edits; none for a fund added in the form. */
  fundId?: string;
}

let fundSeq = 0;

const newFund = (): FundForm => ({ id: ++fundSeq, name: '' });

const existingFund = (fund: FundDTO): FundForm => ({ id: ++fundSeq, name: fund.name, fundId: fund.id });

@Component({
  selector: 'app-new-budget',
  imports: [FormsModule, Modal, TranslatePipe],
  templateUrl: './new-budget.html',
  styleUrl: './new-budget.scss',
})
export class NewBudget {
  private readonly budgets = inject(BudgetService);
  private readonly ledgers = inject(LedgerService);
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  readonly open = input(false);
  /** The budget to edit; null creates a new one. */
  readonly budget = input<BudgetDTO | null>(null);
  readonly closed = output<void>();
  readonly saved = output<void>();

  private readonly fundsList = viewChild<ElementRef<HTMLElement>>('fundsList');

  protected readonly maxNameLength = maxNameLength;
  protected readonly submitting = signal(false);
  protected readonly error = signal('');

  protected name = '';
  protected readonly funds = signal<FundForm[]>([newFund()]);

  constructor() {
    effect(() => {
      if (this.open()) untracked(() => this.reset());
    });
  }

  protected addFund(): void {
    this.funds.update((list) => [...list, newFund()]);
    afterNextRender(() => this.focusLastFund(), { injector: this.injector });
  }

  protected removeFund(index: number): void {
    this.funds.update((list) => list.filter((_, i) => i !== index));
  }

  protected submit(): void {
    if (this.submitting()) return;
    const problem = this.validate();
    if (problem) return this.error.set(problem);

    this.error.set('');
    this.submitting.set(true);
    const editing = this.budget();
    const request: Observable<unknown> = editing
      ? this.budgets.updateBudget(editing.id, {
        name: this.name.trim(),
        funds: this.funds().map((f) => ({ id: f.fundId ?? null, name: f.name.trim() })),
      })
      : this.budgets.createBudget(this.buildCommand());
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
            gone: translate(editing ? 'budget.gone' : 'error.ledgerGone'),
            conflict: translate('budget.conflict'),
            fallback: translate(editing ? 'budget.saveFallback' : 'budget.fallback'),
          }),
        );
      },
    });
  }

  private validate(): string {
    const names = this.funds().map((f) => f.name.trim());
    if (!this.budget() && !this.ledgers.currentLedgerId()) return translate('error.noLedger');
    if (!this.name.trim()) return translate('budget.nameRequired');
    if (names.some((n) => !n)) return translate('error.fundName');
    if (new Set(names).size !== names.length) return translate('wizard.err.uniqueFunds');
    return '';
  }

  private buildCommand(): CreateBudgetCommand {
    return {
      ledgerId: this.ledgers.currentLedgerId()!,
      name: this.name.trim(),
      funds: this.funds().map((f) => ({ name: f.name.trim() })),
    };
  }

  private reset(): void {
    this.error.set('');
    this.submitting.set(false);
    const budget = this.budget();
    this.name = budget?.name ?? '';
    this.funds.set(budget?.funds.length ? budget.funds.map(existingFund) : [newFund()]);
  }

  private focusLastFund(): void {
    const names = this.fundsList()?.nativeElement.querySelectorAll<HTMLElement>('.f-name input');
    names?.[names.length - 1]?.focus();
  }
}
