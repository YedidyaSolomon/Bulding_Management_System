import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';
import { NotificationDto } from '../../shared/models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService {

  private readonly apiUrl = `${environment.apiUrl}/notifications`;

  /** Emits the current unread count — consumed by the toolbar badge. */
  private unreadCount$ = new BehaviorSubject<number>(0);
  readonly unread$ = this.unreadCount$.asObservable();

  constructor(private http: HttpClient) {}

  // ── API calls ─────────────────────────────────────────────────────────────

  /** GET /api/notifications — returns all notifications for the current user */
  getAll(): Observable<NotificationDto[]> {
    return this.http
      .get<ApiResponse<NotificationDto[]>>(this.apiUrl)
      .pipe(
        map(r => r.data ?? []),
        tap(items => this.unreadCount$.next(items.filter(n => !n.isRead).length)),
      );
  }

  /** PUT /api/notifications/{id}/read — mark one as read */
  markAsRead(id: number): Observable<NotificationDto> {
    return this.http
      .put<ApiResponse<NotificationDto>>(`${this.apiUrl}/${id}/read`, {})
      .pipe(map(r => r.data!));
  }

  /** PUT /api/notifications/read-all — mark all as read */
  markAllAsRead(): Observable<void> {
    return this.http
      .put<ApiResponse<null>>(`${this.apiUrl}/read-all`, {})
      .pipe(map(() => void 0));
  }

  /** Manually push a new unread count (used after mark-all-read). */
  setUnreadCount(count: number): void {
    this.unreadCount$.next(count);
  }
}
