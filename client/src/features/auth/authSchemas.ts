import { z } from 'zod'

export const loginSchema = z.object({
  email: z.string().min(1, 'Email is required').email('Enter a valid email address'),
  password: z.string().min(1, 'Password is required'),
})

export type LoginFormValues = z.infer<typeof loginSchema>

export const registerSchema = z.object({
  organizationName: z.string().min(1, 'Organization name is required').max(200),
  firstName: z.string().min(1, 'First name is required').max(100),
  lastName: z.string().min(1, 'Last name is required').max(100),
  email: z.string().min(1, 'Email is required').email('Enter a valid email address').max(320),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

export type RegisterFormValues = z.infer<typeof registerSchema>

export const acceptInvitationSchema = z.object({
  firstName: z.string().min(1, 'First name is required').max(100),
  lastName: z.string().min(1, 'Last name is required').max(100),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

export type AcceptInvitationFormValues = z.infer<typeof acceptInvitationSchema>
