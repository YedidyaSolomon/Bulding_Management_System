import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';
import { CreateUnitDto, UnitDto, UpdateUnitDto } from '../../shared/models/unit.models';

@Injectable({ providedIn: 'root' })
export class UnitService {
  private readonly url = `${environment.apiUrl}/units`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<UnitDto[]> {
    return this.http
      .get<ApiResponse<UnitDto[]>>(this.url)
      .pipe(map(r => r.data ?? []));
  }

  getById(id: number): Observable<UnitDto> {
    return this.http
      .get<ApiResponse<UnitDto>>(`${this.url}/${id}`)
      .pipe(map(r => r.data!));
  }

  create(dto: CreateUnitDto): Observable<UnitDto> {
    return this.http
      .post<ApiResponse<UnitDto>>(this.url, dto)
      .pipe(map(r => r.data!));
  }

  update(id: number, dto: UpdateUnitDto): Observable<UnitDto> {
    return this.http
      .put<ApiResponse<UnitDto>>(`${this.url}/${id}`, dto)
      .pipe(map(r => r.data!));
  }

  delete(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<unknown>>(`${this.url}/${id}`)
      .pipe(map(() => void 0));
  }
}
