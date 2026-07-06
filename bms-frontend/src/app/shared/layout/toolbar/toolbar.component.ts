import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../features/notifications/notification.service';
import { AuthResponse } from '../../models/auth.models';

@Component({
  selector: 'app-toolbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule,
    MatBadgeModule,
    MatDividerModule,
    MatTooltipModule,
  ],
  templateUrl: './toolbar.component.html',
  styleUrls: ['./toolbar.component.scss'],
})
export class ToolbarComponent implements OnInit, OnDestroy {
  @Input()  sidenavCollapsed = false;
  @Output() menuToggle = new EventEmitter<void>();

  currentUser: AuthResponse | null = null;
  unreadNotifications = 0;

  private destroy$ = new Subject<void>();

  constructor(
    private authService:          AuthService,
    private notificationService:  NotificationService,
  ) {}

  ngOnInit(): void {
    // Track current user
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.currentUser = user;

        // Fetch notifications whenever a user session is active
        if (user) {
          this.notificationService.getAll()
            .pipe(takeUntil(this.destroy$))
            .subscribe(); // getAll() updates unread$ via tap() internally
        }
      });

    // Subscribe to live unread count
    this.notificationService.unread$
      .pipe(takeUntil(this.destroy$))
      .subscribe(count => (this.unreadNotifications = count));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onMenuToggle(): void {
    this.menuToggle.emit();
  }

  logout(): void {
    this.authService.logout();
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map(n => n[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }
}
