import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import {
  trigger,
  animateChild,
  group,
  query,
  style,
  animate,
  transition,
} from '@angular/animations';

/**
 * Slide-fade animation for child route transitions inside the right panel.
 * The outgoing form slides left + fades; the incoming form slides in from
 * the right + fades.  Total duration: 180 ms — snappy but perceptible.
 */
export const authRouteAnimation = trigger('authRouteAnimation', [
  transition('* <=> *', [
    // Both host elements are initially stacked via position:relative/absolute
    style({ position: 'relative' }),
    query(':enter, :leave', [
      style({
        position: 'absolute',
        top: 0, left: 0, right: 0,
      }),
    ], { optional: true }),

    // Outgoing form: slide left + fade out
    query(':leave', [
      animate('180ms ease-in', style({ opacity: 0, transform: 'translateX(-24px)' })),
    ], { optional: true }),

    // Incoming form: start right + faded, slide in + fade up
    query(':enter', [
      style({ opacity: 0, transform: 'translateX(24px)' }),
      animate('180ms ease-out', style({ opacity: 1, transform: 'translateX(0)' })),
    ], { optional: true }),

    query(':enter', animateChild(), { optional: true }),
  ]),
]);

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, MatIconModule],
  templateUrl: './auth-layout.component.html',
  styleUrls: ['./auth-layout.component.scss'],
  animations: [authRouteAnimation],
})
export class AuthLayoutComponent {
  /**
   * Returns a stable string key for the currently activated child route —
   * used as the value bound to [@authRouteAnimation] so Angular knows when
   * to fire the transition.
   */
  getRouteState(outlet: RouterOutlet): string {
    return outlet.isActivated ? (outlet.activatedRoute.snapshot.url[0]?.path ?? '') : '';
  }
}
