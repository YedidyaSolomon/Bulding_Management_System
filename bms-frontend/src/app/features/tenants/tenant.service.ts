import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';
import {
  TenantDto,
  CreateTenantDto,
  UpdateTenantDto,
  LegalDocumentDto,
  CreateLegalDocumentDto,
  RegisteredUserDto,
} from '../../shared/models/tenant.models';

@Injectable({ providedIn: 'root' })
export class TenantService {

  private readonly apiUrl = `${environment.apiUrl}/tenants`;

  constructor(private http: HttpClient) {}

  // ── Tenants ────────────────────────────────────────────────────────────────

  /** Admin/Manager: all tenants */
  getAll(): Observable<TenantDto[]> {
    return this.http
      .get<ApiResponse<TenantDto[]>>(this.apiUrl)
      .pipe(map(r => r.data ?? []));
  }

  /** All roles: only tenants linked to the calling user */
  getMyTenants(): Observable<TenantDto[]> {
    return this.http
      .get<ApiResponse<TenantDto[]>>(`${this.apiUrl}/mine`)
      .pipe(map(r => r.data ?? []));
  }

  getById(id: number): Observable<TenantDto> {
    return this.http
      .get<ApiResponse<TenantDto>>(`${this.apiUrl}/${id}`)
      .pipe(map(r => r.data!));
  }

  create(dto: CreateTenantDto): Observable<TenantDto> {
    return this.http
      .post<ApiResponse<TenantDto>>(this.apiUrl, dto)
      .pipe(map(r => r.data!));
  }

  update(id: number, dto: UpdateTenantDto): Observable<TenantDto> {
    return this.http
      .put<ApiResponse<TenantDto>>(`${this.apiUrl}/${id}`, dto)
      .pipe(map(r => r.data!));
  }

  delete(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<null>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }

  /**
   * Admin/Manager: list of all registered user accounts (email + fullName).
   * Used by the tenant-creation form so the admin can pick/validate a user email.
   */
  getRegisteredUsers(): Observable<RegisteredUserDto[]> {
    return this.http
      .get<ApiResponse<RegisteredUserDto[]>>(`${this.apiUrl}/registered-users`)
      .pipe(map(r => r.data ?? []));
  }

  // ── Legal Documents ────────────────────────────────────────────────────────

  getDocuments(tenantId: number): Observable<LegalDocumentDto[]> {
    return this.http
      .get<ApiResponse<LegalDocumentDto[]>>(`${this.apiUrl}/${tenantId}/documents`)
      .pipe(map(r => r.data ?? []));
  }

  addDocument(tenantId: number, dto: CreateLegalDocumentDto): Observable<LegalDocumentDto> {
    return this.http
      .post<ApiResponse<LegalDocumentDto>>(`${this.apiUrl}/${tenantId}/documents`, dto)
      .pipe(map(r => r.data!));
  }
}
