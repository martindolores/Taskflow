import { z } from 'zod'

/** The fixed swatch palette from the design prototype — the only selectable colors. */
export const PROJECT_COLORS = [
  '#6366f1',
  '#22d3ee',
  '#f59e0b',
  '#22c55e',
  '#f43f5e',
  '#a78bfa',
] as const

export const projectFormSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must be 100 characters or fewer'),
  description: z.string().max(280, 'Description must be 280 characters or fewer').optional(),
  color: z.enum(PROJECT_COLORS),
})

export type ProjectFormValues = z.infer<typeof projectFormSchema>
