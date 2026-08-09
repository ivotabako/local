import { Component, inject, signal } from '@angular/core';
import { FormField, form, minLength, required } from '@angular/forms/signals';
import { Router } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CardModule } from '@openng/optimus-ui/card';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { finalize } from 'rxjs';
import { AuthService } from './services/auth.service';
import { NotificationsService } from './services/notifications.service';

@Component({
  selector: 'app-change-password-page',
  standalone: true,
  imports: [FormField, ButtonModule, CardModule, InputTextModule],
  template: `
    <section class="page-grid">
      <p-card header="Change Password">
        <p class="description">Use a strong password with at least 12 characters, upper and lower case letters, a number, and a symbol.</p>

        <form class="form-grid" novalidate (submit)="submit($event)">
          <label>
            Current Password
            <input pInputText type="password" [formField]="passwordForm.currentPassword" />
          </label>
          <label>
            New Password
            <input pInputText type="password" [formField]="passwordForm.newPassword" />
          </label>
          <label>
            Confirm Password
            <input pInputText type="password" [formField]="passwordForm.confirmPassword" />
          </label>
          <div class="actions">
            <button pButton type="submit" [disabled]="saving() || passwordForm().invalid()">Update Password</button>
          </div>
        </form>

        @if (errorMessage()) {
          <p class="error">{{ errorMessage() }}</p>
        }

        @if (statusMessage()) {
          <p class="status">{{ statusMessage() }}</p>
        }
      </p-card>
    </section>
  `,
  styles: [
    `
      .page-grid { width: min(38rem, 100%); margin: 0 auto; }
      .description { color: #34575d; }
      .form-grid { display: grid; gap: 0.9rem; }
      label { display: grid; gap: 0.45rem; font-weight: 600; color: #1b4249; }
      .actions { margin-top: 0.5rem; }
      .error { color: #8d1f1f; font-weight: 600; }
      .status { color: #114a56; font-weight: 600; }
    `
  ]
})
export class ChangePasswordPageComponent {
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationsService);
  private readonly router = inject(Router);

  protected readonly saving = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly statusMessage = signal('');

  private readonly passwordModel = signal({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });

  protected readonly passwordForm = form(this.passwordModel, (path) => {
    required(path.currentPassword, { message: 'Current password is required.' });
    required(path.newPassword, { message: 'New password is required.' });
    minLength(path.newPassword, 12, { message: 'New password must be at least 12 characters.' });
    required(path.confirmPassword, { message: 'Please confirm the new password.' });
  });

  protected submit(event: Event): void {
    event.preventDefault();
    this.errorMessage.set('');
    this.statusMessage.set('');

    if (this.passwordForm().invalid()) {
      this.errorMessage.set('Complete all password fields.');
      this.notifications.error('Complete all password fields.');
      return;
    }

    const model = this.passwordModel();
    if (model.newPassword !== model.confirmPassword) {
      this.errorMessage.set('The new password confirmation does not match.');
      this.notifications.error('The new password confirmation does not match.');
      return;
    }

    this.saving.set(true);
    this.authService
      .changePassword({
        currentPassword: model.currentPassword,
        newPassword: model.newPassword
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.statusMessage.set('Password updated successfully.');
          this.notifications.success('Password updated successfully.');
          this.passwordModel.set({ currentPassword: '', newPassword: '', confirmPassword: '' });
          queueMicrotask(() => this.router.navigateByUrl('/account'));
        },
        error: (error) => {
          const message = error?.error?.error ?? 'Password change failed.';
          this.errorMessage.set(message);
          this.notifications.error(message);
        }
      });
  }
}