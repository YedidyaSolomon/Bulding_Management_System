import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatRippleModule } from '@angular/material/core';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { AuthResponse, UserRole } from '../../models/auth.models';
import { NAV_ITEMS, NavItem } from '../../../core/config/nav.config';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterLinkActive,
    MatIconModule,
    MatTooltipModule,
    MatRippleModule,
  ],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
})
export class SidebarComponent implements OnInit, OnDestroy {
  @Input() collapsed = false;

  visibleItems: NavItem[] = [];
  currentUser: AuthResponse | null = null;

  private destroy$ = new Subject<void>();

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.authService.currentUser$
      .pipe(takeUntil(this.destroy$))
      .subscribe(user => {
        this.currentUser = user;
        this.buildNav(user?.role as UserRole | null);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Returns true if this item should show a read-only lock for the current user. */
  isReadOnly(item: NavItem): boolean {
    const role = this.currentUser?.role as UserRole | undefined;
    return !!role && !!item.readOnly?.includes(role);
  }

  logout(): void {
    this.authService.logout();
  }

  private buildNav(role: UserRole | null): void {
    this.visibleItems = NAV_ITEMS.filter(item => {
      // Empty roles array → visible to everyone
      if (item.roles.length === 0) return true;
      return role !== null && item.roles.includes(role);
    });
  }
}
