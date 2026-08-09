import { Routes } from '@angular/router';
import { AuthCallbackPlaceholderComponent } from './auth-callback-placeholder.component';

export const routes: Routes = [
	{ path: 'auth/callback', component: AuthCallbackPlaceholderComponent },
	{ path: '**', component: AuthCallbackPlaceholderComponent }
];
