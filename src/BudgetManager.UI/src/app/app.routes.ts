import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Register } from './register/register';
import { Shell } from './shell/shell';
import { Dashboard } from './dashboard/dashboard';
import { Statistics } from './statistics/statistics';
import { NewLedger } from './new-ledger/new-ledger';
import { Settings } from './settings/settings';
import { authGuard, guestGuard } from './auth/auth.guard';
import { TranslationKey } from './i18n/i18n';

const title = (key: TranslationKey) => ({ title: key });

export const routes: Routes = [
  { path: '', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  { path: 'new-ledger', component: NewLedger, canActivate: [authGuard] },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', component: Dashboard, data: title('nav.dashboard') },
      { path: 'transactions', redirectTo: 'dashboard' },
      { path: 'budgets', redirectTo: 'dashboard' },
      { path: 'statistics', component: Statistics, data: title('nav.statistics') },
      { path: 'settings', component: Settings, data: title('nav.settings') },
    ],
  },
  { path: '**', redirectTo: '' },
];
