import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormField, form, required } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CardModule } from '@openng/optimus-ui/card';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { finalize } from 'rxjs';
import QRCode from 'qrcode';
import { AuthService } from './services/auth.service';
import { NotificationsService } from './services/notifications.service';
import { TwoFactorEnrollment, TwoFactorVerificationResult } from './models/user-account';

@Component({
  selector: 'app-account-page',
  standalone: true,
  imports: [CommonModule, FormField, RouterLink, ButtonModule, CardModule, InputTextModule],
  template: `
    <section class="page-grid">
      <p-card header="Account Overview">
        @if (authService.currentUser(); as user) {
          <dl class="facts">
            <div><dt>Username</dt><dd>{{ user.username }}</dd></div>
            <div><dt>Roles</dt><dd>{{ user.roles.join(', ') }}</dd></div>
            <div><dt>Created</dt><dd>{{ user.createdAt | date: 'medium' }}</dd></div>
            <div><dt>Created By</dt><dd>{{ user.createdBy ?? 'Bootstrap' }}</dd></div>
            <div><dt>Password Status</dt><dd>{{ user.requiresPasswordChange ? 'Change required' : 'Up to date' }}</dd></div>
            <div><dt>Last Change</dt><dd>{{ user.lastPasswordChangedAt ? (user.lastPasswordChangedAt | date: 'medium') : 'Not recorded' }}</dd></div>
            <div><dt>Two-Factor</dt><dd>{{ user.twoFactorEnabled ? 'Enabled' : 'Not enabled' }}</dd></div>
            <div><dt>Recovery Codes</dt><dd>{{ user.recoveryCodesRemaining }}</dd></div>
            <div><dt>Lock State</dt><dd>{{ user.isLocked ? 'Locked' : 'Active' }}</dd></div>
          </dl>

          @if (user.requiresPasswordChange) {
            <p class="warning">You must change your password before using the protected application routes.</p>
          }

          <div class="action-row">
            <a routerLink="/account/password"><button pButton type="button">Change Password</button></a>
            @if (!user.twoFactorEnabled) {
              <button pButton type="button" severity="secondary" [disabled]="setupLoading()" (click)="beginEnrollment()">Start 2FA Setup</button>
            } @else {
              <button pButton type="button" severity="secondary" [disabled]="manageLoading()" (click)="regenerateRecoveryCodes()">Regenerate Recovery Codes</button>
              <button pButton type="button" severity="danger" [disabled]="manageLoading()" (click)="disableTwoFactor()">Disable 2FA</button>
            }
          </div>

          @if (enrollment()) {
            <section class="two-factor-card">
              <h3>Two-Factor Setup</h3>
              <p>Scan this QR code in your authenticator app, then verify with a code to finish setup.</p>
              @if (qrCodeDataUrl()) {
                <img class="qr-preview" [src]="qrCodeDataUrl()!" alt="Authenticator setup QR code" />
              }
              <p class="mono">{{ enrollment()!.sharedSecret }}</p>
              <p class="uri">{{ enrollment()!.provisioningUri }}</p>

              <form class="verify-form" novalidate (submit)="verifyEnrollment($event)">
                <label>
                  Verification Code
                  <input pInputText [formField]="verificationForm.code" />
                </label>
                <button pButton type="submit" [disabled]="verificationLoading() || verificationForm().invalid()">Enable 2FA</button>
              </form>
            </section>
          }

          @if (user.twoFactorEnabled) {
            <section class="two-factor-card">
              <h3>Manage Two-Factor</h3>
              <p>Enter a current authenticator or recovery code to regenerate recovery codes or disable two-factor.</p>
              <form class="verify-form" novalidate>
                <label>
                  Verification Code
                  <input pInputText [formField]="manageForm.code" />
                </label>
              </form>
            </section>
          }

          @if (recoveryCodes().length > 0) {
            <section class="recovery-list">
              <h3>Recovery Codes</h3>
              <p>Store these codes securely. Each code can be used once.</p>
              <ul>
                @for (code of recoveryCodes(); track code) {
                  <li class="mono">{{ code }}</li>
                }
              </ul>
            </section>
          }
        } @else {
          <p>Loading account details...</p>
        }
      </p-card>
    </section>
  `,
  styles: [
    `
      .page-grid { width: min(56rem, 100%); margin: 0 auto; }
      .facts { display: grid; gap: 0.9rem; margin: 0 0 1rem; }
      .facts div { display: grid; gap: 0.15rem; }
      dt { font-size: 0.82rem; text-transform: uppercase; letter-spacing: 0.08em; color: #516e73; }
      dd { margin: 0; font-weight: 600; color: #18353a; }
      .warning { margin: 0 0 1rem; color: #8d1f1f; font-weight: 600; }
      .action-row { display: flex; gap: 0.75rem; flex-wrap: wrap; }
      .two-factor-card, .recovery-list { margin-top: 1rem; padding-top: 1rem; border-top: 1px solid #d8e4e1; }
      .verify-form { display: grid; gap: 0.75rem; margin-top: 0.75rem; }
      .verify-form label { display: grid; gap: 0.45rem; font-weight: 600; color: #1b4249; }
      .qr-preview { width: 12rem; height: 12rem; display: block; margin: 0.5rem 0 0.75rem; border: 1px solid #d8e4e1; border-radius: 0.5rem; }
      .mono { font-family: Consolas, 'Courier New', monospace; word-break: break-all; }
      .uri { color: #34575d; word-break: break-all; }
    `
  ]
})
export class AccountPageComponent {
  protected readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationsService);

  protected readonly setupLoading = signal(false);
  protected readonly verificationLoading = signal(false);
  protected readonly manageLoading = signal(false);
  protected readonly enrollment = signal<TwoFactorEnrollment | null>(null);
  protected readonly qrCodeDataUrl = signal<string | null>(null);
  protected readonly recoveryCodes = signal<string[]>([]);

  private readonly verificationModel = signal({ code: '' });
  private readonly manageModel = signal({ code: '' });
  protected readonly verificationForm = form(this.verificationModel, (path) => {
    required(path.code, { message: 'Verification code is required.' });
  });
  protected readonly manageForm = form(this.manageModel, (path) => {
    required(path.code, { message: 'Verification code is required.' });
  });

  protected beginEnrollment(): void {
    this.setupLoading.set(true);
    this.authService
      .beginTwoFactorEnrollment()
      .pipe(finalize(() => this.setupLoading.set(false)))
      .subscribe({
        next: (enrollment) => {
          this.enrollment.set(enrollment);
          this.renderQrCode(enrollment.provisioningUri);
          this.recoveryCodes.set([]);
          this.notifications.info('Two-factor setup started. Verify the authenticator code to finish enabling it.');
        },
        error: (error) => {
          this.notifications.error(error?.error?.error ?? 'Unable to start two-factor setup.');
        }
      });
  }

  protected verifyEnrollment(event: Event): void {
    event.preventDefault();
    if (this.verificationForm().invalid()) {
      this.notifications.error('Enter the authenticator verification code.');
      return;
    }

    this.verificationLoading.set(true);
    this.authService
      .verifyTwoFactor(this.verificationModel().code)
      .pipe(finalize(() => this.verificationLoading.set(false)))
      .subscribe({
        next: (result: TwoFactorVerificationResult) => {
          this.recoveryCodes.set(result.recoveryCodes);
          this.enrollment.set(null);
          this.qrCodeDataUrl.set(null);
          this.verificationModel.set({ code: '' });
          this.notifications.success('Two-factor authentication is now enabled. Save the recovery codes securely.');
        },
        error: (error) => {
          this.notifications.error(error?.error?.error ?? 'Unable to verify the two-factor code.');
        }
      });
  }

  protected regenerateRecoveryCodes(): void {
    if (this.manageForm().invalid()) {
      this.notifications.error('Enter a verification code to regenerate recovery codes.');
      return;
    }

    this.manageLoading.set(true);
    this.authService
      .regenerateRecoveryCodes(this.manageModel().code)
      .pipe(finalize(() => this.manageLoading.set(false)))
      .subscribe({
        next: (result: TwoFactorVerificationResult) => {
          this.recoveryCodes.set(result.recoveryCodes);
          this.manageModel.set({ code: '' });
          this.notifications.success('Recovery codes regenerated. Save the new codes securely.');
        },
        error: (error) => {
          this.notifications.error(error?.error?.error ?? 'Unable to regenerate recovery codes.');
        }
      });
  }

  protected disableTwoFactor(): void {
    if (this.manageForm().invalid()) {
      this.notifications.error('Enter a verification code to disable two-factor authentication.');
      return;
    }

    this.manageLoading.set(true);
    this.authService
      .disableTwoFactor(this.manageModel().code)
      .pipe(finalize(() => this.manageLoading.set(false)))
      .subscribe({
        next: () => {
          this.enrollment.set(null);
          this.qrCodeDataUrl.set(null);
          this.recoveryCodes.set([]);
          this.manageModel.set({ code: '' });
          this.notifications.success('Two-factor authentication disabled.');
        },
        error: (error) => {
          this.notifications.error(error?.error?.error ?? 'Unable to disable two-factor authentication.');
        }
      });
  }

  private renderQrCode(provisioningUri: string): void {
    QRCode.toDataURL(provisioningUri, { errorCorrectionLevel: 'M', margin: 1, width: 240 })
      .then((dataUrl: string) => {
        this.qrCodeDataUrl.set(dataUrl);
      })
      .catch(() => {
        this.qrCodeDataUrl.set(null);
        this.notifications.error('Unable to render the QR code. Use the shared secret manually.');
      });
  }
}