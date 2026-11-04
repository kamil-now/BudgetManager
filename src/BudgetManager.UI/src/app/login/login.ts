import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth/auth';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly showPassword = signal(false);
  protected readonly submitting = signal(false);
  protected readonly error = signal('');

  protected email = '';
  protected password = '';
  protected rememberMe = false;

  protected togglePassword(): void {
    this.showPassword.update((shown) => !shown);
  }

  protected submit(): void {
    if (this.submitting()) {
      return;
    }

    this.error.set('');
    this.submitting.set(true);

    this.auth.login(this.email, this.password).subscribe({
      next: (response) => {
        this.auth.storeToken(response.token, this.rememberMe);
        this.router.navigateByUrl('/home');
      },
      error: (response) => {
        this.submitting.set(false);
        this.error.set(
          response.status === 401
            ? 'Wrong email or password.'
            : response.status === 429
              ? 'Too many attempts. Wait a minute and try again.'
              : 'Could not sign in. Try again.',
        );
      },
    });
  }
}
