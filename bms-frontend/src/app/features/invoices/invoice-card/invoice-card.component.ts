import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import { InvoiceDto, INVOICE_STATUS_LABELS, INVOICE_STATUS_CLASSES, MONTH_NAMES } from '../../../shared/models/invoice.models';
import { EtbCurrencyPipe } from '../../../shared/pipes/etb-currency.pipe';

@Component({
  selector: 'app-invoice-card',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatTooltipModule, EtbCurrencyPipe],
  templateUrl: './invoice-card.component.html',
  styleUrls: ['./invoice-card.component.scss'],
})
export class InvoiceCardComponent {
  @Input() invoice!:    InvoiceDto;
  @Input() canManage  = false;
  @Input() canCancel  = false;

  @Output() view      = new EventEmitter<InvoiceDto>();
  @Output() issue     = new EventEmitter<InvoiceDto>();
  @Output() cancel    = new EventEmitter<InvoiceDto>();
  @Output() recordPayment = new EventEmitter<InvoiceDto>();

  statusLabel(s: string): string { return INVOICE_STATUS_LABELS[s]  ?? s; }
  statusClass(s: string): string { return INVOICE_STATUS_CLASSES[s] ?? 'badge-neutral'; }

  get periodLabel(): string {
    return `${MONTH_NAMES[this.invoice.periodMonth - 1]} ${this.invoice.periodYear}`;
  }

  get isDraft():     boolean { return this.invoice.status === 'Draft';     }
  get isIssued():    boolean { return this.invoice.status === 'Issued';    }
  get isOverdue():   boolean { return this.invoice.status === 'Overdue';   }
  get isPaid():      boolean { return this.invoice.status === 'Paid';      }
  get isCancelled(): boolean { return this.invoice.status === 'Cancelled'; }

  get canIssue():       boolean { return this.canManage && this.isDraft; }
  get canRecordPay():   boolean { return this.canManage && (this.isIssued || this.isOverdue); }
  get canCancelInv():   boolean { return this.canCancel  && !this.isPaid && !this.isCancelled; }

  get dueDateClass(): string {
    if (this.isPaid || this.isCancelled) return 'due-ok';
    const due = new Date(this.invoice.dueDate);
    const now = new Date();
    const diff = Math.ceil((due.getTime() - now.getTime()) / 86_400_000);
    if (diff < 0)  return 'due-overdue';
    if (diff <= 7) return 'due-soon';
    return 'due-ok';
  }
}
