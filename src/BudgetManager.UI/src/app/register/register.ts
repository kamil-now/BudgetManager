import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService } from '../auth/auth';
import { translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { LedgerService } from '../shared/ledger';
import { httpErrorMessage } from '../shared/http-error';
import { PreferencesService, UserPreferences } from '../shared/preferences';
import { followDefaultCurrency } from '../shared/preferences-suggest';
import { PreferencesForm } from '../shared/preferences-form/preferences-form';
import { RegisterDraft } from './register-draft';

// matches Constants.MinPasswordLength in the API
const minPasswordLength = 8;
const emailPattern = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

@Component({
  selector: 'app-register',
  imports: [FormsModule, PreferencesForm, RouterLink, TranslatePipe],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly ledgers = inject(LedgerService);
  private readonly prefs = inject(PreferencesService);
  protected readonly draft = inject(RegisterDraft);

  protected readonly minPasswordLength = minPasswordLength;
  protected readonly showPassword = signal(false);
  protected readonly showConfirm = signal(false);
  protected readonly submitting = signal(false);

  constructor() {
    this.prefs.preview(this.draft.preferences());
  }

  protected togglePassword(): void {
    this.showPassword.update((shown) => !shown);
  }

  protected toggleConfirm(): void {
    this.showConfirm.update((shown) => !shown);
  }

  protected toggleTheme(): void {
    this.prefs.cycleTheme();
    this.draft.preferences.update((p) => ({ ...p, theme: this.prefs.theme() }));
  }

  protected back(): void {
    this.draft.error.set('');
    this.draft.step.set(1);
  }

  protected next(): void {
    const draft = this.draft;
    draft.name.update((name) => name.trim());
    draft.email.update((email) => email.trim());
    if (!emailPattern.test(draft.email())) {
      draft.error.set(translate('register.emailInvalid'));
    } else if (draft.password().length < minPasswordLength) {
      draft.error.set(translate('register.passwordLength', { n: minPasswordLength }));
    } else if (draft.password() !== draft.confirm()) {
      draft.error.set(translate('register.mismatch'));
    } else {
      draft.error.set('');
      draft.step.set(2);
    }
  }

  protected onPreferences(next: UserPreferences): void {
    const prev = this.draft.preferences();
    const touched =
      this.draft.favoritesTouched() ||
      prev.favoriteCurrencies.join() !== next.favoriteCurrencies.join();
    const preferences = followDefaultCurrency(prev, next, touched);
    this.draft.favoritesTouched.set(touched);
    this.draft.preferences.set(preferences);
    this.prefs.preview(preferences);
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    const draft = this.draft;
    this.draft.error.set('');
    this.submitting.set(true);

    this.auth
      .register(draft.email(), draft.password(), draft.name() || null, draft.preferences())
      .subscribe({
        next: () => this.signIn(),
        error: (response: HttpErrorResponse) => {
          this.submitting.set(false);
          if (response.status === 409) draft.step.set(1);
          draft.error.set(
            httpErrorMessage(response, {
              invalid: translate('register.invalid'),
              conflict: translate('register.conflict'),
              fallback: translate('register.fallback'),
            }),
          );
        },
      });
  }

  private signIn(): void {
    const draft = this.draft;
    this.auth.login(draft.email(), draft.password()).subscribe({
      next: (response) => {
        const preferences = draft.preferences();
        this.auth.storeToken(response.token, true);
        this.ledgers.clearCurrentLedger();
        this.prefs.adopt(preferences);
        draft.reset();
        this.router.navigateByUrl('/new-ledger');
      },
      error: () => {
        this.submitting.set(false);
        draft.step.set(1);
        draft.error.set(translate('register.signInFailed'));
      },
    });
  }
}
