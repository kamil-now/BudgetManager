import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { AuthService } from '../auth/auth';

// matches Constants.MinPasswordLength in the API
const minPasswordLength = 8;

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly showPassword = signal(false);
  protected readonly submitting = signal(false);
  protected readonly error = signal('');

  protected name = '';
  protected email = '';
  protected password = '';
  protected confirmPassword = '';

  protected togglePassword(): void {
    this.showPassword.update((shown) => !shown);
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    if (this.password.length < minPasswordLength) {
      this.error.set(`Password must be at least ${minPasswordLength} characters.`);
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.error.set('Passwords do not match.');
      return;
    }

    this.error.set('');
    this.submitting.set(true);

    this.auth
      .register(this.email, this.password, this.name.trim() || null)
      .pipe(switchMap(() => this.auth.login(this.email, this.password)))
      .subscribe({
        next: (response) => {
          this.auth.storeToken(response.token, true);
          this.router.navigateByUrl('/home');
        },
        error: (response) => {
          this.submitting.set(false);
          this.error.set(this.messageFor(response.status));
        },
      });
  }

  private messageFor(status: number): string {
    if (status === 409) {
      return 'An account with this email already exists.';
    }

    if (status === 422) {
      return 'Enter a valid email and password.';
    }

    if (status === 429) {
      return 'Too many attempts. Wait a minute and try again.';
    }

    return 'Could not create the account. Try again.';
  }
}
