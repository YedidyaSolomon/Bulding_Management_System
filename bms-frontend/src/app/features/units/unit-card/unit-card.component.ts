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
      Available:        'badge-success',
      Occupied:         'badge-info',
      UnderMaintenance: 'badge-warning',
      Reserved:         'badge-neutral',
    };
    return map[s] ?? 'badge-neutral';
  }
}
