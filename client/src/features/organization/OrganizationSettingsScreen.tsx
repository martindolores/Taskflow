import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import CircularProgress from '@mui/material/CircularProgress'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogContentText from '@mui/material/DialogContentText'
import DialogTitle from '@mui/material/DialogTitle'
import MenuItem from '@mui/material/MenuItem'
import Paper from '@mui/material/Paper'
import Select from '@mui/material/Select'
import Typography from '@mui/material/Typography'
import { extractErrorMessage } from '@/api/errors'
import type { UserRole } from '@/api/authApi'
import type { Member } from '@/api/organizationApi'
import { LabeledField } from '@/components/LabeledField'
import { RoleBadge } from '@/components/RoleBadge'
import { UserAvatar } from '@/components/UserAvatar'
import { useAuth } from '@/features/auth/useAuth'
import {
  useCreateInvitationMutation,
  useDeactivateMemberMutation,
  useInvitationsQuery,
  useMembersQuery,
  useOrganizationQuery,
  useRevokeInvitationMutation,
  useUpdateMemberRoleMutation,
} from './organizationQueries'
import { inviteMemberSchema, type InviteMemberFormValues } from './organizationSchemas'

const statusColors: Record<Member['status'], string> = {
  Active: '#22c55e',
  Invited: '#f59e0b',
  Deactivated: '#52526a',
}

