import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../auth/auth';
import { SessionService } from '../../auth/session';
import { translate } from '../../i18n/i18n';
import { TranslatePipe } from '../../i18n/t.pipe';
import { httpErrorMessage } from '../../shared/http-error';
import { Modal } from '../../shared/modal';
import { PreferencesService } from '../../shared/preferences';

@Component({
  selector: 'app-delete-account',
  imports: [FormsModule, Modal, TranslatePipe],
  templateUrl: './delete-account.html',
})
export class DeleteAccount {
  private readonly auth = inject(AuthService);
  private readonly session = inject(SessionService);
  private readonly prefs = inject(PreferencesService);
  private readonly destroyRef = inject(DestroyRef);

  readonly closed = output<void>();

  protected readonly password = signal('');
  protected readonly submitting = signal(false);
  protected readonly error = signal('');

  protected confirm(): void {
    if (this.submitting()) return;
    if (!this.password()) {
      this.error.set(translate('deleteAccount.passwordRequired'));
      return;
    }
    this.error.set('');
    this.submitting.set(true);
    this.auth
      .deleteAccount(this.password())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.session.signOut();
          this.prefs.forget();
        },
        error: (response: HttpErrorResponse) => {
          this.submitting.set(false);
          // httpErrorMessage would show the server's own 422 detail; this dialog words it itself.
          const wrong = translate('deleteAccount.wrongPassword');
          this.error.set(
            response.status === 422
              ? wrong
              : httpErrorMessage(response, {
                invalid: wrong,
                fallback: translate('deleteAccount.error'),
              }),
          );
        },
      });
  }
}
