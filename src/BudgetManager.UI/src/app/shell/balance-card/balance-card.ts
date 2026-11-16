import { NgTemplateOutlet } from '@angular/common';
import { Component, ElementRef, computed, inject, input, signal, viewChild } from '@angular/core';
import { TranslatePipe } from '../../i18n/t.pipe';
import { CurrencyTotal } from '../../dashboard/dashboard-data';
import { formatAmount, formatMoney } from '../../shared/money';
import { Viewport } from '../../shared/viewport';

let balanceSeq = 0;

@Component({
  selector: 'app-balance-card',
  imports: [NgTemplateOutlet, TranslatePipe],
  templateUrl: './balance-card.html',
  styleUrl: './balance-card.scss',
  host: {
    '(document:pointerdown)': 'onDocumentPointer($event)',
    '(document:keydown.escape)': 'onEscape()',
  },
})
export class BalanceCard {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly viewport = inject(Viewport);

  readonly totals = input.required<readonly CurrencyTotal[]>();
  readonly layout = input<'bar' | 'page'>('bar');
  readonly name = input<string | null>(null);

  protected readonly formatAmount = formatAmount;
  protected readonly formatMoney = formatMoney;
  protected readonly popoverId = `balance-more-${++balanceSeq}`;
  protected readonly popoverOpen = signal(false);
  protected readonly shown = computed(() => this.totals().slice(0, this.inline()));
  protected readonly rest = computed(() => this.totals().slice(this.inline()));

  private readonly moreButton = viewChild<ElementRef<HTMLButtonElement>>('more');

  private inline(): number {
    return this.viewport.wide() ? 3 : 2;
  }

  protected togglePopover(): void {
    this.popoverOpen.update((open) => !open);
  }

  protected onDocumentPointer(event: Event): void {
    if (this.popoverOpen() && !this.host.nativeElement.contains(event.target as Node)) {
      this.closePopover(false);
    }
  }

  protected onEscape(): void {
    if (this.popoverOpen()) this.closePopover(true);
  }

  private closePopover(refocus: boolean): void {
    this.popoverOpen.set(false);
    if (refocus) this.moreButton()?.nativeElement.focus();
  }
}
