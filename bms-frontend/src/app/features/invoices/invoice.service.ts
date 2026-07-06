import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';
import { InvoiceDto, CreateInvoiceDto } from '../../shared/models/invoice.models';

@Injectable({ providedIn: 'root' })
export class InvoiceService {

  private readonly apiUrl = `${environment.apiUrl}/invoices`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<InvoiceDto[]> {
    return this.http
      .get<ApiResponse<InvoiceDto[]>>(this.apiUrl)
      .pipe(map(r => r.data ?? []));
  }

  /** All roles: only invoices for the calling user's tenants (hits /api/invoices/mine) */
  getMyInvoices(): Observable<InvoiceDto[]> {
    return this.http
      .get<ApiResponse<InvoiceDto[]>>(`${this.apiUrl}/mine`)
      .pipe(map(r => r.data ?? []));
  }

  getById(id: number): Observable<InvoiceDto> {
    return this.http
      .get<ApiResponse<InvoiceDto>>(`${this.apiUrl}/${id}`)
      .pipe(map(r => r.data!));
  }

  getByLease(leaseId: number): Observable<InvoiceDto[]> {
    return this.http
      .get<ApiResponse<InvoiceDto[]>>(`${this.apiUrl}/by-lease/${leaseId}`)
      .pipe(map(r => r.data ?? []));
  }

  getOverdue(): Observable<InvoiceDto[]> {
    return this.http
      .get<ApiResponse<InvoiceDto[]>>(`${this.apiUrl}/overdue`)
      .pipe(map(r => r.data ?? []));
  }

  /** POST /api/invoices/generate */
  generate(dto: CreateInvoiceDto): Observable<InvoiceDto> {
    return this.http
      .post<ApiResponse<InvoiceDto>>(`${this.apiUrl}/generate`, dto)
      .pipe(map(r => r.data!));
  }

  /** PUT /api/invoices/{id}/issue — Draft → Issued */
  issue(id: number): Observable<InvoiceDto> {
    return this.http
      .put<ApiResponse<InvoiceDto>>(`${this.apiUrl}/${id}/issue`, {})
      .pipe(map(r => r.data!));
  }

  /** PUT /api/invoices/{id}/cancel */
  cancel(id: number): Observable<InvoiceDto> {
    return this.http
      .put<ApiResponse<InvoiceDto>>(`${this.apiUrl}/${id}/cancel`, {})
      .pipe(map(r => r.data!));
  }
}
