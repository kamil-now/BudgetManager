import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

interface TokenResponse {
  token: string;
}

export interface RegisterPreferences {
  theme: string;
  dateFormat: string;
  defaultCurrency: string;
  favoriteCurrencies: string[];
  language: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenKey = 'budget-manager.token';

  login(email: string, password: string): Observable<TokenResponse> {
    return this.http.post<TokenResponse>('/api/auth/login', { email, password });
  }

  register(
    email: string,
    password: string,
    name: string | null,
    preferences?: RegisterPreferences,
  ): Observable<string> {
    return this.http.post<string>('/api/auth/register', {
      email,
      password,
      name,
      ...(preferences ? { preferences } : {}),
    });
  }

  deleteAccount(password: string): Observable<void> {
    return this.http.delete<void>('/api/users/me', { body: { password } });
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

  /** Display name and email read from the token claims (not verified; display only). */
  get user(): { name: string; email: string } | null {
    const token = this.token;
    if (!token) return null;
    try {
      const claims = decodeClaims<Record<string, string>>(token);
      const email = pick(claims, 'email', 'emailaddress') ?? '';
      return { name: pick(claims, 'name', 'unique_name') ?? email, email };
    } catch {
      return null;
    }
  }

  clear(): void {
    localStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.tokenKey);
  }
}

function base64Payload(token: string): string {
  return token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
}

function decodeClaims<T>(token: string): T {
  const bytes = Uint8Array.from(atob(base64Payload(token)), (c) => c.charCodeAt(0));
  return JSON.parse(new TextDecoder().decode(bytes)) as T;
}

// ASP.NET emits either short claim names or full claim-type URIs depending on the token handler.
function pick(claims: Record<string, string>, ...names: string[]): string | undefined {
  const key = Object.keys(claims).find((k) => names.some((n) => k === n || k.endsWith('/' + n)));
  return key ? claims[key] : undefined;
}

function isExpired(token: string): boolean {
  try {
    const { exp } = decodeClaims<{ exp?: number }>(token);
    return exp !== undefined && exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}