function formatExpiry(expiresAt: string): string {
  return new Date(expiresAt).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

const memberGridColumns = (isAdmin: boolean) =>
  isAdmin ? '1fr 1fr 130px 110px 90px' : '1fr 1fr 130px 110px'

export function OrganizationSettingsScreen() {
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'

  const organizationQuery = useOrganizationQuery()
  const membersQuery = useMembersQuery()
  const invitationsQuery = useInvitationsQuery(isAdmin)

  const createInvitation = useCreateInvitationMutation()
  const revokeInvitation = useRevokeInvitationMutation()
  const updateMemberRole = useUpdateMemberRoleMutation()
  const deactivateMember = useDeactivateMemberMutation()

  const [inviteSuccess, setInviteSuccess] = useState(false)
  const [memberToRemove, setMemberToRemove] = useState<Member | null>(null)

  const inviteForm = useForm<InviteMemberFormValues>({
    resolver: zodResolver(inviteMemberSchema),
    defaultValues: { email: '', role: 'Member' },
  })

  async function onInvite(values: InviteMemberFormValues) {
    try {
      await createInvitation.mutateAsync(values)
      inviteForm.reset()
      setInviteSuccess(true)
    } catch {
      // surfaced via createInvitation.isError/error in the render below
    }
  }

  async function confirmRemove() {
    if (!memberToRemove) {
      return
    }
    try {
      await deactivateMember.mutateAsync(memberToRemove.id)
      setMemberToRemove(null)
    } catch {
      // deactivateMember.isError is available if this needs surfacing later
    }
  }

  const members = membersQuery.data ?? []
  const pendingInvitations = (invitationsQuery.data ?? []).filter(
    (invitation) => invitation.status === 'Pending',
  )
  const orgName = organizationQuery.data?.name ?? 'your organization'

  return (
    <Box sx={{ p: '36px 40px', maxWidth: 820 }}>
      <Box sx={{ mb: 4 }}>
        <Typography variant="h1">Organization settings</Typography>
        <Typography variant="body2" sx={{ color: 'text.disabled', mt: 0.5 }}>
          Manage your team and workspace
        </Typography>
      </Box>

      {isAdmin && (
        <Paper sx={{ borderRadius: '11px', p: '22px 24px', mb: 2.25 }}>
          <Typography sx={{ fontSize: 14, fontWeight: 600, mb: 0.5 }}>
            Invite team member
          </Typography>
          <Typography variant="body2" sx={{ color: 'text.disabled', mb: 2.25 }}>
            New members will receive an email invitation to join {orgName}.
          </Typography>

          {inviteSuccess ? (
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                gap: 1.125,
                bgcolor: 'rgba(34,197,94,0.07)',
                border: '1px solid rgba(34,197,94,0.14)',
                borderRadius: '8px',
                padding: '11px 14px',
              }}
            >
              <svg width="15" height="15" viewBox="0 0 16 16" fill="none">
                <circle cx="8" cy="8" r="7" fill="#22c55e" opacity={0.15} />
                <path
                  d="M5 8l2.2 2.2 3.8-4"
                  stroke="#22c55e"
                  strokeWidth={1.5}
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
              <Typography variant="body2" sx={{ color: '#22c55e', flex: 1 }}>
                Invitation sent successfully.
              </Typography>
              <Button
                onClick={() => setInviteSuccess(false)}
                sx={{ color: 'text.disabled', fontSize: 12, minWidth: 0, p: 0 }}
              >
                Dismiss
              </Button>
            </Box>
          ) : (
            <Box
              component="form"
              noValidate
              onSubmit={(event) => void inviteForm.handleSubmit(onInvite)(event)}
              sx={{ display: 'flex', gap: 1.25, alignItems: 'flex-end', flexWrap: 'wrap' }}
            >
              {createInvitation.isError && (
                <Alert severity="error" sx={{ width: '100%' }}>
                  {extractErrorMessage(createInvitation.error, 'Could not send the invitation')}
                </Alert>
              )}
              <Box sx={{ flex: 1, minWidth: 200 }}>
                <LabeledField
                  label="Email address"
                  placeholder="colleague@company.com"
                  type="email"
                  {...inviteForm.register('email')}
                  error={!!inviteForm.formState.errors.email}
                  helperText={inviteForm.formState.errors.email?.message}
                />
              </Box>
              <Controller
                control={inviteForm.control}
                name="role"
                render={({ field }) => (
                  <LabeledField label="Role" select sx={{ width: 130 }} {...field}>
                    <MenuItem value="Member">Member</MenuItem>
                    <MenuItem value="Admin">Admin</MenuItem>
                  </LabeledField>
                )}
              />
              <Button
                type="submit"
                variant="contained"
                disabled={inviteForm.formState.isSubmitting}
                sx={{ py: 1.25, px: 2.25, fontSize: 13.5, fontWeight: 500, whiteSpace: 'nowrap' }}
              >
                Send invite
              </Button>
            </Box>
          )}
        </Paper>
      )}

      <Paper sx={{ borderRadius: '11px', overflow: 'hidden' }}>
        <Box sx={{ p: '16px 22px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
          <Typography sx={{ fontSize: 14, fontWeight: 600 }}>Team members</Typography>
          <Typography variant="caption" sx={{ display: 'block', mt: 0.25 }}>
            {organizationQuery.data?.memberCount ?? members.length} members in {orgName}
          </Typography>
        </Box>

        {membersQuery.isLoading ? (
          <Box sx={{ p: 4, display: 'flex', justifyContent: 'center' }}>
            <CircularProgress size={22} />
          </Box>
        ) : (
          <>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: memberGridColumns(isAdmin),
                padding: '9px 22px',
                borderBottom: '1px solid rgba(255,255,255,0.055)',
                bgcolor: 'rgba(255,255,255,0.018)',
              }}
            >
              {['Name', 'Email', 'Role', 'Status'].map((label) => (
                <Typography key={label} variant="overline" sx={{ fontSize: 11 }}>
                  {label}
                </Typography>
              ))}
              {isAdmin && (
                <Typography variant="overline" sx={{ fontSize: 11 }}>
                  Actions
                </Typography>
              )}
            </Box>

            {members.map((member) => {
              const isSelf = member.id === user?.id
              const isDeactivated = member.status === 'Deactivated'
              return (
                <Box
                  key={member.id}
                  sx={{
                    display: 'grid',
                    gridTemplateColumns: memberGridColumns(isAdmin),
                    padding: '12px 22px',
                    borderBottom: '1px solid rgba(255,255,255,0.04)',
                    alignItems: 'center',
                    opacity: isDeactivated ? 0.55 : 1,
                  }}
                >
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25 }}>
                    <UserAvatar name={`${member.firstName} ${member.lastName}`} />
                    <Typography sx={{ fontSize: 13.5, fontWeight: 500 }}>
                      {member.firstName} {member.lastName}
                    </Typography>
                  </Box>
                  <Typography sx={{ fontSize: 13, color: 'text.secondary' }}>
                    {member.email}
                  </Typography>
                  {isAdmin && !isSelf && !isDeactivated ? (
                    <Select
                      size="small"
                      value={member.role}
                      onChange={(event) =>
                        void updateMemberRole
                          .mutateAsync({ userId: member.id, role: event.target.value as UserRole })
                          .catch(() => {})
                      }
                      sx={{ width: 110, fontSize: 13 }}
                    >
                      <MenuItem value="Member">Member</MenuItem>
                      <MenuItem value="Admin">Admin</MenuItem>
                    </Select>
                  ) : (
                    <RoleBadge role={member.role} />
                  )}
                  <Typography
                    sx={{ fontSize: 12, fontWeight: 500, color: statusColors[member.status] }}
                  >
                    {member.status}
                  </Typography>
                  {isAdmin && (
                    <Box>
                      {!isSelf && !isDeactivated && (
                        <Button
                          onClick={() => setMemberToRemove(member)}
                          sx={{ fontSize: 12, color: 'error.main', minWidth: 0, p: 0 }}
                        >
                          Remove
                        </Button>
                      )}
                    </Box>
                  )}
                </Box>
              )
            })}
          </>
        )}
      </Paper>

      {isAdmin && (
        <Paper sx={{ borderRadius: '11px', overflow: 'hidden', mt: 2.25 }}>
          <Box sx={{ p: '16px 22px', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
            <Typography sx={{ fontSize: 14, fontWeight: 600 }}>Pending invitations</Typography>
          </Box>

          {invitationsQuery.isLoading ? (
            <Box sx={{ p: 4, display: 'flex', justifyContent: 'center' }}>
              <CircularProgress size={22} />
            </Box>
          ) : pendingInvitations.length === 0 ? (
            <Box sx={{ p: '22px' }}>
              <Typography variant="body2" sx={{ color: 'text.disabled' }}>
                No pending invitations.
              </Typography>
            </Box>
          ) : (
            pendingInvitations.map((invitation) => (
              <Box
                key={invitation.id}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1.5,
                  padding: '12px 22px',
                  borderBottom: '1px solid rgba(255,255,255,0.04)',
                }}
              >
                <Typography sx={{ fontSize: 13.5, flex: 1 }}>{invitation.email}</Typography>
                <RoleBadge role={invitation.role} />
                <Typography sx={{ fontSize: 12, color: 'text.disabled', width: 110 }}>
                  Expires {formatExpiry(invitation.expiresAt)}
                </Typography>
                <Button
                  onClick={() => void revokeInvitation.mutateAsync(invitation.id).catch(() => {})}
                  sx={{ fontSize: 12, color: 'error.main' }}
                >
                  Revoke
                </Button>
              </Box>
            ))
          )}
        </Paper>
      )}

      <Dialog open={!!memberToRemove} onClose={() => setMemberToRemove(null)}>
        <DialogTitle>Remove team member</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Remove {memberToRemove?.firstName} {memberToRemove?.lastName} from the organization?
            They will lose access immediately.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setMemberToRemove(null)}>Cancel</Button>
          <Button
            onClick={() => void confirmRemove()}
            color="error"
            variant="contained"
            disabled={deactivateMember.isPending}
          >
            Remove
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
