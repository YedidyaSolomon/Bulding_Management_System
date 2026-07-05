import { Pipe, PipeTransform } from '@angular/core';

/**
 * EtbCurrencyPipe
 * Formats a number as Ethiopian Birr (ETB).
 * Output example: 1,500 ETB
 */
@Pipe({
  name: 'etbCurrency',
  standalone: true,
})
export class EtbCurrencyPipe implements PipeTransform {
  transform(value: number | null | undefined): string {
    if (value == null || isNaN(value)) return '— ETB';
    const formatted = new Intl.NumberFormat('en-ET', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(value);
    return `${formatted} ETB`;
  }
}
