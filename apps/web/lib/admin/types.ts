export type UserRole = "Parent" | "Athlete" | "Coach" | "Admin" | "SuperAdmin";

export interface CurrentUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    role: UserRole;
  };
}

export interface AdminAnnouncement {
  id: string;
  title: string;
  slug: string;
  summary: string;
  body: string;
  imageUrl: string | null;
  isFeatured: boolean;
  isPublished: boolean;
  publishDateUtc: string | null;
  expirationDateUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface AnnouncementWriteRequest {
  title: string;
  summary: string;
  body: string;
  imageUrl: string | null;
  isFeatured: boolean;
  isPublished: boolean;
  publishDateUtc: string | null;
  expirationDateUtc: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

export interface AnnouncementFilters {
  search?: string;
  isPublished?: string;
  isFeatured?: string;
  includeExpired?: string;
  page?: string;
}

export type AnnouncementState = "Draft" | "Scheduled" | "Published" | "Expired";
