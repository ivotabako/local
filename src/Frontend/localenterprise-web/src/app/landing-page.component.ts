import { Component } from '@angular/core';
import { CardModule } from '@openng/optimus-ui/card';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CardModule],
  template: `
    <section class="landing-grid">
      <p-card header="Auth Server Upgrade">
        <p>Bearer-protected account APIs, admin-managed users, and password lifecycle controls now sit behind the local OpenIddict host.</p>
      </p-card>
      <p-card header="Secure Admin Flow">
        <p>Admins can manage persisted users from the frontend after authenticating through the authorization code + PKCE flow.</p>
      </p-card>
      <p-card header="Phase 2 Started">
        <p>Forced password change and self-service password updates are now part of the account experience and backend contracts.</p>
      </p-card>
    </section>
  `,
  styles: [
    `
      .landing-grid {
        width: min(78rem, 100%);
        margin: 0 auto;
        display: grid;
        gap: 1rem;
        grid-template-columns: repeat(auto-fit, minmax(15rem, 1fr));
      }
    `
  ]
})
export class LandingPageComponent {}