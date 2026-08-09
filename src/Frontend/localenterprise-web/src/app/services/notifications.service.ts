import { Injectable, signal } from '@angular/core';

export interface NotificationItem {
  id: number;
  kind: 'success' | 'error' | 'info';
  message: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private nextId = 0;
  private readonly items = signal<NotificationItem[]>([]);

  readonly notifications = this.items.asReadonly();

  success(message: string): void {
    this.push('success', message);
  }

  error(message: string): void {
    this.push('error', message);
  }

  info(message: string): void {
    this.push('info', message);
  }

  dismiss(id: number): void {
    this.items.update((items) => items.filter((item) => item.id !== id));
  }

  private push(kind: NotificationItem['kind'], message: string): void {
    const id = ++this.nextId;
    this.items.update((items) => [...items, { id, kind, message }]);
    setTimeout(() => this.dismiss(id), 5000);
  }
}