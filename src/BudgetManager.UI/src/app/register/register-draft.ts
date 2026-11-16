import { Injectable, inject, signal } from '@angular/core';
import { UserPreferences } from '../shared/preferences';
import { suggestPreferences } from '../shared/preferences-suggest';
import { ThemeService } from '../shared/theme';

/**
 * What the registration form holds between its two steps. It lives in the root injector because
 * changing the language on step 2 re-creates the page (the router outlet is keyed on the language),
 * which would otherwise lose everything typed on step 1.
 */
@Injectable({ providedIn: 'root' })
export class RegisterDraft {
  private readonly themes = inject(ThemeService);

  readonly step = signal<1 | 2>(1);
  readonly name = signal('');
  readonly email = signal('');
  readonly password = signal('');
  readonly confirm = signal('');
  readonly error = signal('');
  readonly favoritesTouched = signal(false);
  readonly preferences = signal<UserPreferences>(this.suggest());

  reset(): void {
    this.step.set(1);
    this.name.set('');
    this.email.set('');
    this.password.set('');
    this.confirm.set('');
    this.error.set('');
    this.favoritesTouched.set(false);
    this.preferences.set(this.suggest());
  }

  private suggest(): UserPreferences {
    return suggestPreferences(browserLocales(), this.themes.preference());
  }
}

function browserLocales(): readonly string[] {
  if (typeof navigator === 'undefined') return [];
  if (navigator.languages?.length) return navigator.languages;
  return navigator.language ? [navigator.language] : [];
}
