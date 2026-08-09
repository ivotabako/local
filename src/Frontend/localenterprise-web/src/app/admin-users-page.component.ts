import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormField, form, minLength, required } from '@angular/forms/signals';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CardModule } from '@openng/optimus-ui/card';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { finalize } from 'rxjs';
import { CreateUserAccountRequest, UserAccount, UserRole, userRoles } from './models/user-account';
import { NotificationsService } from './services/notifications.service';
import { UserAccountsService } from './services/user-accounts.service';

@Component({
  selector: 'app-admin-users-page',
  standalone: true,
  imports: [CommonModule, FormField, ButtonModule, CardModule, InputTextModule],
  template: `
    <section class="users-layout">
      <p-card header="Create User">
        <form class="form-grid" novalidate (submit)="createUser($event)">
          <label>
            Username
            <input pInputText [formField]="createForm.username" />
          </label>
          <label>
            Temporary Password
            <input pInputText type="password" [formField]="createForm.password" />
          </label>
          <label>
            Role
            <select [formField]="createForm.role">
              @for (role of roles; track role) {
                <option [value]="role">{{ role }}</option>
              }
            </select>
          </label>
          <button pButton type="submit" [disabled]="saving() || createForm().invalid()">Create User</button>
        </form>

        @if (errorMessage()) {
          <p class="error">{{ errorMessage() }}</p>
        }

        @if (statusMessage()) {
          <p class="status">{{ statusMessage() }}</p>
        }
      </p-card>

      <p-card header="Users">
        <div class="actions">
          <button pButton type="button" (click)="loadUsers()" [disabled]="loading()">Refresh</button>
        </div>
        <div class="table-shell">
          <table>
            <thead>
              <tr>
                <th>Username</th>
                <th>Roles</th>
                <th>2FA</th>
                <th>Lock</th>
                <th>Password</th>
                <th>Created</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              @for (user of users(); track user.id) {
                <tr>
                  <td>{{ user.username }}</td>
                  <td>
                    <select [value]="user.roles[0]" (change)="updateRole(user, $any($event.target).value)">
                      @for (role of roles; track role) {
                        <option [value]="role">{{ role }}</option>
                      }
                    </select>
                  </td>
                  <td>{{ user.twoFactorEnabled ? 'Enabled' : 'Not enabled' }}</td>
                  <td>{{ user.isLocked ? 'Locked' : 'Active' }}</td>
                  <td>{{ user.requiresPasswordChange ? 'Reset required' : 'Current' }}</td>
                  <td>{{ user.createdAt | date: 'short' }}</td>
                  <td>
                    <div class="row-actions">
                      <button pButton type="button" severity="secondary" size="small" (click)="startResetPassword(user)">Reset Password</button>
                      <button pButton type="button" severity="secondary" size="small" (click)="toggleLock(user)">{{ user.isLocked ? 'Unlock' : 'Lock' }}</button>
                      <button pButton type="button" severity="danger" size="small" (click)="deleteUser(user)">Delete</button>
                    </div>
                  </td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="7">No users available.</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        @if (resetTargetUser()) {
          <section class="reset-panel">
            <h3>Reset Password for {{ resetTargetUser()!.username }}</h3>
            <form class="form-grid" novalidate (submit)="resetPassword($event)">
              <label>
                New Temporary Password
                <input pInputText type="password" [formField]="resetForm.newPassword" />
              </label>
              <div class="row-actions">
                <button pButton type="submit" [disabled]="saving() || resetForm().invalid()">Apply Reset</button>
                <button pButton type="button" severity="secondary" (click)="cancelResetPassword()">Cancel</button>
              </div>
            </form>
          </section>
        }
      </p-card>
    </section>
  `,
  styles: [
    `
      .users-layout { display: grid; gap: 1rem; width: min(78rem, 100%); margin: 0 auto; }
      .form-grid { display: grid; gap: 0.9rem; grid-template-columns: repeat(auto-fit, minmax(12rem, 1fr)); align-items: end; }
      label { display: grid; gap: 0.45rem; font-weight: 600; color: #1b4249; }
      .actions { margin-bottom: 1rem; }
      .table-shell { overflow-x: auto; }
      table { width: 100%; border-collapse: collapse; }
      th, td { padding: 0.8rem 0.65rem; border-bottom: 1px solid #d5e0de; text-align: left; }
      .error { color: #8d1f1f; font-weight: 600; }
      .status { color: #114a56; font-weight: 600; }
      select { min-height: 2.5rem; border-radius: 0.75rem; border: 1px solid #b8c9c6; padding: 0 0.8rem; }
      .row-actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
      .reset-panel { margin-top: 1rem; padding-top: 1rem; border-top: 1px solid #d8e4e1; }
    `
  ]
})
export class AdminUsersPageComponent {
  private readonly userAccountsService = inject(UserAccountsService);
  private readonly notifications = inject(NotificationsService);

  protected readonly users = signal<UserAccount[]>([]);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly statusMessage = signal('');
  protected readonly roles = [...userRoles];
  protected readonly resetTargetUser = signal<UserAccount | null>(null);

