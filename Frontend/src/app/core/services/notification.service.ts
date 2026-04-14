import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface AppNotification {
  id: string;
  type: 'success' | 'error' | 'info';
  message: string;
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private notificationsSubject = new BehaviorSubject<AppNotification[]>([]);
  notifications$ = this.notificationsSubject.asObservable();
  private readonly autoDismissMs = 2000;

  private add(type: AppNotification['type'], message: string): string {
    const id = crypto.randomUUID();
    const current = this.notificationsSubject.getValue();
    this.notificationsSubject.next([...current, { id, type, message }]);
    return id;
  }

  success(message: string): void {
    const id = this.add('success', message);
    setTimeout(() => this.dismiss(id), this.autoDismissMs);
  }

  error(message: string): void {
    const id = this.add('error', message);
    setTimeout(() => this.dismiss(id), this.autoDismissMs);
  }

  info(message: string): void {
    const id = this.add('info', message);
    setTimeout(() => this.dismiss(id), this.autoDismissMs);
  }

  dismiss(id: string): void {
    const current = this.notificationsSubject.getValue();
    this.notificationsSubject.next(current.filter(n => n.id !== id));
  }
}
