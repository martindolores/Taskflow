import { apiClient } from './client'

export type UserRole = 'Admin' | 'Member'

export interface CurrentUser {
  id: string
  email: string
  firstName: string
  lastName: string
  role: UserRole
  organizationId: string
  organizationName: string
}

export interface AuthTokens {
  accessToken: string
  refreshToken: string
}

export interface RegisterPayload {
  organizationName: string
  email: string
  password: string
  firstName: string
  lastName: string
}

export interface LoginPayload {
  email: string
  password: string
}

export async function register(payload: RegisterPayload): Promise<AuthTokens> {
  const { data } = await apiClient.post<AuthTokens>('/api/auth/register', payload)
  return data
}

export async function login(payload: LoginPayload): Promise<AuthTokens> {
  const { data } = await apiClient.post<AuthTokens>('/api/auth/login', payload)
  return data
}

export async function logout(refreshToken: string): Promise<void> {
  await apiClient.post('/api/auth/logout', { refreshToken })
}

export async function getCurrentUser(): Promise<CurrentUser> {
  const { data } = await apiClient.get<CurrentUser>('/api/users/me')
  return data
}

export interface AcceptInvitationPayload {
  token: string
  password: string
  firstName: string
  lastName: string
}

export async function acceptInvitation(payload: AcceptInvitationPayload): Promise<AuthTokens> {
  const { data } = await apiClient.post<AuthTokens>('/api/auth/accept-invitation', payload)
  return data
}
