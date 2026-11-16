import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AllocateFundsBody,
  AllocationPreviewRequest,
  SplitPreview,
  UpdateAllocationBody,
} from './allocation-models';

@Injectable({ providedIn: 'root' })
export class AllocationApi {
  private readonly http = inject(HttpClient);

  preview(ledgerId: string, body: AllocationPreviewRequest): Observable<SplitPreview> {
    return this.http.post<SplitPreview>(`/api/ledgers/${ledgerId}/allocation-preview`, body);
  }

  allocate(ledgerId: string, body: AllocateFundsBody): Observable<string> {
    return this.http.post<string>(`/api/ledgers/${ledgerId}/allocations`, {
      ledgerId,
      ...body,
    });
  }

  updateAllocation(ledgerId: string, id: string, body: UpdateAllocationBody): Observable<void> {
    return this.http.put<void>(`/api/ledgers/${ledgerId}/allocations/${id}`, {
      ledgerId,
      id,
      ...body,
    });
  }

  deleteAllocation(ledgerId: string, id: string): Observable<void> {
    return this.http.delete<void>(`/api/ledgers/${ledgerId}/allocations/${id}`);
  }
}
