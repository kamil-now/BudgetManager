import { createAccountTransferRequest, deleteAccountTransferRequest, updateAccountTransferRequest } from '@/api/account-transfer-requests';
import { createAllocationRequest, deleteAllocationRequest, updateAllocationRequest } from '@/api/allocation-requests';
import { getBalanceRequest } from '@/api/balance-requests';
import { createCurrencyExchangeRequest, deleteCurrencyExchangeRequest, updateCurrencyExchangeRequest } from '@/api/currency-exchange-requests';
import { createExpenseRequest, deleteExpenseRequest, getExpenseRequest, updateExpenseRequest } from '@/api/expense-requests';
import { createFundTransferRequest, deleteFundTransferRequest, updateFundTransferRequest } from '@/api/fund-transfer-requests';
import { createIncomeRequest, deleteIncomeRequest, getIncomeRequest, updateIncomeRequest } from '@/api/income-requests';
import { Account } from '@/models/account';
import { AccountTransfer } from '@/models/account-transfer';
import { Allocation } from '@/models/allocation';
import { Balance } from '@/models/balance';
import { BudgetSummary } from '@/models/budget-summary';
import { CurrencyExchange } from '@/models/currency-exchange';
import { Expense } from '@/models/expense';
import { Fund } from '@/models/fund';
import { FundTransfer } from '@/models/fund-transfer';
import { Income } from '@/models/income';
import { MoneyOperation } from '@/models/money-operation';
import { MoneyOperationType } from '@/models/money-operation-type.enum';
import axios from 'axios';
import { DefineStoreOptions, Store, defineStore } from 'pinia';
import { createAccountRequest, deleteAccountRequest, getAccountRequest, updateAccountRequest } from '../api/account-requests';
import { fetchBudgetSummary } from '../api/fetch-budget-request';
import { createFundRequest, deleteFundRequest, getFundRequest, updateFundRequest } from '../api/fund-requests';

export type AppState = {
  isLoading: boolean;
  isLoggedIn: boolean;
  isNewUser: boolean,
  budget: BudgetSummary,
};
export type AppGetters = {
  balance: (state: AppState) => Balance,
  unallocated: (state: AppState) => Balance,
  funds: (state: AppState) => Fund[],
  accounts: (state: AppState) => Account[],
  incomes: (state: AppState) => MoneyOperation[],
  allocations: (state: AppState) => MoneyOperation[],
  expenses: (state: AppState) => MoneyOperation[],
  currencyExchanges: (state: AppState) => MoneyOperation[],
  fundTransfers: (state: AppState) => MoneyOperation[],
  accountTransfers: (state: AppState) => MoneyOperation[],
  // findIndexById: (state: AppState) => (id: string) => number;
};
export type AppActions = {
  createBudget(): void;
  updateUserSettings(): void,
  setLoggedIn(value: boolean): void;
  fetchBudget(): void;

  createNewAccount(account: Account): void,
  updateAccount(account: Account): void;
  deleteAccount(accountId: string): void;

  createNewFund(fund: Fund): void,
  updateFund(fund: Fund): void;
  deleteFund(fundId: string): void;
  
  createNewIncome(income: Income): void,
  updateIncome(income: Income): void;
  deleteIncome(incomeId: string): void;

  createNewExpense(expense: Expense): void,
  updateExpense(expense: Expense): void;
  deleteExpense(expenseId: string): void;

  createNewAllocation(allocation: Allocation): void,
  updateAllocation(allocation: Allocation): void;
  deleteAllocation(allocationId: string): void;

  createNewFundTransfer(fundTransfer: FundTransfer): void,
  updateFundTransfer(fundTransfer: FundTransfer): void;
  deleteFundTransfer(fundTransferId: string): void;
  
  createNewAccountTransfer(accountTransfer: AccountTransfer): void,
  updateAccountTransfer(accountTransfer: AccountTransfer): void;
  deleteAccountTransfer(accountTransferId: string): void;

  createNewCurrencyExchange(currencyExchange: CurrencyExchange): void,
  updateCurrencyExchange(currencyExchange: CurrencyExchange): void;
  deleteCurrencyExchange(currencyExchangeId: string): void;
};
export type AppStore = Store<string, AppState, AppGetters, AppActions>;

export const getInitialAppState: () => AppState = () => ({
  isLoading: true,
  isLoggedIn: false,
  isNewUser: true,
  budget: {
    userSettings: { accountsOrder: [], fundsOrder: [] },
    balance: {},
    unallocated: {},
    funds: [],
    accounts: [],
    operations: []
  }
});

export const APP_STORE: DefineStoreOptions<
  string,
  AppState,
  AppGetters,
  AppActions
