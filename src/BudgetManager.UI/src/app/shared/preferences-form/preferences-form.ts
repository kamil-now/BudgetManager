import { Component, computed, effect, input, model, signal, untracked } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LANGUAGES, TranslationKey, translate } from '../../i18n/i18n';
import { TranslatePipe } from '../../i18n/t.pipe';
import { DATE_FORMATS, formatDate } from '../date';
import { currencyOptions } from '../money';
import { MAX_FAVORITE_CURRENCIES, UserPreferences } from '../preferences';
import { THEMES, ThemePreference } from '../theme';

const themeLabels: Record<ThemePreference, TranslationKey> = {
  system: 'theme.system',
  light: 'theme.light',
  dark: 'theme.dark',
};

const SAMPLE_DATE = '2026-11-14T00:00:00.000Z';

@Component({
  selector: 'app-preferences-form',
  imports: [FormsModule, TranslatePipe],
  templateUrl: './preferences-form.html',
  styleUrl: './preferences-form.scss',
  host: { '[class.plain]': 'plain()' },
})
export class PreferencesForm {
  readonly value = model.required<UserPreferences>();
  readonly plain = input(false);

  protected readonly maxFavorites = MAX_FAVORITE_CURRENCIES;
  protected readonly themes = THEMES.map((option) => ({ value: option, label: themeLabels[option] }));
  protected readonly dateFormats = DATE_FORMATS.map((option) => ({
    value: option,
    sample: formatDate(SAMPLE_DATE, option),
  }));
  protected readonly languages = LANGUAGES;

  protected readonly favoriteToAdd = signal('');
  protected readonly error = signal('');

  protected readonly currencyOptions = computed(() =>
    currencyOptions(this.value().favoriteCurrencies, [this.value().defaultCurrency]),
  );
  protected readonly addable = computed(
    () => currencyOptions(this.value().favoriteCurrencies).others,
  );
  protected readonly favoritesFull = computed(
    () => this.value().favoriteCurrencies.length >= MAX_FAVORITE_CURRENCIES,
  );

  constructor() {
    effect(() => {
      const addable = this.addable();
      untracked(() => {
        if (!addable.includes(this.favoriteToAdd())) this.favoriteToAdd.set(addable[0] ?? '');
      });
    });
  }

  protected update<K extends keyof UserPreferences>(key: K, next: UserPreferences[K]): void {
    this.error.set('');
    this.value.set({ ...this.value(), [key]: next });
  }

  protected addFavorite(): void {
    const currency = this.favoriteToAdd();
    const favorites = this.value().favoriteCurrencies;
    if (!currency || favorites.includes(currency)) return;
    if (this.favoritesFull()) {
      this.error.set(translate('settings.favoritesFull', { max: MAX_FAVORITE_CURRENCIES }));
      return;
    }
    this.update('favoriteCurrencies', [...favorites, currency]);
  }

  protected removeFavorite(currency: string): void {
    this.update(
      'favoriteCurrencies',
      this.value().favoriteCurrencies.filter((c) => c !== currency),
    );
  }
}
