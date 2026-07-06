// ── Tenant DTOs — mirrors backend BMS.Application.DTOs.Tenants exactly ──────

export interface TenantDto {
  id:                number;
  /** AppUser.Id the tenant is linked to */
  userId:            string;
  /** Email of the linked user account */
  userEmail:         string;
  organizationName:  string;
  tin:               string;
  phone:             string;
  businessType:      string;
  contactPersonName: string;
  contactEmail:      string;
  isActive:          boolean;
}

export interface CreateTenantDto {
  /** Email of an already-registered user account — resolved to UserId on the backend */
  userEmail:         string;
  organizationName:  string;
  tin:               string;
  phone:             string;
  businessType:      string;
  contactPersonName: string;
  contactEmail:      string;
}

export interface UpdateTenantDto {
  organizationName:  string;
  tin:               string;
  phone:             string;
  businessType:      string;
  contactPersonName: string;
  contactEmail:      string;
  isActive:          boolean;
}

/** Lightweight record returned by GET /api/tenants/registered-users */
export interface RegisteredUserDto {
  email:    string;
  fullName: string;
}

// ── Legal Document DTOs ──────────────────────────────────────────────────────

export interface LegalDocumentDto {
  id:           number;
  tenantId:     number;
  documentType: string;
  filePath:     string;
  uploadedAt:   string;   // ISO date string
  expiryDate:   string | null;
  isVerified:   boolean;
}

export interface CreateLegalDocumentDto {
  tenantId:     number;
  documentType: string;
  filePath:     string;
  expiryDate:   string | null;
}

// ── Constants ────────────────────────────────────────────────────────────────

export const BUSINESS_TYPES = [
  'Retail',
  'Restaurant',
  'Office',
  'Services',
  'Healthcare',
  'Education',
  'Other',
] as const;

export const DOCUMENT_TYPES = [
  'BusinessLicense',
  'TradeLicense',
  'TaxClearance',
  'NationalID',
  'LeaseContract',
  'Other',
] as const;

export type BusinessType  = typeof BUSINESS_TYPES[number];
export type DocumentType  = typeof DOCUMENT_TYPES[number];

// ── Display helpers ──────────────────────────────────────────────────────────

export const DOCUMENT_TYPE_LABELS: Record<string, string> = {
  BusinessLicense: 'Business License',
  TradeLicense:    'Trade License',
  TaxClearance:    'Tax Clearance',
  NationalID:      'National ID',
  LeaseContract:   'Lease Contract',
  Other:           'Other',
};
