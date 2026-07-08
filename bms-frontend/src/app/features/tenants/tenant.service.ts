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
  LinkTenantUserDto,
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

  /** Viewer: only tenants linked to the calling user */
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

  /** Create a tenant. dto.appUserId is optional — omit to create unlinked. */
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
   * PUT /api/tenants/{id}/link-user
   * Link an existing tenant to a registered Viewer account.
   * Pass force=true to overwrite an existing link (triggers confirm dialog in UI).
   */
  linkUser(tenantId: number, dto: LinkTenantUserDto): Observable<TenantDto> {
    return this.http
      .put<ApiResponse<TenantDto>>(`${this.apiUrl}/${tenantId}/link-user`, dto)
      .pipe(map(r => r.data!));
  }

  /**
   * GET /api/tenants/registered-users — Admin/Manager only.
   * Returns id + email + fullName of every active registered user.
   * Used by the tenant form's user-picker to let admin select by name/email
   * and submit the user's Id as AppUserId.
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
