import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

export type ThemePreference = 'system' | Theme;

export const THEMES: readonly ThemePreference[] = ['system', 'light', 'dark'];

export function isThemePreference(value: unknown): value is ThemePreference {
  return THEMES.includes(value as ThemePreference);
}

export function nextTheme(theme: ThemePreference): ThemePreference {
  return THEMES[(THEMES.indexOf(theme) + 1) % THEMES.length];
}

const darkQuery = '(prefers-color-scheme: dark)';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'budget-manager.theme';
  private readonly _preference = signal<ThemePreference>(this.stored() ?? 'system');
  private readonly systemDark = signal(matches(darkQuery));

  readonly preference = this._preference.asReadonly();
  readonly theme = computed<Theme>(() => {
    const preference = this._preference();
    if (preference !== 'system') return preference;
    return this.systemDark() ? 'dark' : 'light';
  });

  constructor() {
    this.applyToPage(this._preference());
    if (typeof matchMedia !== 'function') return;
    const media = matchMedia(darkQuery);
    const update = () => this.systemDark.set(media.matches);
    media.addEventListener('change', update);
    inject(DestroyRef).onDestroy(() => media.removeEventListener('change', update));
  }

  set(preference: ThemePreference): void {
    this._preference.set(preference);
    this.applyToPage(preference);
    try {
      localStorage.setItem(this.storageKey, preference);
    } catch {
      // storage unavailable; the choice just doesn't persist locally
    }
  }

  private applyToPage(preference: ThemePreference): void {
    if (typeof document === 'undefined') return;
    if (preference === 'system') delete document.documentElement.dataset['theme'];
    else document.documentElement.dataset['theme'] = preference;
  }

  private stored(): ThemePreference | null {
    try {
      const stored = localStorage.getItem(this.storageKey);
      return isThemePreference(stored) ? stored : null;
    } catch {
      return null;
    }
  }
}

function matches(query: string): boolean {
  return typeof matchMedia === 'function' && matchMedia(query).matches;
}
