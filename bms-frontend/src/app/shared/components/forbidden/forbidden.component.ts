import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule],
  template: `
    <div class="forbidden-container">
      <div class="forbidden-content">
        <div class="forbidden-icon">
          <mat-icon>block</mat-icon>
        </div>
        <h1>403</h1>
        <h2>Access Denied</h2>
        <p>You don't have permission to view this page. Please contact your administrator if you believe this is an error.</p>
        <button mat-raised-button color="primary" routerLink="/dashboard">
          <mat-icon>arrow_back</mat-icon>
          Back to Dashboard
        </button>
      </div>
    </div>
  `,
  styles: [`
    .forbidden-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: calc(100vh - 64px);
      padding: 24px;
    }
    .forbidden-content {
      text-align: center;
      max-width: 400px;
    }
    .forbidden-icon {
      width: 80px;
      height: 80px;
      border-radius: 50%;
      background: #FEE2E2;
      display: flex;
      align-items: center;
      justify-content: center;
      margin: 0 auto 16px;
      mat-icon {
        font-size: 40px;
        width: 40px;
        height: 40px;
        color: #EF4444;
      }
    }
    h1 { font-size: 72px; font-weight: 800; color: #EF4444; margin: 0 0 4px; }
    h2 { font-size: 22px; font-weight: 600; color: var(--bms-text-primary); margin: 0 0 12px; }
    p  { color: var(--bms-text-secondary); font-size: 15px; margin: 0 0 28px; }
    button mat-icon { margin-right: 8px; }
  `]
})
export class ForbiddenComponent {}
