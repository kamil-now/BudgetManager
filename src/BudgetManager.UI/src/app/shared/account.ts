import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AllocationInput } from './allocation-models';
import { AccountTransactionDTO, Money } from './models';

export interface TransactionBody {
  value: Money;
  date: string;
  title?: string | null;
  allocation?: AllocationInput;
  fundId?: string | null;
}

export interface TransferBody {
  targetAccountId: string;
  value: Money;
  date: string;
  title?: string | null;
}

export interface CurrencyExchangeBody {
  targetAccountId?: string | null;
  buy: Money;
  sell: Money;
  date: string;
  title?: string | null;
}

export interface CreateAccountBody {
  ledgerId: string;
  initialBalance: Money;
  name: string;
}

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);

  createAccount(body: CreateAccountBody): Observable<string> {
    return this.http.post<string>('/api/accounts', body);
  }

  updateAccount(accountId: string, name: string): Observable<void> {
    return this.http.put<void>(`/api/accounts/${accountId}`, { name });
  }

  getTransaction(accountId: string, id: string): Observable<AccountTransactionDTO> {
    return this.http.get<AccountTransactionDTO>(`/api/accounts/${accountId}/transaction/${id}`);
  }

  createTransaction(accountId: string, body: TransactionBody): Observable<string> {
    return this.http.post<string>(`/api/accounts/${accountId}/transaction`, {
      accountId,
      ...body,
    });
  }

  updateTransaction(accountId: string, id: string, body: TransactionBody): Observable<void> {
    const { value, ...rest } = body;
    return this.http.put<void>(`/api/accounts/${accountId}/transaction/${id}`, {
      id,
      accountId,
      amount: value,
      ...rest,
    });
  }

  deleteTransaction(accountId: string, id: string): Observable<void> {
    return this.http.delete<void>(`/api/accounts/${accountId}/transaction/${id}`);
  }

  getTransfer(accountId: string, id: string): Observable<AccountTransactionDTO> {
    return this.http.get<AccountTransactionDTO>(`/api/accounts/${accountId}/transfer/${id}`);
  }

  createTransfer(accountId: string, body: TransferBody): Observable<string> {
    return this.http.post<string>(`/api/accounts/${accountId}/transfer`, { accountId, ...body });
  }

  updateTransfer(accountId: string, id: string, body: TransferBody): Observable<void> {
    return this.http.put<void>(`/api/accounts/${accountId}/transfer/${id}`, {
      id,
      accountId,
      ...body,
    });
  }

  deleteTransfer(accountId: string, id: string): Observable<void> {
    return this.http.delete<void>(`/api/accounts/${accountId}/transfer/${id}`);
  }

  createCurrencyExchange(accountId: string, body: CurrencyExchangeBody): Observable<string> {
    return this.http.post<string>(`/api/accounts/${accountId}/currencyExchange`, {
      accountId,
      ...body,
    });
  }

  updateCurrencyExchange(
    accountId: string,
    id: string,
    body: CurrencyExchangeBody,
  ): Observable<void> {
    return this.http.put<void>(`/api/accounts/${accountId}/currencyExchange/${id}`, {
      id,
      accountId,
      ...body,
    });
  }
}
