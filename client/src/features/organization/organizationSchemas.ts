import { z } from 'zod'

export const inviteMemberSchema = z.object({
  email: z.string().min(1, 'Email is required').email('Enter a valid email address').max(320),
  role: z.enum(['Admin', 'Member']),
})

export type InviteMemberFormValues = z.infer<typeof inviteMemberSchema>
