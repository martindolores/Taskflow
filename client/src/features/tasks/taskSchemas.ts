import { z } from 'zod'

export const taskFormSchema = z.object({
  title: z.string().min(1, 'Title is required').max(200, 'Title must be 200 characters or fewer'),
  description: z.string().max(5000, 'Description is too long').optional(),
  status: z.enum(['ToDo', 'InProgress', 'Done']),
  priority: z.enum(['Low', 'Medium', 'High']),
  assigneeId: z.string().nullable(),
  dueDate: z.string().optional(),
})

export type TaskFormValues = z.infer<typeof taskFormSchema>

export const commentFormSchema = z.object({
  body: z.string().min(1, 'Comment cannot be empty').max(2000, 'Comment is too long'),
})

export type CommentFormValues = z.infer<typeof commentFormSchema>
