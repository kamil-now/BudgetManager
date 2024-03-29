import { createAccountTransferRequest, deleteAccountTransferRequest, getAccountTransferRequest, updateAccountTransferRequest } from '@/api/account-transfer-requests';
import { MoneyOperationUtils } from '@/helpers/money-operation-utils';
import { AccountTransfer } from '@/models/account-transfer';
import { AppStore } from '../store';
import { StoreUtils } from '../store-utils';

export interface IAccountTransferActions {   
  createNewAccountTransfer(accountTransfer: AccountTransfer): void,
  updateAccountTransfer(accountTransfer: AccountTransfer): void;
  deleteAccountTransfer(accountTransferId: string): void;
}

export class AccountTransferActions {
  static async createNewAccountTransfer(store: AppStore, accountTransfer: AccountTransfer) {
    await StoreUtils.runAsyncOperation(store, async () => {
      const fromResponse = await StoreUtils.createOperation(
        store,
        () => createAccountTransferRequest(accountTransfer),
        id => getAccountTransferRequest(id)
      );
      await this.reload(store, fromResponse);
    });
  }

  static async updateAccountTransfer(store: AppStore, accountTransfer: AccountTransfer) {
    await StoreUtils.runAsyncOperation(store, async () => {
      const fromResponse = await updateAccountTransferRequest(accountTransfer);
      StoreUtils.replaceInCollection(store.budget.operations, fromResponse);
      MoneyOperationUtils.sort(store.budget.operations);
      await this.reload(store, fromResponse);
    });
  }

  static async deleteAccountTransfer(store: AppStore, accountTransferId: string) {
    await StoreUtils.runAsyncOperation(store, async () => {
      await deleteAccountTransferRequest(accountTransferId);
      await this.reload(store, StoreUtils.getFromCollection(store.accountTransfers, accountTransferId));
      store.budget.operations = store.budget.operations.filter(x => x.id !== accountTransferId);
    });
  }

  private static async reload(store: AppStore, accountTransfer: AccountTransfer) {
    await StoreUtils.reloadAccount(store, accountTransfer.accountId);
    await StoreUtils.reloadAccount(store, accountTransfer.targetAccountId);
    await StoreUtils.reloadBalance(store);
  }
}
