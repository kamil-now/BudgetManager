import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { AuthService } from '../auth/auth';
import { translate } from '../i18n/i18n';
import { TranslatePipe } from '../i18n/t.pipe';
import { RegisterDraft } from '../register/register-draft';
import { LedgerService } from '../shared/ledger';
import { PreferencesService } from '../shared/preferences';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink, TranslatePipe],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly ledgers = inject(LedgerService);
  private readonly router = inject(Router);
  private readonly prefs = inject(PreferencesService);

  constructor() {
    inject(RegisterDraft).reset();
  }

  protected readonly showPassword = signal(false);
  protected readonly submitting = signal(false);
  protected readonly error = signal('');

  protected email = '';
  protected password = '';
  protected rememberMe = false;

  protected togglePassword(): void {
    this.showPassword.update((shown) => !shown);
  }

  protected toggleTheme(): void {
    this.prefs.cycleTheme();
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    this.error.set('');
    this.submitting.set(true);

    this.auth.login(this.email.trim(), this.password).subscribe({
      next: (response) => {
        this.auth.storeToken(response.token, this.rememberMe);
        forkJoin([
          this.prefs.load().pipe(catchError(() => of(null))),
          this.ledgers.ensureLedgerSelected().pipe(catchError(() => of(undefined))),
        ]).subscribe(([, id]) =>
          this.router.navigateByUrl(id === null ? '/new-ledger' : '/dashboard'),
        );
      },
      error: (response) => {
        this.submitting.set(false);
        this.error.set(
          translate(
            response.status === 401
              ? 'login.wrong'
              : response.status === 429
                ? 'error.tooManyAttempts'
                : 'login.fallback',
          ),
        );
      },
    });
  }
}
