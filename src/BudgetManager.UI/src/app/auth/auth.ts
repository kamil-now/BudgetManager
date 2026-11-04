import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

interface TokenResponse {
  token: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenKey = 'budget-manager.token';

  login(email: string, password: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>('/api/auth/login', { email, password });
  }

  register(email: string, password: string, name: string | null): Observable<string> {
    return this.http.post<string>('/api/auth/register', { email, password, name });
  }

  storeToken(token: string, remember: boolean): void {
    this.clear();
    (remember ? localStorage : sessionStorage).setItem(this.tokenKey, token);
  }

  get token(): string | null {
    const token = localStorage.getItem(this.tokenKey) ?? sessionStorage.getItem(this.tokenKey);
    if (token && isExpired(token)) {
      this.clear();
      return null;
    }
    return token;
  }

  get isAuthenticated(): boolean {
    return this.token !== null;
  }

  clear(): void {
    localStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.tokenKey);
  }
}

function isExpired(token: string): boolean {
  try {
    const payload = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    const { exp } = JSON.parse(atob(payload)) as { exp?: number };
    return exp !== undefined && exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}
