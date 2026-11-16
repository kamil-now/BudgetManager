import { Injectable, WritableSignal, signal } from '@angular/core';

const MAX_SET_SIZE = 200;

@Injectable({ providedIn: 'root' })
export class UiState {
  private readonly prefix = 'budget-manager.ui.';
  private readonly cache = new Map<string, WritableSignal<unknown>>();

  persisted<T>(key: string, fallback: T, parse?: (raw: unknown) => T | undefined): WritableSignal<T> {
    const cached = this.cache.get(key);
    if (cached) return cached as WritableSignal<T>;
    const raw = this.read<unknown>(key);
    const stored = raw === null ? undefined : parse ? parse(raw) : (raw as T);
    const state = signal<T>(stored ?? fallback);
    // Stored synchronously on every change, so nothing depends on when an effect would flush.
    const set = state.set.bind(state);
    state.set = (value: T) => {
      set(value);
      this.write(key, value);
    };
    state.update = (update: (value: T) => T) => state.set(update(state()));
    this.cache.set(key, state);
    return state;
  }

  persistedSet(key: string): WritableSignal<ReadonlySet<string>> {
    const state = this.persisted<ReadonlySet<string>>(key, new Set(), (raw) =>
      Array.isArray(raw) ? new Set(raw.filter((v) => typeof v === 'string')) : undefined,
    );
    return state;
  }

  toggle(state: WritableSignal<ReadonlySet<string>>, id: string): void {
    state.update((ids) => {
      const next = new Set(ids);
      if (!next.delete(id)) next.add(id);
      return new Set([...next].slice(-MAX_SET_SIZE));
    });
  }

  read<T>(key: string): T | null {
    try {
      const text = localStorage.getItem(this.prefix + key);
      return text === null ? null : (JSON.parse(text) as T);
    } catch {
      return null;
    }
  }

  write(key: string, value: unknown): void {
    try {
      const json = JSON.stringify(value instanceof Set ? [...value] : value);
      localStorage.setItem(this.prefix + key, json);
    } catch {
      // storage unavailable; the state stays in memory only
    }
  }
}