  private readonly createModel = signal({
    username: '',
    password: '',
    role: 'Reader' as UserRole
  });
  private readonly resetModel = signal({ newPassword: '' });

  protected readonly createForm = form(this.createModel, (path) => {
    required(path.username, { message: 'Username is required.' });
    minLength(path.username, 3, { message: 'Username must be at least 3 characters.' });
    required(path.password, { message: 'Password is required.' });
    minLength(path.password, 12, { message: 'Password must be at least 12 characters.' });
    required(path.role, { message: 'Role is required.' });
  });

  protected readonly resetForm = form(this.resetModel, (path) => {
    required(path.newPassword, { message: 'Temporary password is required.' });
    minLength(path.newPassword, 12, { message: 'Temporary password must be at least 12 characters.' });
  });

  constructor() {
    this.loadUsers();
  }

  protected loadUsers(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.userAccountsService
      .list()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (users) => {
          this.users.set(users);
        },
        error: (error) => {
          const message = error?.error?.error ?? 'Failed to load users.';
          this.errorMessage.set(message);
          this.notifications.error(message);
        }
      });
  }

  protected createUser(event: Event): void {
    event.preventDefault();
    this.errorMessage.set('');
    this.statusMessage.set('');

    if (this.createForm().invalid()) {
      this.errorMessage.set('Complete all create-user fields.');
      return;
    }

    const model = this.createModel();
    const request: CreateUserAccountRequest = {
      username: model.username.trim(),
      password: model.password,
      roles: [model.role]
    };

    this.saving.set(true);
    this.userAccountsService
      .create(request)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (created) => {
          this.users.update((users) => [...users, created].sort((left, right) => left.username.localeCompare(right.username)));
          this.statusMessage.set(`Created ${created.username}. Password change is required on first use.`);
          this.notifications.success(`Created ${created.username}.`);
          this.createModel.set({ username: '', password: '', role: 'Reader' });
        },
        error: (error) => {
          const message = error?.error?.error ?? 'User creation failed.';
          this.errorMessage.set(message);
          this.notifications.error(message);
        }
      });
  }

  protected updateRole(user: UserAccount, role: string): void {
    const nextRole = role as UserRole;
    if (user.roles[0] === nextRole) {
      return;
    }

    this.errorMessage.set('');
    this.statusMessage.set('');
    this.userAccountsService.update(user.id, { roles: [nextRole] }).subscribe({
      next: (updated) => {
        this.users.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
        this.statusMessage.set(`Updated ${updated.username} to ${updated.roles.join(', ')}.`);
        this.notifications.success(`Updated ${updated.username}.`);
      },
      error: (error) => {
        const message = error?.error?.error ?? 'Role update failed.';
        this.errorMessage.set(message);
        this.notifications.error(message);
      }
    });
  }

  protected startResetPassword(user: UserAccount): void {
    this.resetTargetUser.set(user);
    this.resetModel.set({ newPassword: '' });
  }

  protected cancelResetPassword(): void {
    this.resetTargetUser.set(null);
    this.resetModel.set({ newPassword: '' });
  }

  protected resetPassword(event: Event): void {
    event.preventDefault();

    const user = this.resetTargetUser();
    if (!user) {
      return;
    }

    if (this.resetForm().invalid()) {
      this.notifications.error('Enter a temporary password with at least 12 characters.');
      return;
    }

    this.saving.set(true);
    this.userAccountsService
      .resetPassword(user.id, { newPassword: this.resetModel().newPassword })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (updated) => {
          this.users.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
          this.cancelResetPassword();
          this.notifications.success(`Password reset applied for ${updated.username}.`);
        },
        error: (error) => {
          const message = error?.error?.error ?? 'Password reset failed.';
          this.errorMessage.set(message);
          this.notifications.error(message);
        }
      });
  }

  protected toggleLock(user: UserAccount): void {
    const action = user.isLocked ? this.userAccountsService.unlock(user.id) : this.userAccountsService.lock(user.id);

    action.subscribe({
      next: (updated) => {
        this.users.update((items) => items.map((item) => (item.id === updated.id ? updated : item)));
        this.notifications.success(`${updated.username} is now ${updated.isLocked ? 'locked' : 'unlocked'}.`);
      },
      error: (error) => {
        const message = error?.error?.error ?? 'Unable to update lock state.';
        this.errorMessage.set(message);
        this.notifications.error(message);
      }
    });
  }

  protected deleteUser(user: UserAccount): void {
    this.errorMessage.set('');
    this.statusMessage.set('');

    if (!window.confirm(`Delete ${user.username}?`)) {
      return;
    }

    this.userAccountsService.delete(user.id).subscribe({
      next: () => {
        this.users.update((items) => items.filter((item) => item.id !== user.id));
        this.statusMessage.set(`Deleted ${user.username}.`);
        this.notifications.success(`Deleted ${user.username}.`);
        if (this.resetTargetUser()?.id === user.id) {
          this.cancelResetPassword();
        }
      },
      error: (error) => {
        const message = error?.error?.error ?? 'User deletion failed.';
        this.errorMessage.set(message);
        this.notifications.error(message);
      }
    });
  }
}