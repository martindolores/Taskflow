import { createContext } from 'react'
import type { CurrentUser, LoginPayload, RegisterPayload } from '@/api/authApi'

export interface AuthContextValue {
  user: CurrentUser | null
  isLoading: boolean
  login: (payload: LoginPayload) => Promise<void>
  register: (payload: RegisterPayload) => Promise<void>
  logout: () => Promise<void>
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
