import {
  Component,
  ElementRef,
  Injector,
  afterNextRender,
  inject,
  input,
  output,
  signal,
  viewChild,
  viewChildren,
} from '@angular/core';

export type MenuEntry =
  | { type: 'item'; id: string; label: string; disabledReason?: string | null }
  | { type: 'separator' };

let menuSeq = 0;

@Component({
  selector: 'app-menu-button',
  templateUrl: './menu-button.html',
  host: {
    'class': 'menu-host',
    '[class.menu-host--block]': 'block()',
    '(document:pointerdown)': 'onDocumentPointer($event)',
  },
})
export class MenuButton {
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  readonly items = input.required<readonly MenuEntry[]>();
  readonly triggerClass = input('btn btn-secondary');
  readonly align = input<'start' | 'end'>('start');
  readonly block = input(false);
  readonly disabled = input(false);
  readonly label = input<string | null>(null);
  readonly picked = output<string>();

  protected readonly id = `menu-${++menuSeq}`;
  protected readonly isOpen = signal(false);

  private readonly trigger = viewChild.required<ElementRef<HTMLButtonElement>>('trigger');
  private readonly itemEls = viewChildren<ElementRef<HTMLButtonElement>>('item');

  protected toggle(): void {
    if (this.isOpen()) this.close(false);
    else this.openAt('first');
  }

  protected onTriggerArrow(event: Event, at: 'first' | 'last'): void {
    event.preventDefault();
    this.openAt(at);
  }

  protected choose(entry: MenuEntry): void {
    if (entry.type === 'separator' || (entry.type === 'item' && entry.disabledReason)) return;
    this.close(true);
    this.picked.emit(entry.id);
  }

  protected onItemKey(event: KeyboardEvent): void {
    const items = this.itemEls().map((ref) => ref.nativeElement);
    const current = items.indexOf(event.target as HTMLButtonElement);
    const move = (index: number) => {
      event.preventDefault();
      items[(index + items.length) % items.length]?.focus();
    };
    switch (event.key) {
      case 'ArrowDown':
        return move(current + 1);
      case 'ArrowUp':
        return move(current - 1);
      case 'Home':
        return move(0);
      case 'End':
        return move(items.length - 1);
      case 'Escape':
      case 'Tab':
        event.preventDefault();
        event.stopPropagation();
        return this.close(true);
    }
    if (event.key.length === 1 && /\S/.test(event.key) && !event.ctrlKey && !event.metaKey) {
      const key = event.key.toLowerCase();
      const next = [...items.slice(current + 1), ...items.slice(0, current + 1)].find((el) =>
        labelOf(el).startsWith(key),
      );
      if (next) {
        event.preventDefault();
        next.focus();
      }
    }
  }

  protected onDocumentPointer(event: Event): void {
    if (this.isOpen() && !this.host.nativeElement.contains(event.target as Node)) {
      this.close(false);
    }
  }

  private openAt(at: 'first' | 'last'): void {
    if (this.disabled()) return;
    this.isOpen.set(true);
    afterNextRender(
      () => {
        const items = this.itemEls();
        items[at === 'first' ? 0 : items.length - 1]?.nativeElement.focus();
      },
      { injector: this.injector },
    );
  }

  private close(refocus: boolean): void {
    this.isOpen.set(false);
    if (refocus) this.trigger().nativeElement.focus();
  }
}

function labelOf(el: HTMLElement): string {
  const text = el.querySelector('.menu-label')?.textContent ?? '';
  return text.trim().toLowerCase();
}
