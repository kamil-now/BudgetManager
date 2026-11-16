import { Injectable, signal } from '@angular/core';
import { TranslationKey, en } from './en';
import { pl } from './pl';

export type { TranslationKey } from './en';

export type Language = 'en' | 'pl';

export const LANGUAGES: readonly { value: Language; name: string }[] = [
  { value: 'en', name: 'English' },
  { value: 'pl', name: 'Polski' },
];

export type TranslationParams = Record<string, string | number>;

const dictionaries: Record<Language, Record<TranslationKey, string>> = { en, pl };

const locales: Record<Language, string> = { en: 'en-GB', pl: 'pl-PL' };

const pluralForms: Record<Language, readonly Intl.LDMLPluralRule[]> = {
  en: ['one', 'other'],
  pl: ['one', 'few', 'many', 'other'],
};

const pluralRules = new Map<Language, Intl.PluralRules>();

const _language = signal<Language>('en');

export const currentLanguage = _language.asReadonly();

export function isLanguage(value: unknown): value is Language {
  return value === 'en' || value === 'pl';
}

export function setLanguage(language: Language): void {
  _language.set(language);
  if (typeof document !== 'undefined') document.documentElement.lang = language;
}

export function currentLocale(language: Language = _language()): string {
  return locales[language];
}

export function translate(
  key: TranslationKey,
  params?: TranslationParams,
  language: Language = _language(),
): string {
  let text = dictionaries[language][key] ?? en[key] ?? key;
  const count = params?.['count'];
  if (typeof count === 'number' && text.includes('|')) text = pluralForm(text, count, language);
  if (!params) return text;
  return text.replace(/\{(\w+)\}/g, (match, name: string) =>
    name in params ? String(params[name]) : match,
  );
}

function pluralForm(text: string, count: number, language: Language): string {
  const forms = text.split('|');
  let rules = pluralRules.get(language);
  if (!rules) {
    rules = new Intl.PluralRules(locales[language]);
    pluralRules.set(language, rules);
  }
  const index = pluralForms[language].indexOf(rules.select(count));
  return forms[index] ?? forms[forms.length - 1];
}

@Injectable({ providedIn: 'root' })
export class I18nService {
  readonly language = currentLanguage;

  setLanguage(language: Language): void {
    setLanguage(language);
  }

  t(key: TranslationKey, params?: TranslationParams): string {
    return translate(key, params);
  }

  locale(): string {
    return currentLocale();
  }
}
