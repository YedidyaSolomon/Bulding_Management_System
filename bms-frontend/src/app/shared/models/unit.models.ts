export interface UnitDto {
  id:           number;
  floorNumber:  number;
  unitNumber:   string;
  unitType:     string;
  areaSqMeters: number;
  monthlyRent:  number;
  status:       string;
  description:  string | null;
}

export interface CreateUnitDto {
  floorNumber:  number;
  unitNumber:   string;
  unitType:     string;
  areaSqMeters: number;
  monthlyRent:  number;
  description:  string | null;
}

export interface UpdateUnitDto {
  floorNumber:  number;
  unitNumber:   string;
  unitType:     string;
  areaSqMeters: number;
  monthlyRent:  number;
  status:       string;
  description:  string | null;
}

export const UNIT_TYPES   = ['Shop', 'Office'] as const;
export const UNIT_STATUSES = ['Available', 'Occupied', 'UnderMaintenance', 'Reserved'] as const;

export type UnitType   = typeof UNIT_TYPES[number];
export type UnitStatus = typeof UNIT_STATUSES[number];
