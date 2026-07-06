import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UnitDto } from '../../../shared/models/unit.models';
import { EtbCurrencyPipe } from '../../../shared/pipes/etb-currency.pipe';

@Component({
  selector: 'app-unit-card',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule, EtbCurrencyPipe],
  templateUrl: './unit-card.component.html',
  styleUrls: ['./unit-card.component.scss']
})
export class UnitCardComponent {
  @Input() unit!: UnitDto;
  @Input() canEdit = false;
  @Input() canDelete = false;

  @Output() view = new EventEmitter<UnitDto>();
  @Output() edit = new EventEmitter<UnitDto>();
  @Output() delete = new EventEmitter<UnitDto>();

  statusLabel(s: string): string {
    const map: Record<string, string> = {
      Available:        'Available',
      Occupied:         'Occupied',
      UnderMaintenance: 'Under Maintenance',
      Reserved:         'Reserved',
    };
    return map[s] ?? s;
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      Available:        'badge-success',    // green
      Occupied:         'badge-info',       // blue
      UnderMaintenance: 'badge-warning',    // amber
      Reserved:         'badge-reserved',   // purple (defined in component SCSS)
    };
    return map[s] ?? 'badge-neutral';
  }

  /** Returns the Material icon name for a given unit type. */
  unitTypeIcon(type: string): string {
    const map: Record<string, string> = {
      Shop:   'storefront',
      Office: 'business_center',
    };
    return map[type] ?? 'apartment';
  }
}
