import { http } from '@/lib/http'
import type { AuthResponse, Credentials, RegisterPayload, User } from '@/types'

export const authService = {
  async register(payload: RegisterPayload): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/auth/register', payload)
    return data
  },

  async login(credentials: Credentials): Promise<AuthResponse> {
    const { data } = await http.post<AuthResponse>('/auth/login', credentials)
    return data
  },

  async me(): Promise<User> {
    const { data } = await http.get<User>('/auth/me')
    return data
  },
}
