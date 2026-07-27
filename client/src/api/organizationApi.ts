import { apiClient } from './client'
import type { UserRole } from './authApi'

export type MemberStatus = 'Invited' | 'Active' | 'Deactivated'
export type InvitationStatus = 'Pending' | 'Accepted' | 'Revoked' | 'Expired'

export interface Organization {
  id: string
  name: string
  slug: string
  memberCount: number
}

export interface Member {
  id: string
  email: string
  firstName: string
  lastName: string
  role: UserRole
  status: MemberStatus
}

export interface Invitation {
  id: string
  email: string
  role: UserRole
  status: InvitationStatus
  expiresAt: string
  token: string
  emailSent: boolean
}

export interface CreateInvitationPayload {
  email: string
  role: UserRole
}

export async function getOrganization(): Promise<Organization> {
  const { data } = await apiClient.get<Organization>('/api/organization')
  return data
}

export async function getMembers(): Promise<Member[]> {
  const { data } = await apiClient.get<Member[]>('/api/organization/members')
  return data
}

export async function getInvitations(): Promise<Invitation[]> {
  const { data } = await apiClient.get<Invitation[]>('/api/organization/invitations')
  return data
}

export async function createInvitation(payload: CreateInvitationPayload): Promise<Invitation> {
  const { data } = await apiClient.post<Invitation>('/api/organization/invitations', payload)
  return data
}

export async function revokeInvitation(id: string): Promise<void> {
  await apiClient.delete(`/api/organization/invitations/${id}`)
}

export async function updateMemberRole(
  userId: string,
  role: UserRole,
): Promise<{ id: string; role: UserRole }> {
  const { data } = await apiClient.patch<{ id: string; role: UserRole }>(
    `/api/organization/members/${userId}/role`,
    { role },
  )
  return data
}

export async function deactivateMember(userId: string): Promise<void> {
  await apiClient.delete(`/api/organization/members/${userId}`)
}
