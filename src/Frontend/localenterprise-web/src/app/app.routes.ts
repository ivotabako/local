import { Routes } from '@angular/router';
import { AccountPageComponent } from './account-page.component';
import { AdminUsersPageComponent } from './admin-users-page.component';
import { AuthCallbackPlaceholderComponent } from './auth-callback-placeholder.component';
import { CarsPageComponent } from './cars-page.component';
import { ChangePasswordPageComponent } from './change-password-page.component';
import { LandingPageComponent } from './landing-page.component';
import { adminGuard, authGuard, passwordChangeGuard } from './services/auth.guards';

export const routes: Routes = [
	{ path: '', component: LandingPageComponent },
	{ path: 'auth/callback', component: AuthCallbackPlaceholderComponent },
	{ path: 'cars', component: CarsPageComponent, canActivate: [authGuard, passwordChangeGuard] },
	{ path: 'account', component: AccountPageComponent, canActivate: [authGuard] },
	{ path: 'account/password', component: ChangePasswordPageComponent, canActivate: [authGuard] },
	{ path: 'admin/users', component: AdminUsersPageComponent, canActivate: [authGuard, passwordChangeGuard, adminGuard] },
	{ path: '**', redirectTo: '' }
];
