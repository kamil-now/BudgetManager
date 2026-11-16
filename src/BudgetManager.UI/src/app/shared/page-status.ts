import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../i18n/t.pipe';

export type PageState = 'loading' | 'ready' | 'error' | 'no-ledger';

@Component({
  selector: 'app-page-status',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './page-status.html',
})
export class PageStatus {
  readonly state = input.required<PageState>();
  readonly loadingText = input('');
  readonly errorText = input('');
  readonly retry = output<void>();
}
