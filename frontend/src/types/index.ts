export interface User {
  id: string
  email: string
  displayName: string
  createdAt: string
}

export interface AuthResponse {
  token: string
  expiresAt: string
  user: User
}

/** Full note, returned by detail/create/update endpoints. */
export interface Note {
  id: string
  title: string
  content: string | null
  createdAt: string
  updatedAt: string
}

/** List projection: `preview` is a server-trimmed excerpt of the content. */
export interface NoteListItem {
  id: string
  title: string
  preview: string | null
  hasContent: boolean
  createdAt: string
  updatedAt: string
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPrevious: boolean
  hasNext: boolean
}

export type NoteSortField = 'UpdatedAt' | 'CreatedAt' | 'Title'
export type SortDirection = 'Asc' | 'Desc'

/** Mirrors the NoteQuery record bound by GET /api/notes. */
export interface NoteQuery {
  search?: string
  sortBy: NoteSortField
  sortDirection: SortDirection
  createdFrom?: string
  createdTo?: string
  hasContent?: boolean
  page: number
  pageSize: number
}

export interface NotePayload {
  title: string
  content: string | null
}

export interface Credentials {
  email: string
  password: string
}

export interface RegisterPayload extends Credentials {
  displayName: string
}
