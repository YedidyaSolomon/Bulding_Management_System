import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';
import { PaymentDto, CreatePaymentDto } from '../../shared/models/payment.models';

@Injectable({ providedIn: 'root' })
export class PaymentService {

  private readonly apiUrl = `${environment.apiUrl}/payments`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<PaymentDto[]> {
    return this.http
      .get<ApiResponse<PaymentDto[]>>(this.apiUrl)
      .pipe(map(r => r.data ?? []));
  }

  /** All roles: only payments for the calling user's tenants (hits /api/payments/mine) */
  getMyPayments(): Observable<PaymentDto[]> {
    return this.http
      .get<ApiResponse<PaymentDto[]>>(`${this.apiUrl}/mine`)
      .pipe(map(r => r.data ?? []));
  }

  getById(id: number): Observable<PaymentDto> {
    return this.http
      .get<ApiResponse<PaymentDto>>(`${this.apiUrl}/${id}`)
      .pipe(map(r => r.data!));
  }

  getByInvoice(invoiceId: number): Observable<PaymentDto[]> {
    return this.http
      .get<ApiResponse<PaymentDto[]>>(`${this.apiUrl}/by-invoice/${invoiceId}`)
      .pipe(map(r => r.data ?? []));
  }

  /** POST /api/payments — record a payment against an invoice */
  record(dto: CreatePaymentDto): Observable<PaymentDto> {
    return this.http
      .post<ApiResponse<PaymentDto>>(this.apiUrl, dto)
      .pipe(map(r => r.data!));
  }
}
