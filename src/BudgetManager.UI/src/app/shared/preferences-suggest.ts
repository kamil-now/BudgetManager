import { Language, isLanguage } from '../i18n/i18n';
import { DEFAULT_DATE_FORMAT, DateFormat } from './date';
import { ALL_CURRENCIES } from './money';
import { DEFAULT_PREFERENCES, MAX_FAVORITE_CURRENCIES, UserPreferences } from './preferences';
import { ThemePreference } from './theme';

const EUROZONE = [
  'AT', 'BE', 'CY', 'DE', 'EE', 'ES', 'FI', 'FR', 'GR', 'HR',
  'IE', 'IT', 'LT', 'LU', 'LV', 'MT', 'NL', 'PT', 'SI', 'SK',
];

const REGION_CURRENCIES: Record<string, string> = {
  PL: 'PLN',
  US: 'USD',
  GB: 'GBP',
  CH: 'CHF',
  CZ: 'CZK',
  SE: 'SEK',
  NO: 'NOK',
  DK: 'DKK',
  HU: 'HUF',
  CA: 'CAD',
  AU: 'AUD',
  JP: 'JPY',
  ...Object.fromEntries(EUROZONE.map((region) => [region, 'EUR'])),
};

export function languageOf(locales: readonly string[]): Language {
  for (const locale of locales) {
    const language = locale.split('-')[0].toLowerCase();
    if (isLanguage(language)) return language;
  }
  return 'en';
}

export function dateFormatOf(locale: string): DateFormat {
  try {
    const parts = new Intl.DateTimeFormat(locale, {
      year: 'numeric',
      month: 'numeric',
      day: 'numeric',
      timeZone: 'UTC',
    }).formatToParts(new Date(Date.UTC(2026, 10, 25)));
    const order = parts
      .filter((p) => p.type === 'year' || p.type === 'month' || p.type === 'day')
      .map((p) => p.type[0])
      .join('');
    const separator = parts.find((p) => p.type === 'literal')?.value ?? '';
    if (order === 'ymd') return 'yyyy-MM-dd';
    if (order === 'dmy') return separator.includes('/') ? 'dd/MM/yyyy' : 'dd.MM.yyyy';
    if (order === 'mdy' && separator.includes('/')) return 'MM/dd/yyyy';
    return DEFAULT_DATE_FORMAT;
  } catch {
    return DEFAULT_DATE_FORMAT;
  }
}

export function currencyOf(locale: string): string {
  try {
    const region = new Intl.Locale(locale).maximize().region;
    const currency = region ? REGION_CURRENCIES[region] : undefined;
    return currency && ALL_CURRENCIES.includes(currency) ? currency : DEFAULT_PREFERENCES.defaultCurrency;
  } catch {
    return DEFAULT_PREFERENCES.defaultCurrency;
  }
}

export function favoritesFor(currency: string): string[] {
  return [...new Set([currency, ...DEFAULT_PREFERENCES.favoriteCurrencies])].slice(
    0,
    MAX_FAVORITE_CURRENCIES,
  );
}

export function suggestPreferences(
  locales: readonly string[],
  theme: ThemePreference,
): UserPreferences {
  const first = locales[0];
  if (!first) return { ...DEFAULT_PREFERENCES, theme, favoriteCurrencies: favoritesFor('EUR') };
  const defaultCurrency = currencyOf(first);
  return {
    theme,
    dateFormat: dateFormatOf(first),
    defaultCurrency,
    favoriteCurrencies: favoritesFor(defaultCurrency),
    language: languageOf(locales),
  };
}

export function followDefaultCurrency(
  prev: UserPreferences,
  next: UserPreferences,
  favoritesTouched: boolean,
): UserPreferences {
  if (favoritesTouched || prev.defaultCurrency === next.defaultCurrency) return next;
  return { ...next, favoriteCurrencies: favoritesFor(next.defaultCurrency) };
}
