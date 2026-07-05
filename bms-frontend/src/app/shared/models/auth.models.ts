/**
 * Mirrors the backend Auth DTOs exactly.
 */

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  fullName: string;
  email: string;
  role: string;
  expiry: string; // ISO datetime string
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors: string[] | null;
}

export type UserRole = 'Admin' | 'Manager' | 'Viewer';
