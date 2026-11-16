import { signal } from '@angular/core';

export type DateFormat = 'yyyy-MM-dd' | 'dd.MM.yyyy' | 'dd/MM/yyyy' | 'MM/dd/yyyy';

export const DATE_FORMATS: readonly DateFormat[] = ['yyyy-MM-dd', 'dd.MM.yyyy', 'dd/MM/yyyy', 'MM/dd/yyyy'];

export const DEFAULT_DATE_FORMAT: DateFormat = 'yyyy-MM-dd';

export function isDateFormat(value: unknown): value is DateFormat {
  return DATE_FORMATS.includes(value as DateFormat);
}

const _dateFormat = signal<DateFormat>(DEFAULT_DATE_FORMAT);

export const currentDateFormat = _dateFormat.asReadonly();

export function setDateFormat(format: DateFormat): void {
  _dateFormat.set(format);
}

export function formatDate(iso: string, format: DateFormat = _dateFormat()): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime()) || date.getUTCFullYear() < 1000) return '—';
  const parts: Record<string, string> = {
    yyyy: String(date.getUTCFullYear()),
    MM: String(date.getUTCMonth() + 1).padStart(2, '0'),
    dd: String(date.getUTCDate()).padStart(2, '0'),
  };
  return format.replace(/yyyy|MM|dd/g, (part) => parts[part]);
}

export function calendarDate(value: string): string {
  return new Date(value).toISOString()
    .slice(0, 10);
}

export function today(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${now.getFullYear()}-${month}-${day}`;
}

/**
 * An `<input type="date">` value (`YYYY-MM-DD`) as the ISO-8601 instant at the start of that calendar
 * day in UTC. Stored dates are UTC calendar dates and are displayed in UTC, so the chosen day never
 * shifts with the user's time zone.
 */
export function toIso(date: string): string {
  return `${date}T00:00:00.000Z`;
}

export function dayEndIso(date: string): string {
  return `${date}T23:59:59.999Z`;
}