> = {
  id: 'app',
  state: () => getInitialAppState(),
  getters: {
    balance: (state: AppState) => state.budget.balance,
    unallocated: (state: AppState) => state.budget.unallocated,
    funds: (state: AppState) => state.budget.funds.filter(x => !x.isDeleted),
    accounts: (state: AppState) => state.budget.accounts.filter(x => !x.isDeleted),
    incomes: (state: AppState) => state.budget.operations.filter(x => x.type === MoneyOperationType.Income),
    allocations: (state: AppState) => state.budget.operations.filter(x => x.type === MoneyOperationType.Allocation),
    expenses: (state: AppState) => state.budget.operations.filter(x => x.type === MoneyOperationType.Expense),
    currencyExchanges: (state: AppState) => state.budget.operations.filter(x => x.type === MoneyOperationType.CurrencyExchange),
    accountTransfers: (state: AppState) => state.budget.operations.filter(x => x.type === MoneyOperationType.AccountTransfer),
    fundTransfers: (state: AppState) => state.budget.operations.filter(x => x.type === MoneyOperationType.FundTransfer)
  },
  actions: {
    async setLoggedIn(value: boolean) {
      this.isLoggedIn = value;
      this.fetchBudget();
    },
    async fetchBudget() {
      await Utils.runAsyncOperation(this, () =>
        fetchBudgetSummary()
          .then(res => {
            if (res !== null) {
              this.budget = res;
              this.isNewUser = false;
            } else {
              this.isNewUser = true;
            }
          },
          (error) => {
            console.error(error);
          })
      );
    },
    async createBudget() {
      await Utils.runAsyncOperation(this, () => axios.post<void>('budget'));
    },
    async updateUserSettings() {
      await axios.put('user-settings', { 
        accountsOrder: this.budget.accounts.map(x => x.id),
        fundsOrder: this.budget.funds.map(x => x.id)
      });
    },
    
    async createNewAccount(account: Account) {
      await Utils.runAsyncOperation(this, async (state) => {
        const id = await createAccountRequest(account);
        const fromResponse = await getAccountRequest(id);
        state.budget.accounts.unshift(fromResponse);
        this.updateUserSettings();
      });
    },
    async updateAccount(account: Account) {
      await Utils.runAsyncOperation(this, (state) => 
        updateAccountRequest(account)
          .then(async account => {
            const fromState = state.budget.accounts.find(x => x.id === account.id);
            if (!fromState || !fromState.id) {
              throw new Error('Invalid operation - account does not exist.');
            }
            const index = state.budget.accounts.indexOf(fromState);
            const res = await getAccountRequest(fromState.id);
            state.budget.accounts[index] = res;
          })
      );
    },
    async deleteAccount(accountId: string) {
      await Utils.runAsyncOperation(this, (state) => 
        deleteAccountRequest(accountId)
          .then(() => state.budget.accounts = state.budget.accounts.filter(x => x.id !== accountId))
      );
    },

    async createNewFund(
      fund: Fund,
    ) {
      await Utils.runAsyncOperation(this, (state) => 
        createFundRequest(fund)
          .then(id => {
            state.budget.funds.unshift({ ...fund, id });
            this.updateUserSettings();
          })
      );
    },
    async updateFund(fund: Fund) {
      await Utils.runAsyncOperation(this, (state) => 
        updateFundRequest(fund)
          .then(() => {
            const fromState = state.budget.funds.find(x => x.id === fund.id);
            if (!fromState) {
              throw new Error('Invalid operation - fund does not exist.');
            }
            const index = state.budget.funds.indexOf(fromState);
            state.budget.funds[index] = fund; 
          })
      );
    },
    async deleteFund(fundId: string) {
      await Utils.runAsyncOperation(this, (state) => 
        deleteFundRequest(fundId)
          .then(() => state.budget.funds = state.budget.funds.filter(x => x.id !== fundId))
      );
    },

    async createNewExpense(
      expense: Expense,
    ) {
      await Utils.runAsyncOperation(this, async (state) => {
        const id = await createExpenseRequest(expense); 
        const fromResponse = await getExpenseRequest(id);
        state.budget.operations.unshift(fromResponse);

        await Utils.reloadAccount(this, expense.accountId);
        await Utils.reloadFund(this, expense.fundId);
        await Utils.reloadBalance(this);
      });
    },
    async updateExpense(expense: Expense) {
      await Utils.runAsyncOperation(this, (state) => 
        updateExpenseRequest(expense)
          .then(async expense => {
            const fromState = state.budget.operations.find(x => x.id === expense.id);
            if (!fromState) {
              throw new Error('Invalid operation - expense does not exist.');
            }
            const index = state.budget.operations.indexOf(fromState);
            state.budget.operations[index] = expense;
            
            await Utils.reloadAccount(this, expense.accountId);
            await Utils.reloadFund(this, expense.fundId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async deleteExpense(expenseId: string) {
      await Utils.runAsyncOperation(this, () => 
        deleteExpenseRequest(expenseId)
          .then(async () => {
            const fromState = this.budget.operations.find(x => x.id === expenseId);
            if (!fromState || !fromState.accountId || !fromState.fundId) {
              throw new Error('Invalid operation - expense data is invalid.');
            }
            await Utils.reloadAccount(this, fromState.accountId);
            await Utils.reloadFund(this, fromState.fundId);
            await Utils.reloadBalance(this);
          })
      );
    },

    async createNewIncome(
      income: Income,
    ) {
      await Utils.runAsyncOperation(this, async (state) => {
        const id = await createIncomeRequest(income); 
        const fromResponse = await getIncomeRequest(id);
        state.budget.operations.unshift(fromResponse);

        await Utils.reloadAccount(this, income.accountId);
        await Utils.reloadBalance(this);
      });
    },
    async updateIncome(income: Income) {
      await Utils.runAsyncOperation(this, (state) => 
        updateIncomeRequest(income)
          .then(async income => {
            const fromState = state.budget.operations.find(x => x.id === income.id);
            if (!fromState) {
              throw new Error('Invalid operation - income does not exist.');
            }
            const index = state.budget.operations.indexOf(fromState);
            state.budget.operations[index] = income;

            await Utils.reloadAccount(this, income.accountId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async deleteIncome(incomeId: string) {
      await Utils.runAsyncOperation(this, () => 
        deleteIncomeRequest(incomeId)
          .then(async () => {
            const fromState = this.budget.operations.find(x => x.id === incomeId);
            if (!fromState || !fromState.accountId) {
              throw new Error('Invalid operation - income data is invalid.');
            }
            await Utils.reloadAccount(this, fromState.accountId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async createNewAllocation(
      allocation: Allocation,
    ) {
      await Utils.runAsyncOperation(this, (state) => 
        createAllocationRequest(allocation)
          .then(async id => {
            state.budget.operations.unshift({ ...allocation, id }); 
            
            await Utils.reloadFund(this, allocation.fundId);
            await Utils.reloadFund(this, allocation.targetFundId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async updateAllocation(allocation: Allocation) {
      await Utils.runAsyncOperation(this, (state) => 
        updateAllocationRequest(allocation)
          .then(async allocation => {
            const fromState = state.budget.operations.find(x => x.id === allocation.id);
            if (!fromState) {
              throw new Error('Invalid operation - allocation does not exist.');
            }
            const index = state.budget.operations.indexOf(fromState);
            state.budget.operations[index] = allocation; 
            
            await Utils.reloadFund(this, allocation.fundId);
            await Utils.reloadFund(this, allocation.targetFundId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async deleteAllocation(allocationId: string) {
      await Utils.runAsyncOperation(this, () => 
        deleteAllocationRequest(allocationId)
          .then(async () => {
            const fromState = this.budget.operations.find(x => x.id === allocationId);
            if (!fromState || !fromState.fundId || !fromState.targetFundId) {
              throw new Error('Invalid operation - allocation data is invalid.');
            }
            await Utils.reloadFund(this, fromState.fundId);
            await Utils.reloadFund(this, fromState.targetFundId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async createNewFundTransfer(
      fundTransfer: FundTransfer,
    ) {
      await Utils.runAsyncOperation(this, (state) => 
        createFundTransferRequest(fundTransfer)
          .then(async id => {
            state.budget.operations.unshift({ ...fundTransfer, id }); 
            
            await Utils.reloadFund(this, fundTransfer.fundId);
            await Utils.reloadFund(this, fundTransfer.targetFundId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async updateFundTransfer(fundTransfer: FundTransfer) {
      await Utils.runAsyncOperation(this, (state) => 
        updateFundTransferRequest(fundTransfer)
          .then(async fundTransfer => {
            const fromState = state.budget.operations.find(x => x.id === fundTransfer.id);
            if (!fromState) {
              throw new Error('Invalid operation - fund transfer does not exist.');
            }
            const index = state.budget.operations.indexOf(fromState);
            state.budget.operations[index] = fundTransfer; 
            
            await Utils.reloadFund(this, fundTransfer.fundId);
            await Utils.reloadFund(this, fundTransfer.targetFundId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async deleteFundTransfer(fundTransferId: string) {
      await Utils.runAsyncOperation(this, () => 
        deleteFundTransferRequest(fundTransferId)
          .then(async () => {
            const fromState = this.budget.operations.find(x => x.id === fundTransferId);
            if (!fromState || !fromState.fundId || !fromState.targetFundId) {
              throw new Error('Invalid operation - fund transfer data is invalid.');
            }
            
            await Utils.reloadFund(this, fromState.fundId);
            await Utils.reloadFund(this, fromState.targetFundId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async createNewAccountTransfer(
      accountTransfer: AccountTransfer,
    ) {
      await Utils.runAsyncOperation(this, (state) => 
        createAccountTransferRequest(accountTransfer)
          .then(async id => {
            state.budget.operations.unshift({ ...accountTransfer, id }); 
            await Utils.reloadAccount(this, accountTransfer.accountId);
            await Utils.reloadAccount(this, accountTransfer.targetAccountId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async updateAccountTransfer(accountTransfer: AccountTransfer) {
      await Utils.runAsyncOperation(this, (state) => 
        updateAccountTransferRequest(accountTransfer)
          .then(async accountTransfer => {
            const fromState = state.budget.operations.find(x => x.id === accountTransfer.id);
            if (!fromState) {
              throw new Error('Invalid operation - account transfer does not exist.');
            }
            const index = state.budget.operations.indexOf(fromState);
            state.budget.operations[index] = accountTransfer; 

            await Utils.reloadAccount(this, accountTransfer.accountId);
            await Utils.reloadAccount(this, accountTransfer.targetAccountId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async deleteAccountTransfer(accountTransferId: string) {
      await Utils.runAsyncOperation(this, () => 
        deleteAccountTransferRequest(accountTransferId)
          .then(async () => {
            const fromState = this.budget.operations.find(x => x.id === accountTransferId);
            if (!fromState || !fromState.accountId || !fromState.targetAccountId) {
              throw new Error('Invalid operation - account transfer data is invalid.');
            }
            await Utils.reloadAccount(this, fromState.accountId);
            await Utils.reloadAccount(this, fromState.targetAccountId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async createNewCurrencyExchange(
      currencyExchange: CurrencyExchange,
    ) {
      await Utils.runAsyncOperation(this, (state) => 
        createCurrencyExchangeRequest(currencyExchange)
          .then(async id => {
            state.budget.operations.unshift({ ...currencyExchange, id }); 
            await Utils.reloadAccount(this, currencyExchange.accountId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async updateCurrencyExchange(currencyExchange: CurrencyExchange) {
      await Utils.runAsyncOperation(this, (state) => 
        updateCurrencyExchangeRequest(currencyExchange)
          .then(async currencyExchange => {
            const fromState = state.budget.operations.find(x => x.id === currencyExchange.id);
            if (!fromState) {
              throw new Error('Invalid operation - currency exchange does not exist.');
            }
            const index = state.budget.operations.indexOf(fromState);
            state.budget.operations[index] = currencyExchange; 
            await Utils.reloadAccount(this, currencyExchange.accountId);
            await Utils.reloadBalance(this);
          })
      );
    },
    async deleteCurrencyExchange(currencyExchangeId: string) {
      await Utils.runAsyncOperation(this, () => 
        deleteCurrencyExchangeRequest(currencyExchangeId)
          .then(async () => {
            const fromState = this.budget.operations.find(x => x.id === currencyExchangeId);
            if (!fromState || !fromState.accountId) {
              throw new Error('Invalid operation - currency exchange data is invalid.');
            }
            await Utils.reloadAccount(this, fromState.accountId);
            await Utils.reloadBalance(this);
          })
      );
    },
  }
};

export const useAppStore = defineStore<
  string,
  AppState,
  AppGetters,
  AppActions
>(APP_STORE);

class Utils {
  static async runAsyncOperation(
    state: AppState,
    op: (state: AppState) => Promise<unknown>
  ): Promise<void> {
    state.isLoading = true;
    try {
      await op(state);
    } catch (error) {
      // TODO console.error(error);
    } finally {
      state.isLoading = false;
    }
  }

  static async reloadBalance(state: AppState): Promise<void> {
    const budgetBalance = await getBalanceRequest();
    state.budget.balance = budgetBalance.balance;
    state.budget.unallocated = budgetBalance.unallocated;
  }

  static async reloadAccount(state: AppState, accountId: string): Promise<void> {
    const account = await getAccountRequest(accountId);
    const accountIndex = state.budget.accounts.findIndex(x => x.id === account.id);
    state.budget.accounts[accountIndex] = account;
  }

  static async reloadFund(state: AppState, fundId: string): Promise<void> {
    const fund = await getFundRequest(fundId);
    const fundIndex = state.budget.accounts.findIndex(x => x.id === fund.id);
    state.budget.funds[fundIndex] = fund;
  }

  static ensureDefined(actionName: string, ...payload: unknown[]): boolean {
    if (
      payload === null ||
      payload === undefined ||
      payload.some((x) => x === null || x === undefined)
    )
      throw new Error(`${actionName} action payload must be defined`);
    return true;
  }
}
