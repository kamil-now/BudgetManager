import { DestroyRef, Injectable, Signal, inject, signal } from '@angular/core';

function media(query: string): Signal<boolean> {
  const state = signal(false);
  if (typeof matchMedia !== 'function') return state.asReadonly();
  const list = matchMedia(query);
  state.set(list.matches);
  const onChange = (event: MediaQueryListEvent) => state.set(event.matches);
  list.addEventListener('change', onChange);
  inject(DestroyRef).onDestroy(() => list.removeEventListener('change', onChange));
  return state.asReadonly();
}

@Injectable({ providedIn: 'root' })
export class Viewport {
  /** Narrower than the 860px drawer breakpoint. */
  readonly phone = media('(max-width: 859.98px)');
  readonly wide = media('(min-width: 1100px)');
}
