import { Directive, ElementRef, OnDestroy, afterNextRender, inject } from '@angular/core';

const FOCUSABLE =
  'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';
const FIELDS = 'input:not([disabled]), select:not([disabled]), textarea:not([disabled])';

@Directive({
  selector: '[appDialog]',
  host: {
    '(keydown.tab)': 'onTab($event, false)',
    '(keydown.shift.tab)': 'onTab($event, true)',
  },
})
export class DialogDirective implements OnDestroy {
  private readonly el = inject(ElementRef).nativeElement as HTMLElement;
  private readonly opener = document.activeElement as HTMLElement | null;

  constructor() {
    afterNextRender(() => {
      (
        this.el.querySelector<HTMLElement>(FIELDS) ?? this.el.querySelector<HTMLElement>(FOCUSABLE)
      )?.focus();
    });
  }

  ngOnDestroy(): void {
    this.opener?.focus?.();
  }

  onTab(event: Event, backward: boolean): void {
    const items = Array.from(this.el.querySelectorAll<HTMLElement>(FOCUSABLE)).filter(
      (el) => el.offsetParent !== null,
    );
    if (items.length === 0) return;
    const first = items[0];
    const last = items[items.length - 1];
    const active = document.activeElement;
    if (backward && active === first) {
      event.preventDefault();
      last.focus();
    } else if (!backward && active === last) {
      event.preventDefault();
      first.focus();
    }
  }
}
