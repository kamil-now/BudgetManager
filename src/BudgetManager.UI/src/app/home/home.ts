import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth';

@Component({
  selector: 'app-home',
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected signOut(): void {
    this.auth.clear();
    this.router.navigateByUrl('/');
  }
}
