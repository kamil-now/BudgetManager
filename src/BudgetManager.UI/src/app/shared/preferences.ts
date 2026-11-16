import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { AuthService } from '../auth/auth';
import { Language, isLanguage, setLanguage } from '../i18n/i18n';
import { DEFAULT_DATE_FORMAT, DateFormat, isDateFormat, setDateFormat } from './date';
import { currencyOptions } from './money';
import { ThemePreference, ThemeService, isThemePreference, nextTheme } from './theme';

export interface UserPreferences {
  theme: ThemePreference;
  dateFormat: DateFormat;
  defaultCurrency: string;
  favoriteCurrencies: string[];
  language: Language;
}

/** Matches Constants.MaxFavoriteCurrencies in the API. */
export const MAX_FAVORITE_CURRENCIES = 10;

export const DEFAULT_PREFERENCES: UserPreferences = {
  theme: 'system',
  dateFormat: DEFAULT_DATE_FORMAT,
  defaultCurrency: 'EUR',
  favoriteCurrencies: ['EUR', 'USD', 'PLN'],
  language: 'en',
};

const isCurrency = (value: unknown): value is string =>
  typeof value === 'string' && /^[A-Z]{3}$/.test(value);

export function normalizePreferences(value: unknown): UserPreferences {
  const raw = (value ?? {}) as Partial<Record<keyof UserPreferences, unknown>>;
  const favorites = Array.isArray(raw.favoriteCurrencies)
    ? [...new Set(raw.favoriteCurrencies.filter(isCurrency))].slice(0, MAX_FAVORITE_CURRENCIES)
    : DEFAULT_PREFERENCES.favoriteCurrencies;
  return {
    theme: isThemePreference(raw.theme) ? raw.theme : DEFAULT_PREFERENCES.theme,
    dateFormat: isDateFormat(raw.dateFormat) ? raw.dateFormat : DEFAULT_PREFERENCES.dateFormat,
    defaultCurrency: isCurrency(raw.defaultCurrency)
      ? raw.defaultCurrency
      : DEFAULT_PREFERENCES.defaultCurrency,
    favoriteCurrencies: favorites,
    language: isLanguage(raw.language) ? raw.language : DEFAULT_PREFERENCES.language,
  };
}

@Injectable({ providedIn: 'root' })
export class PreferencesService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly themes = inject(ThemeService);
  private readonly storageKey = 'budget-manager.preferences';
  private readonly url = '/api/users/me/preferences';

  private readonly _preferences = signal<UserPreferences>(this.cached());
  readonly preferences = this._preferences.asReadonly();
  readonly theme = computed(() => this._preferences().theme);
  readonly dateFormat = computed(() => this._preferences().dateFormat);
  readonly language = computed(() => this._preferences().language);
  readonly defaultCurrency = computed(() => this._preferences().defaultCurrency);
  readonly favoriteCurrencies = computed(() => this._preferences().favoriteCurrencies);
  readonly currencyOptions = computed(() => currencyOptions(this.favoriteCurrencies()));

  readonly savedNotice = signal(false);

  constructor() {
    this.apply(this._preferences());
  }

  load(): Observable<UserPreferences> {
    return this.http.get<unknown>(this.url).pipe(
      map(normalizePreferences),
      tap((preferences) => this.set(preferences)),
    );
  }

  refresh(): void {
    if (!this.auth.isAuthenticated) return;
    this.load().subscribe({ error: () => undefined });
  }

  save(preferences: UserPreferences): Observable<void> {
    const body = normalizePreferences(preferences);
    return this.http.put<void>(this.url, body).pipe(tap(() => this.set(body)));
  }

  preview(preferences: UserPreferences): void {
    this.apply(normalizePreferences(preferences));
  }

  adopt(preferences: UserPreferences): void {
    this.set(normalizePreferences(preferences));
  }

  forget(): void {
    try {
      localStorage.removeItem(this.storageKey);
    } catch {
      // storage unavailable; nothing to forget
    }
  }

  cycleTheme(): void {
    const next = { ...this._preferences(), theme: nextTheme(this._preferences().theme) };
    this.set(next);
    if (this.auth.isAuthenticated) this.http.put<void>(this.url, next).subscribe({ error: () => undefined });
  }

  private set(preferences: UserPreferences): void {
    this._preferences.set(preferences);
    this.apply(preferences);
    try {
      localStorage.setItem(this.storageKey, JSON.stringify(preferences));
    } catch {
      // storage unavailable; the server copy still applies after sign-in
    }
  }

  private apply(preferences: UserPreferences): void {
    this.themes.set(preferences.theme);
    setDateFormat(preferences.dateFormat);
    setLanguage(preferences.language);
  }

  private cached(): UserPreferences {
    let stored: unknown;
    try {
      stored = JSON.parse(localStorage.getItem(this.storageKey) ?? 'null');
    } catch {
      stored = null;
    }
    const preferences = normalizePreferences(stored);
    return { ...preferences, theme: this.themes.preference() };
  }
}
