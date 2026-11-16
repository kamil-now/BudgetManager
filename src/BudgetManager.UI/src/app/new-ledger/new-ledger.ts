import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SessionService } from '../auth/session';
import { TranslationKey, translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { allocationType, stepsProblem } from '../shared/allocation';
import { InitialAllocation } from '../shared/allocation-models';
import {
  AllocationSteps,
  StepDraft,
  StepFund,
  newStep,
  stepProblemText,
} from '../shared/allocation-steps/allocation-steps';
import { httpErrorMessage } from '../shared/http-error';
import { LedgerService } from '../shared/ledger';
import { CreateLedgerCommand, NewAllocationTemplateInput } from '../shared/models';
import { formatMoney, hasAtMostTwoDecimals } from '../shared/money';
import { PreferencesService } from '../shared/preferences';
import { AllocateStep } from './allocate-step';
import {
  AllocationPlan,
  PlanBudget,
  PlanTemplate,
  leftover,
  planAllocations,
  toInitialAllocations,
} from './initial-allocation';

type CreateLedgerBody = CreateLedgerCommand & { initialAllocations: InitialAllocation[] };

type Step = number;

const maxNameLength = 100;
const allocateStep = 4;

interface FundForm {
  id: number;
  name: string;
}

interface TemplateForm {
  name: string;
  currency: string;
  steps: StepDraft[];
}

interface BudgetForm {
  name: string;
  funds: FundForm[];
  stepFunds: StepFund[];
  template: TemplateForm | null;
}

interface AccountForm {
  name: string;
  amount: number | null;
  currency: string;
}

function toPlanBudget(b: BudgetForm): PlanBudget {
  const templates: PlanTemplate[] = [];
  if (b.template) {
    const steps = b.template.steps.flatMap((s) => {
      const fund = b.funds.findIndex((f) => String(f.id) === s.fundId);
      return fund < 0 ? [] : [{ fund, kind: s.kind, value: s.value }];
    });
    templates.push({ name: b.template.name, currency: b.template.currency, steps });
  }
  return { name: b.name, funds: b.funds.map((f) => ({ name: f.name })), templates };
}

function toTemplateInputs(b: BudgetForm): NewAllocationTemplateInput[] {
  const template = b.template;
  if (!template) return [];
  const lines = template.steps.map((s, i) => ({
    fund: b.funds.find((f) => String(f.id) === s.fundId)?.name.trim() ?? '',
    sequence: i,
    type: allocationType[s.kind],
    amount: s.kind === 'Fixed' ? s.value : null,
    percent: s.kind === 'Percent' ? s.value : null,
  }));
  return [{ name: template.name.trim(), currency: template.currency, lines }];
}

let fundSeq = 0;

const blankFund = (name = ''): FundForm => ({ id: ++fundSeq, name });

const stepFundsOf = (funds: readonly FundForm[]): StepFund[] =>
  funds.map((f) => ({ id: String(f.id), label: f.name.trim() || translate('common.fundName') }));

const blankTemplate = (currency: string, steps: StepDraft[] = []): TemplateForm => ({
  name: translate('wizard.default.template'),
  currency,
  steps,
});

const defaultBudgets = (currency: string): BudgetForm[] => {
  const [rent, utilities, food, entertainment, savings] = [
    translate('wizard.default.rent'),
    translate('wizard.default.utilities'),
    translate('wizard.default.food'),
    translate('wizard.default.entertainment'),
    translate('wizard.default.savings'),
  ].map((name) => blankFund(name));
  const funds = [rent, utilities, food, entertainment, savings];
  const id = (f: FundForm) => String(f.id);
  return [
    {
      name: translate('wizard.default.budget'),
      funds,
      stepFunds: stepFundsOf(funds),
      template: blankTemplate(currency, [
        newStep(id(rent), 'Fixed', 800),
        newStep(id(utilities), 'Fixed', 200),
        newStep(id(food), 'Fixed', 600),
        newStep(id(entertainment), 'Percent', 20),
        newStep(id(savings), 'Percent', 100),
      ]),
    },
  ];
};

const defaultAccounts = (currency: string): AccountForm[] => [
  { name: translate('wizard.default.cash'), amount: 256, currency },
  { name: translate('wizard.default.mainAccount'), amount: 2048, currency },
  { name: translate('wizard.default.savingsAccount'), amount: 4096, currency },
];

const stepLabels: readonly TranslationKey[] = [
  'wizard.step.ledger',
  'wizard.step.budgets',
  'wizard.step.accounts',
  'wizard.step.allocate',
];

@Component({
  selector: 'app-new-ledger',
  imports: [AllocateStep, AllocationSteps, FormsModule, RouterLink, TranslatePipe],
  templateUrl: './new-ledger.html',
  styleUrl: './new-ledger.scss',
})
export class NewLedger {
  private readonly ledgers = inject(LedgerService);
  private readonly router = inject(Router);
  private readonly prefs = inject(PreferencesService);
  private readonly session = inject(SessionService);
  protected readonly defaultCurrency = this.prefs.defaultCurrency();

  protected readonly maxNameLength = maxNameLength;
  protected readonly currencyOptions = this.prefs.currencyOptions;
  protected readonly stepLabels = stepLabels;
  protected readonly formatMoney = formatMoney;

  protected readonly step = signal<Step>(1);
  protected readonly lastStep = computed(() => this.stepLabels.length);
  protected readonly submitting = signal(false);
  protected readonly error = signal('');

  protected readonly hasLedgers = computed(() => (this.ledgers.ledgerList()?.length ?? 0) > 0);

  protected signOut(): void {
    this.session.signOut();
  }

  protected readonly ledgerName = signal(translate('wizard.default.ledger'));
  private nameEdited = false;

  protected readonly budgets = signal<BudgetForm[]>(defaultBudgets(this.defaultCurrency));
  protected readonly accounts = signal<AccountForm[]>(defaultAccounts(this.defaultCurrency));
  protected readonly plans = signal<AllocationPlan[]>([]);

  protected readonly planBudgets = computed<PlanBudget[]>(() => this.budgets().map(toPlanBudget));

  constructor() {
    this.ledgers.refreshLedgers();
    effect(() => {
      if (this.hasLedgers() && !this.nameEdited) this.ledgerName.set('');
    });
  }

  protected setLedgerName(name: string): void {
    this.nameEdited = true;
    this.ledgerName.set(name);
  }

  protected toggleTheme(): void {
    this.prefs.cycleTheme();
  }

  protected goTo(step: Step): void {
    this.error.set('');
    if (step === allocateStep) {
      this.plans.set(planAllocations(this.planBudgets(), this.accounts(), this.plans()));
    }
    this.step.set(step);
  }

  protected back(): void {
    this.goTo(Math.max(1, this.step() - 1));
  }

  protected next(): void {
    const problem = this.validate(this.step());
    if (problem) {
      this.error.set(problem);
      return;
    }
    if (this.step() >= this.lastStep()) {
      this.submit();
    } else {
      this.goTo(this.step() + 1);
    }
  }

  protected addBudget(): void {
    const funds = [blankFund()];
    this.budgets.update((list) => [
      ...list,
      { name: '', funds, stepFunds: stepFundsOf(funds), template: null },
    ]);
  }

  protected removeBudget(index: number): void {
    this.budgets.update((list) => (list.length > 1 ? list.filter((_, i) => i !== index) : list));
  }

  protected addFund(budget: BudgetForm): void {
    budget.funds = [...budget.funds, blankFund()];
    this.refreshFunds(budget);
  }

  protected removeFund(budget: BudgetForm, index: number): void {
    const removed = budget.funds[index];
    budget.funds = budget.funds.filter((_, i) => i !== index);
    if (budget.template && removed) {
      const id = String(removed.id);
      budget.template.steps = budget.template.steps.filter((s) => s.fundId !== id);
    }
    this.refreshFunds(budget);
  }

  protected refreshFunds(budget: BudgetForm): void {
    budget.stepFunds = stepFundsOf(budget.funds);
    this.touchBudgets();
  }

  protected addTemplate(budget: BudgetForm): void {
    budget.template = blankTemplate(this.defaultCurrency, [
      newStep(budget.funds[0] ? String(budget.funds[0].id) : ''),
    ]);
    this.touchBudgets();
  }

  protected removeTemplate(budget: BudgetForm): void {
    budget.template = null;
    this.touchBudgets();
  }

  protected setSteps(template: TemplateForm, steps: StepDraft[]): void {
    template.steps = steps;
    this.touchBudgets();
  }

  protected touchBudgets(): void {
    this.budgets.update((list) => [...list]);
  }

  protected addAccount(): void {
    this.accounts.update((list) => [
      ...list,
      { name: '', amount: 0, currency: this.defaultCurrency },
    ]);
  }

  protected removeAccount(index: number): void {
    this.accounts.update((list) => list.filter((_, i) => i !== index));
  }

  private validate(step: Step): string {
    if (step === 1) {
      return this.ledgerName().trim() ? '' : translate('wizard.err.ledgerName');
    }
    if (step === 2) {
      const budgets = this.budgets();
      if (budgets.some((b) => !b.name.trim())) return translate('wizard.err.budgetName');
      for (const budget of budgets) {
        if (budget.funds.length === 0) {
          return translate('wizard.err.addFund', { name: budget.name.trim() });
        }
        if (budget.funds.some((f) => !f.name.trim())) return translate('error.fundName');
        if (new Set(budget.funds.map((f) => f.name.trim())).size !== budget.funds.length) {
          return translate('wizard.err.uniqueFunds');
        }
        const template = budget.template;
        if (template) {
          if (!template.name.trim()) return translate('templates.nameRequired');
          const problem = stepProblemText(stepsProblem(template.steps));
          if (problem) return `${template.name.trim()}: ${problem}`;
        }
      }
      return '';
    }
    if (step === 3) {
      if (this.accounts().some((a) => !a.name.trim())) return translate('wizard.err.accountName');
      if (this.accounts().some((a) => a.amount === null || !hasAtMostTwoDecimals(a.amount))) {
        return translate('wizard.err.accountBalance');
      }
      return '';
    }
    if (step !== allocateStep) return '';
    for (const plan of this.plans()) {
      if (plan.total <= 0) continue;
      const amounts = Object.values(plan.amounts);
      if (amounts.some((a) => a !== null && (a < 0 || !hasAtMostTwoDecimals(a)))) {
        return translate('wizard.err.negative');
      }
      const left = leftover(plan.total, plan.amounts);
      const currency = plan.currency;
      if (left > 0) {
        return translate('wizard.err.allocateAll', { currency, amount: formatMoney(left, currency) });
      }
      if (left < 0) {
        return translate('wizard.err.tooMuch', { currency, amount: formatMoney(-left, currency) });
      }
    }
    return '';
  }

  private buildCommand(): CreateLedgerBody {
    return {
      initialAllocations: toInitialAllocations(this.planBudgets(), this.plans()),
      name: this.ledgerName().trim(),
      budgets: this.budgets().map((budget) => ({
        name: budget.name.trim(),
        funds: budget.funds.map((f) => ({ name: f.name.trim() })),
        allocationTemplates: toTemplateInputs(budget),
      })),
      accounts: this.accounts().map((a) => ({
        name: a.name.trim(),
        initialBalance: { amount: a.amount ?? 0, currency: a.currency },
      })),
    };
  }

  private submit(): void {
    if (this.submitting()) return;
    this.error.set('');
    this.submitting.set(true);
    this.ledgers.createLedger(this.buildCommand()).subscribe({
      next: (id) => {
        this.ledgers.setCurrentLedgerId(id);
        this.ledgers.refreshLedgers();
        this.ledgers.requestReload();
        this.router.navigateByUrl('/dashboard');
      },
      error: (response: HttpErrorResponse) => {
        this.submitting.set(false);
        this.error.set(
          httpErrorMessage(response, {
            invalid: translate('error.invalidDetails'),
            fallback: translate('wizard.err.fallback'),
          }),
        );
      },
    });
  }
}
