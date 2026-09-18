import { http } from '@/lib/http'
import type { Note, NoteListItem, NotePayload, NoteQuery, PagedResult } from '@/types'

/** Strips empty values so the query string stays readable and the API uses its defaults. */
function toParams(query: NoteQuery): Record<string, string | number | boolean> {
  const params: Record<string, string | number | boolean> = {
    sortBy: query.sortBy,
    sortDirection: query.sortDirection,
    page: query.page,
    pageSize: query.pageSize,
  }

  if (query.search) params.search = query.search
  if (query.createdFrom) params.createdFrom = query.createdFrom
  if (query.createdTo) params.createdTo = query.createdTo
  if (typeof query.hasContent === 'boolean') params.hasContent = query.hasContent

  return params
}

export const noteService = {
  async list(query: NoteQuery, signal?: AbortSignal): Promise<PagedResult<NoteListItem>> {
    const { data } = await http.get<PagedResult<NoteListItem>>('/notes', {
      params: toParams(query),
      signal,
    })
    return data
  },

  async get(id: string): Promise<Note> {
    const { data } = await http.get<Note>(`/notes/${id}`)
    return data
  },

  async create(payload: NotePayload): Promise<Note> {
    const { data } = await http.post<Note>('/notes', payload)
    return data
  },

  async update(id: string, payload: NotePayload): Promise<Note> {
    const { data } = await http.put<Note>(`/notes/${id}`, payload)
    return data
  },

  async remove(id: string): Promise<void> {
    await http.delete(`/notes/${id}`)
  },
}
