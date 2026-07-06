import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';
import {
  LeaseDto,
  CreateLeaseDto,
  UpdateLeaseDto,
  TerminateLeaseDto,
} from '../../shared/models/lease.models';

@Injectable({ providedIn: 'root' })
export class LeaseService {

  private readonly apiUrl = `${environment.apiUrl}/leases`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<LeaseDto[]> {
    return this.http
      .get<ApiResponse<LeaseDto[]>>(this.apiUrl)
      .pipe(map(r => r.data ?? []));
  }

  getById(id: number): Observable<LeaseDto> {
    return this.http
      .get<ApiResponse<LeaseDto>>(`${this.apiUrl}/${id}`)
      .pipe(map(r => r.data!));
  }

  getByTenant(tenantId: number): Observable<LeaseDto[]> {
    return this.http
      .get<ApiResponse<LeaseDto[]>>(`${this.apiUrl}/by-tenant/${tenantId}`)
      .pipe(map(r => r.data ?? []));
  }

  getByUnit(unitId: number): Observable<LeaseDto[]> {
    return this.http
      .get<ApiResponse<LeaseDto[]>>(`${this.apiUrl}/by-unit/${unitId}`)
      .pipe(map(r => r.data ?? []));
  }

  create(dto: CreateLeaseDto): Observable<LeaseDto> {
    return this.http
      .post<ApiResponse<LeaseDto>>(this.apiUrl, dto)
      .pipe(map(r => r.data!));
  }

  /** Renew / extend a lease — PUT /api/leases/{id}/renew */
  renew(id: number, dto: UpdateLeaseDto): Observable<LeaseDto> {
    return this.http
      .put<ApiResponse<LeaseDto>>(`${this.apiUrl}/${id}/renew`, dto)
      .pipe(map(r => r.data!));
  }

  /** Terminate a lease early — PUT /api/leases/{id}/terminate */
  terminate(id: number, dto: TerminateLeaseDto): Observable<void> {
    return this.http
      .put<ApiResponse<null>>(`${this.apiUrl}/${id}/terminate`, dto)
      .pipe(map(() => void 0));
  }
}
