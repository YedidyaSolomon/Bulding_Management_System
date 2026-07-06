import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from '../../../environments/environment';
import {
  ApiResponse,
  OccupancyReport,
  RevenueReport,
  ArrearsReport,
  LeaseExpiryReport,
  DocumentExpiryReport,
} from './report.models';

export type ReportType =
  | 'occupancy'
  | 'revenue'
  | 'arrears'
  | 'lease-expiry'
  | 'document-expiry';

@Injectable({ providedIn: 'root' })
export class ReportService {

  private readonly base = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  // ── Report fetchers ───────────────────────────────────────────────────────

  getOccupancy(): Observable<OccupancyReport> {
    return this.http
      .get<ApiResponse<OccupancyReport>>(`${this.base}/occupancy`)
      .pipe(map(r => r.data!));
  }

  getRevenue(): Observable<RevenueReport> {
    return this.http
      .get<ApiResponse<RevenueReport>>(`${this.base}/revenue`)
      .pipe(map(r => r.data!));
  }

  getArrears(): Observable<ArrearsReport> {
    return this.http
      .get<ApiResponse<ArrearsReport>>(`${this.base}/arrears`)
      .pipe(map(r => r.data!));
  }

  getLeaseExpiry(daysAhead = 30): Observable<LeaseExpiryReport> {
    return this.http
      .get<ApiResponse<LeaseExpiryReport>>(
        `${this.base}/lease-expiry?daysAhead=${daysAhead}`
      )
      .pipe(map(r => r.data!));
  }

  getDocumentExpiry(daysAhead = 30): Observable<DocumentExpiryReport> {
    return this.http
      .get<ApiResponse<DocumentExpiryReport>>(
        `${this.base}/document-expiry?daysAhead=${daysAhead}`
      )
      .pipe(map(r => r.data!));
  }

  // ── Export ────────────────────────────────────────────────────────────────

  /**
   * Triggers a file download for the given report type.
   * The backend returns an Excel (.xlsx) binary; we create a temporary
   * object URL and click it so the browser downloads the file.
   */
  exportToExcel(type: ReportType): Observable<Blob> {
    return this.http.get(`${this.base}/export/${type}`, {
      responseType: 'blob',
    });
  }
}
