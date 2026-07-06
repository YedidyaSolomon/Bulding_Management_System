import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { TenantDto } from '../../../shared/models/tenant.models';

@Component({
  selector: 'app-tenant-card',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './tenant-card.component.html',
  styleUrls: ['./tenant-card.component.scss'],
})
export class TenantCardComponent {
  @Input() tenant!:    TenantDto;
  @Input() canEdit   = false;
  @Input() canDelete = false;

  @Output() view   = new EventEmitter<TenantDto>();
  @Output() edit   = new EventEmitter<TenantDto>();
  @Output() delete = new EventEmitter<TenantDto>();

  statusClass(isActive: boolean): string {
    return isActive ? 'badge-success' : 'badge-warning';
  }

  statusLabel(isActive: boolean): string {
    return isActive ? 'Active' : 'Inactive';
  }

  /** Returns first two letters of organization name for the avatar */
  get initials(): string {
    const words = this.tenant.organizationName.trim().split(/\s+/);
    if (words.length >= 2) {
      return (words[0][0] + words[1][0]).toUpperCase();
    }
    return this.tenant.organizationName.substring(0, 2).toUpperCase();
  }
}
