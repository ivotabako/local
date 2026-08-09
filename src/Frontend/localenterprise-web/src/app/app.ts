import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { AuthService } from './services/auth.service';
import { apiConfig } from './services/api-config';
import { NotificationsService } from './services/notifications.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly authService = inject(AuthService);
  private readonly notificationsService = inject(NotificationsService);

  protected readonly isAuthenticated = this.authService.isAuthenticated;
  protected readonly currentUser = this.authService.currentUser;
  protected readonly isAdmin = this.authService.isAdmin;
  protected readonly title = computed(() => 'LocalEnterprise Access Hub');
  protected readonly notifications = this.notificationsService.notifications;

  constructor() {
    if (this.isAuthenticated() && this.currentUser() === null) {
      void this.authService.ensureCurrentUserLoaded();
    }
  }

  protected async login(event: Event): Promise<void> {
    event.preventDefault();
    const authorizeUrl = await this.authService.beginAuthorizationCodeLogin(this.redirectUri());
    window.location.assign(authorizeUrl);
  }

  protected logout(): void {
    this.authService.logout();
    window.location.assign(this.authService.buildLogoutUrl(window.location.origin));
  }

  protected dismissNotification(id: number): void {
    this.notificationsService.dismiss(id);
  }

  private redirectUri(): string {
    return `${window.location.origin}${apiConfig.authRedirectPath}`;
  }
}
