import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { httpErrorMessage } from '../shared/http-error';
import { PreferencesForm } from '../shared/preferences-form/preferences-form';
import { DEFAULT_PREFERENCES, PreferencesService, UserPreferences } from '../shared/preferences';
import { DeleteAccount } from './delete-account/delete-account';

type LoadState = 'loading' | 'ready' | 'error';

@Component({
  selector: 'app-settings',
  imports: [DeleteAccount, FormsModule, PreferencesForm, TranslatePipe],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
})
export class Settings {
  private readonly prefs = inject(PreferencesService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly state = signal<LoadState>('loading');
  protected readonly submitting = signal(false);
  protected readonly error = signal('');
  protected readonly saved = signal(false);
  protected readonly draft = signal<UserPreferences>(DEFAULT_PREFERENCES);
  protected readonly deletingAccount = signal(false);

  constructor() {
    if (this.prefs.savedNotice()) {
      this.saved.set(true);
      this.prefs.savedNotice.set(false);
    }
    this.load();
  }

  protected load(): void {
    this.state.set('loading');
    this.prefs
      .load()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (preferences) => {
          this.draft.set(preferences);
          this.state.set('ready');
        },
        error: () => this.state.set('error'),
      });
  }

  protected edit(preferences: UserPreferences): void {
    this.draft.set(preferences);
    this.changed();
  }

  protected changed(): void {
    this.saved.set(false);
    this.error.set('');
  }

  protected save(): void {
    if (this.submitting() || this.state() !== 'ready') return;
    const preferences = this.draft();
    const languageChanges = preferences.language !== this.prefs.language();
    this.error.set('');
    this.saved.set(false);
    this.submitting.set(true);
    this.prefs
      .save(preferences)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.submitting.set(false);
          if (languageChanges) this.prefs.savedNotice.set(true);
          else this.saved.set(true);
        },
        error: (response: HttpErrorResponse) => {
          this.submitting.set(false);
          this.error.set(
            httpErrorMessage(response, {
              invalid: translate('settings.invalid'),
              fallback: translate('settings.saveError'),
            }),
          );
        },
      });
  }
}
