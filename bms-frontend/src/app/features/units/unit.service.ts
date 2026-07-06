import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../../shared/models/auth.models';
import { UnitDto, CreateUnitDto, UpdateUnitDto } from '../../shared/models/unit.models';

/** Building-wide constraints — single source of truth for the frontend. */
export const MAX_FLOORS          = 7;
export const MAX_UNITS_PER_FLOOR = 3;

/** Maps floor number → count of existing units on that floor. */
export type FloorCapacityMap = Record<number, number>;

@Injectable({ providedIn: 'root' })
export class UnitService {

  private readonly apiUrl = `${environment.apiUrl}/units`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<UnitDto[]> {
    return this.http
      .get<ApiResponse<UnitDto[]>>(this.apiUrl)
      .pipe(map(r => r.data ?? []));
  }

  getById(id: number): Observable<UnitDto> {
    return this.http
      .get<ApiResponse<UnitDto>>(`${this.apiUrl}/${id}`)
      .pipe(map(r => r.data!));
  }

  create(dto: CreateUnitDto): Observable<UnitDto> {
    return this.http
      .post<ApiResponse<UnitDto>>(this.apiUrl, dto)
      .pipe(map(r => r.data!));
  }

  update(id: number, dto: UpdateUnitDto): Observable<UnitDto> {
    return this.http
      .put<ApiResponse<UnitDto>>(`${this.apiUrl}/${id}`, dto)
      .pipe(map(r => r.data!));
  }

  delete(id: number): Observable<void> {
    return this.http
      .delete<ApiResponse<null>>(`${this.apiUrl}/${id}`)
      .pipe(map(() => void 0));
  }

  /**
   * Derives a floor-capacity map from an already-loaded list of units.
   * Avoids an extra HTTP call — callers pass in their cached unit list.
   *
   * @param units     Full list of units already in memory.
   * @param excludeId Omit this unit from the count (used when editing, so the
   *                  unit being moved doesn't count against its current floor).
   */
  buildFloorCapacityMap(units: UnitDto[], excludeId?: number): FloorCapacityMap {
    const result: FloorCapacityMap = {};
    for (let f = 1; f <= MAX_FLOORS; f++) {
      result[f] = units.filter(
        u => u.floorNumber === f && u.id !== excludeId,
      ).length;
    }
    return result;
  }
}
