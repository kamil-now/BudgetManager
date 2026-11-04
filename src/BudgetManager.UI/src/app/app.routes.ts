import { Routes } from '@angular/router';
import { Login } from './login/login';
import { Register } from './register/register';
import { Home } from './home/home';
import { authGuard, guestGuard } from './auth/auth.guard';

export const routes: Routes = [
  { path: '', component: Login, canActivate: [guestGuard] },
  { path: 'register', component: Register, canActivate: [guestGuard] },
  { path: 'home', component: Home, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];
