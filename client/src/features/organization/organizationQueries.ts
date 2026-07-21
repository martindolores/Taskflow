import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import * as organizationApi from '@/api/organizationApi'
import type { CreateInvitationPayload } from '@/api/organizationApi'
import type { UserRole } from '@/api/authApi'
import { showToast } from '@/components/toast'

export const organizationKeys = {
  detail: ['organization', 'detail'] as const,
  members: ['organization', 'members'] as const,
  invitations: ['organization', 'invitations'] as const,
}

export function useOrganizationQuery() {
  return useQuery({ queryKey: organizationKeys.detail, queryFn: organizationApi.getOrganization })
}

export function useMembersQuery() {
  return useQuery({ queryKey: organizationKeys.members, queryFn: organizationApi.getMembers })
}

export function useInvitationsQuery(enabled: boolean) {
  return useQuery({
    queryKey: organizationKeys.invitations,
    queryFn: organizationApi.getInvitations,
    enabled,
  })
}

export function useCreateInvitationMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (payload: CreateInvitationPayload) => organizationApi.createInvitation(payload),
    meta: { suppressErrorToast: true },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: organizationKeys.invitations })
    },
  })
}

export function useRevokeInvitationMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => organizationApi.revokeInvitation(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: organizationKeys.invitations })
      showToast('Invitation revoked')
    },
  })
}

export function useUpdateMemberRoleMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ userId, role }: { userId: string; role: UserRole }) =>
      organizationApi.updateMemberRole(userId, role),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: organizationKeys.members })
      showToast('Role updated')
    },
  })
}

export function useDeactivateMemberMutation() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (userId: string) => organizationApi.deactivateMember(userId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: organizationKeys.members })
      showToast('Member removed')
    },
  })
}
