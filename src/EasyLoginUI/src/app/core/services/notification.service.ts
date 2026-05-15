import { Injectable, inject } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { NotificationItem } from '../models/notification.model';
import { ApiService } from './api.service';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private connection: HubConnection | null = null;
  private starting: Promise<void> | null = null;

  readonly list$ = new BehaviorSubject<NotificationItem[]>([]);
  readonly unreadCount$ = new BehaviorSubject<number>(0);

  init(): void {
    if (!this.auth.isAuthenticated()) return;

    void this.ensureStarted();
  }

  stop(): void {
    const currentConnection = this.connection;
    this.connection = null;
    this.starting = null;
    this.list$.next([]);
    this.unreadCount$.next(0);

    if (currentConnection) {
      void currentConnection.stop();
    }
  }

  loadInitial(): void {
    this.api
      .get<NotificationItem[]>('/notifications?unreadOnly=false&skip=0&take=50')
      .subscribe({
        next: notifications => this.list$.next(notifications),
        error: error => console.error('Failed to load notifications.', error),
      });
  }

  loadUnreadCount(): void {
    this.api.get<number>('/notifications/unread-count').subscribe({
      next: count => this.unreadCount$.next(count),
      error: error => console.error('Failed to load unread notification count.', error),
    });
  }

  markRead(id: string): void {
    this.api.put<void>(`/notifications/${id}/read`, {}).subscribe({
      next: () => {
        const current = this.list$.value;
        const notification = current.find(item => item.id === id);
        const wasUnread = notification ? !notification.isRead : false;

        this.list$.next(current.map(item => item.id === id ? { ...item, isRead: true } : item));
        if (wasUnread) {
          this.unreadCount$.next(Math.max(0, this.unreadCount$.value - 1));
        }
      },
      error: error => console.error('Failed to mark notification as read.', error),
    });
  }

  markAllRead(): void {
    this.api.put<void>('/notifications/read-all', {}).subscribe({
      next: () => {
        this.list$.next(this.list$.value.map(item => ({ ...item, isRead: true })));
        this.unreadCount$.next(0);
      },
      error: error => console.error('Failed to mark all notifications as read.', error),
    });
  }

  private async ensureStarted(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) {
      return;
    }

    if (this.connection?.state === HubConnectionState.Connecting
      || this.connection?.state === HubConnectionState.Reconnecting) {
      return this.starting ?? Promise.resolve();
    }

    if (!this.connection) {
      this.connection = this.createConnection();
    }

    this.starting = this.connection
      .start()
      .then(() => this.refreshSnapshot())
      .catch(error => {
        console.error('Failed to start notification hub.', error);
        this.connection = null;
      })
      .finally(() => {
        this.starting = null;
      });

    return this.starting;
  }

  private createConnection(): HubConnection {
    const hubUrl = `${environment.apiUrl.replace(/\/api\/?$/, '')}/hubs/notifications`;
    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => this.auth.getAccessToken() ?? '' })
      .withAutomaticReconnect()
      .build();

    connection.on('notification', (notification: NotificationItem) => {
      this.list$.next([notification, ...this.list$.value].slice(0, 50));
      if (!notification.isRead) {
        this.unreadCount$.next(this.unreadCount$.value + 1);
      }
    });

    connection.onreconnected(() => this.refreshSnapshot());

    return connection;
  }

  private refreshSnapshot(): void {
    this.loadUnreadCount();
    this.loadInitial();
  }
}
