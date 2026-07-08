// ── Tenant DTOs — mirrors backend BMS.Application.DTOs.Tenants exactly ──────

export interface TenantDto {
  id:                number;
  /** AppUser.Id of the linked user account. Null when not yet linked. */
  appUserId:         string | null;
  /** Email of the linked user account — null when not yet linked. */
  userEmail:         string | null;
  organizationName:  string;
  tin:               string;
  phone:             string;
  businessType:      string;
  contactPersonName: string;
  contactEmail:      string;
  isActive:          boolean;
}

export interface CreateTenantDto {
  /** Optional — AppUser.Id of a registered Viewer account to link as tenant owner.
   *  Omit (null/undefined) to create an unlinked tenant (link later via PUT link-user). */
  appUserId?:        string | null;
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

export interface LinkTenantUserDto {
  appUserId: string;
  /** Pass true to overwrite an existing link. Backend rejects without this flag. */
  force:     boolean;
}

/** Lightweight record returned by GET /api/tenants/registered-users.
 *  Includes id so the frontend can submit AppUserId directly. */
export interface RegisteredUserDto {
  id:       string;
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
