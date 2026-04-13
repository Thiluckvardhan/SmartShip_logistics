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

  private add(type: AppNotification['type'], message: string): string {
    const id = crypto.randomUUID();
    const current = this.notificationsSubject.getValue();
    this.notificationsSubject.next([...current, { id, type, message }]);
    return id;
  }

  success(message: string): void {
    const id = this.add('success', message);
    setTimeout(() => this.dismiss(id), 3000);
  }

  error(message: string): void {
    this.add('error', message);
  }

  info(message: string): void {
    const id = this.add('info', message);
    setTimeout(() => this.dismiss(id), 3000);
  }

  dismiss(id: string): void {
    const current = this.notificationsSubject.getValue();
    this.notificationsSubject.next(current.filter(n => n.id !== id));
  }
}
