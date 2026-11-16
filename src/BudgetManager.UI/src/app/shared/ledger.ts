import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, Subscription, finalize, map, shareReplay } from 'rxjs';
import { FeedFilters, LedgerFeedPageDTO } from './feed-models';
import {
  CreateLedgerAllocationTemplateBody,
  CreateLedgerCommand,
  LedgerAllocationTemplateDTO,
  LedgerDTO,
  LedgerListItem,
  LedgerStatisticsDTO,
  StatisticsFilters,
  UpdateLedgerAllocationTemplateBody,
} from './models';

@Injectable({ providedIn: 'root' })
export class LedgerService {
  private readonly http = inject(HttpClient);
  private readonly storageKey = 'budget-manager.ledgerId';

  private readonly _currentLedgerId = signal<string | null>(this.load());
  readonly currentLedgerId = this._currentLedgerId.asReadonly();

  readonly currentLedgerName = signal<string | null>(null);

  private readonly _reloadTick = signal(0);
  readonly reloadTick = this._reloadTick.asReadonly();

  private readonly _ledgerList = signal<LedgerListItem[] | null>(null);
  readonly ledgerList = this._ledgerList.asReadonly();
  private listRequest?: Subscription;

  requestReload(): void {
    this._reloadTick.update((n) => n + 1);
  }

  refreshLedgers(): void {
    this.listRequest?.unsubscribe();
    this.listRequest = this.listLedgers().subscribe({
      next: (list) => this._ledgerList.set(list),
      error: () => undefined,
    });
  }

  setCurrentLedgerId(id: string): void {
    this._currentLedgerId.set(id);
    try {
      localStorage.setItem(this.storageKey, id);
    } catch {
      // storage unavailable; the id stays in memory only
    }
  }

  clearCurrentLedger(): void {
    this._currentLedgerId.set(null);
    this.currentLedgerName.set(null);
    this.listRequest?.unsubscribe();
    this._ledgerList.set(null);
    try {
      localStorage.removeItem(this.storageKey);
    } catch {
      // nothing to clear
    }
  }

  private readonly ledgerRequests = new Map<string, Observable<LedgerDTO>>();

  listLedgers(): Observable<LedgerListItem[]> {
    return this.http.get<LedgerListItem[]>('/api/ledgers');
  }

  ensureLedgerSelected(): Observable<string | null> {
    const current = this._currentLedgerId();
    return this.listLedgers().pipe(
      map((ledgers) => {
        if (ledgers.length === 0) {
          this.clearCurrentLedger();
          this._ledgerList.set(ledgers);
          return null;
        }
        this._ledgerList.set(ledgers);
        const chosen = ledgers.find((l) => l.id === current) ?? ledgers[0];
        this.setCurrentLedgerId(chosen.id);
        this.currentLedgerName.set(chosen.name);
        return chosen.id;
      }),
    );
  }

  getLedger(id: string): Observable<LedgerDTO> {
    const inFlight = this.ledgerRequests.get(id);
    if (inFlight) return inFlight;
    const request = this.http.get<LedgerDTO>(`/api/ledgers/${id}`).pipe(
      finalize(() => this.ledgerRequests.delete(id)),
      shareReplay({ bufferSize: 1, refCount: true }),
    );
    this.ledgerRequests.set(id, request);
    return request;
  }

  getFeed(
    id: string,
    filters: FeedFilters,
    offset: number,
    limit: number,
  ): Observable<LedgerFeedPageDTO> {
    const { types, ...rest } = filters;
    let params = toParams({ ...rest, offset, limit });
    for (const type of types ?? []) params = params.append('types', type);
    return this.http.get<LedgerFeedPageDTO>(`/api/ledgers/${id}/feed`, { params });
  }

  getStatistics(id: string, filters?: StatisticsFilters): Observable<LedgerStatisticsDTO> {
    return this.http.get<LedgerStatisticsDTO>(`/api/ledgers/${id}/statistics`, {
      params: toParams(filters),
    });
  }

  createLedger(command: CreateLedgerCommand): Observable<string> {
    return this.http.post<string>('/api/ledgers', command);
  }

  getAllocationTemplates(ledgerId: string): Observable<LedgerAllocationTemplateDTO[]> {
    return this.http.get<LedgerAllocationTemplateDTO[]>(`/api/ledgers/${ledgerId}/allocationTemplates`);
  }

  createAllocationTemplate(ledgerId: string, body: CreateLedgerAllocationTemplateBody): Observable<string> {
    return this.http.post<string>(`/api/ledgers/${ledgerId}/allocationTemplates`, { ledgerId, ...body });
  }

  updateAllocationTemplate(
    ledgerId: string,
    templateId: string,
    body: UpdateLedgerAllocationTemplateBody,
  ): Observable<void> {
    return this.http.put<void>(`/api/ledgers/${ledgerId}/allocationTemplates/${templateId}`, {
      ledgerId,
      templateId,
      ...body,
    });
  }

  deleteAllocationTemplate(ledgerId: string, templateId: string): Observable<void> {
    return this.http.delete<void>(`/api/ledgers/${ledgerId}/allocationTemplates/${templateId}`);
  }

  private load(): string | null {
    try {
      return localStorage.getItem(this.storageKey);
    } catch {
      return null;
    }
  }
}

function toParams(filters?: object): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(filters ?? {})) {
    if (value !== undefined && value !== null && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}
