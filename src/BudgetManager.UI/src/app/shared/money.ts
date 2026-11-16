import { currentLocale } from '../i18n/i18n';
import { Balance, Money } from './models';

const MINUS = '−';
const NBSP = ' ';
const numberFormats = new Map<string, Intl.NumberFormat>();

export const CURRENCIES = ['EUR', 'USD', 'GBP', 'PLN', 'CHF'];

export const ALL_CURRENCIES: readonly string[] = (() => {
  try {
    const codes = Intl.supportedValuesOf('currency');
    return codes.length > 0 ? [...codes].sort() : [...CURRENCIES].sort();
  } catch {
    return [...CURRENCIES].sort();
  }
})();

export interface CurrencyOptions {
  favorites: string[];
  others: string[];
}

export function currencyOptions(
  favorites: readonly string[],
  extra: readonly string[] = [],
  all: readonly string[] = ALL_CURRENCIES,
): CurrencyOptions {
  const favs = [...new Set(favorites.filter(Boolean))];
  const others = [...new Set([...all, ...extra.filter(Boolean)])]
    .filter((c) => !favs.includes(c))
    .sort();
  return { favorites: favs, others };
}

export function formatAmount(amount: number): string {
  // Round first so a value that rounds to zero (e.g. -0.004) never keeps a spurious minus sign.
  const rounded = Math.round(amount * 100) / 100;
  const sign = rounded < 0 ? MINUS : '';
  return `${sign}${numberFormat(currentLocale()).format(Math.abs(rounded))}`;
}

export function formatMoney(amount: number, currency: string): string {
  return `${formatAmount(amount)}${NBSP}${currency}`;
}

export function signedMoney(amount: number, currency: string): string {
  const rounded = Math.round(amount * 100) / 100;
  const sign = rounded > 0 ? '+' : '';
  return `${sign}${formatMoney(rounded, currency)}`;
}

export function moneyParts(
  amount: number,
  currency: string,
): { number: string; currency: string; negative: boolean } {
  return {
    number: formatAmount(amount),
    currency,
    negative: Math.round(amount * 100) < 0,
  };
}

export function formatMoneyValue(money: Money): string {
  return formatMoney(money.amount, money.currency);
}

export function formatNumber(value: number, maxDigits = 2, minDigits = 0, grouping = true): string {
  return new Intl.NumberFormat(currentLocale(), {
    minimumFractionDigits: minDigits,
    maximumFractionDigits: maxDigits,
    useGrouping: grouping,
  }).format(value);
}

export interface MoneyLine {
  currency: string;
  amount: number;
  number: string;
  text: string;
}

export function balanceLines(balance: Balance | null | undefined): MoneyLine[] {
  return Object.entries(balance ?? {})
    .filter(([, amount]) => amount !== 0)
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([currency, amount]) => moneyLine(currency, amount));
}

export function sumBalances(balances: readonly (Balance | null | undefined)[]): Balance {
  const cents: Record<string, number> = {};
  for (const balance of balances) {
    for (const [currency, amount] of Object.entries(balance ?? {})) {
      cents[currency] = (cents[currency] ?? 0) + Math.round(amount * 100);
    }
  }
  return Object.fromEntries(Object.entries(cents).map(([currency, c]) => [currency, c / 100]));
}

export function amountClass(amount: number): string {
  if (amount === 0) return 'amount--neutral';
  return amount > 0 ? 'amount--in' : 'amount--out';
}

export function hasAtMostTwoDecimals(value: number | null): boolean {
  return value === null || (Number.isFinite(value) && Number(value.toFixed(2)) === value);
}

function numberFormat(locale: string): Intl.NumberFormat {
  let format = numberFormats.get(locale);
  if (!format) {
    format = new Intl.NumberFormat(locale, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    numberFormats.set(locale, format);
  }
  return format;
}

export function moneyLine(currency: string, amount: number): MoneyLine {
  return { currency, amount, number: formatAmount(amount), text: formatMoney(amount, currency) };
}
