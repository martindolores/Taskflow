import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate, useParams } from 'react-router-dom'
import { useTheme } from '@mui/material/styles'
import useMediaQuery from '@mui/material/useMediaQuery'
import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogContentText from '@mui/material/DialogContentText'
import DialogTitle from '@mui/material/DialogTitle'
import Paper from '@mui/material/Paper'
import Skeleton from '@mui/material/Skeleton'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { extractErrorMessage } from '@/api/errors'
import { ActivityFeed } from '@/components/ActivityFeed'
import { PriorityLabel } from '@/components/PriorityLabel'
import { StatusChip } from '@/components/StatusChip'
import { UserAvatar } from '@/components/UserAvatar'
import { useActivityQuery } from '@/features/activity/activityQueries'
import { useAuth } from '@/features/auth/useAuth'
import { useMembersQuery } from '@/features/organization/organizationQueries'
import { useProjectsQuery } from '@/features/projects/projectsQueries'
import { formatRelativeTime } from '@/utils/relativeTime'
import {
  useCreateCommentMutation,
  useCommentsQuery,
  useDeleteCommentMutation,
} from './commentsQueries'
import { TaskFormModal } from './TaskFormModal'
import { commentFormSchema, type CommentFormValues } from './taskSchemas'
import { useDeleteTaskMutation, useTaskQuery } from './tasksQueries'

const TASK_HISTORY_LOOKBACK = 50

function formatDueDate(dueDate: string | null): string {
  if (!dueDate) {
    return 'No due date'
  }
  return new Date(dueDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

export function TaskDetailScreen() {
  const { id } = useParams<{ id: string }>()
  const taskId = id ?? ''
  const navigate = useNavigate()
  const { user } = useAuth()
  const isAdmin = user?.role === 'Admin'
  const theme = useTheme()
  const stacked = useMediaQuery(theme.breakpoints.down('md'))

  const taskQuery = useTaskQuery(taskId)
  const membersQuery = useMembersQuery()
  const projectsQuery = useProjectsQuery()
  const commentsQuery = useCommentsQuery(taskId)
  const activityQuery = useActivityQuery(TASK_HISTORY_LOOKBACK)

  const createComment = useCreateCommentMutation(taskId)
  const deleteComment = useDeleteCommentMutation(taskId)
  const deleteTask = useDeleteTaskMutation()

  const [editModalOpen, setEditModalOpen] = useState(false)
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false)

  const commentForm = useForm<CommentFormValues>({
    resolver: zodResolver(commentFormSchema),
    defaultValues: { body: '' },
  })

  async function onSubmitComment(values: CommentFormValues) {
    try {
      await createComment.mutateAsync(values.body)
      commentForm.reset()
    } catch {
      // surfaced via createComment.isError below
    }
  }

  async function confirmDeleteTask() {
    try {
      await deleteTask.mutateAsync(taskId)
      navigate('/tasks')
    } catch {
      // surfaced via the global error toast; keep the confirm dialog open to retry
    }
  }

  if (taskQuery.isLoading) {
    return (
      <Box sx={{ p: '36px 40px', maxWidth: 900 }}>
        <Skeleton variant="text" width={120} height={32} sx={{ mb: 3 }} />
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: stacked ? '1fr' : '1fr 248px',
            gap: '22px',
          }}
        >
          <Box>
            <Skeleton variant="text" width="70%" height={40} sx={{ mb: 2 }} />
            <Skeleton variant="rounded" height={110} sx={{ mb: 2.25 }} />
            <Skeleton variant="rounded" height={160} />
          </Box>
          <Skeleton variant="rounded" height={340} />
        </Box>
      </Box>
    )
  }

  const task = taskQuery.data
  if (!task) {
    return (
      <Box sx={{ p: '36px 40px' }}>
        <Typography variant="body2" sx={{ color: 'text.disabled' }}>
          Task not found.
        </Typography>
      </Box>
    )
  }

  const members = membersQuery.data ?? []
  const assignee = members.find((member) => member.id === task.assigneeId)
  const project = projectsQuery.data?.find((item) => item.id === task.projectId)
  const comments = commentsQuery.data ?? []
  const taskActivity = (activityQuery.data ?? []).filter((item) => item.taskId === task.id)
  const canDeleteTask = isAdmin || task.createdById === user?.id

  const metadataPanel = (
    <Paper
      sx={{
        borderRadius: '10px',
        p: '20px',
        display: 'flex',
        flexDirection: 'column',
        gap: 2.25,
        ...(stacked ? { mb: 2.25 } : { position: 'sticky', top: 36 }),
      }}
    >
      <Box>
        <Typography variant="overline" sx={{ display: 'block', mb: 0.875 }}>
          Status
        </Typography>
        <StatusChip status={task.status} />
      </Box>

      <Box sx={{ borderTop: '1px solid rgba(255,255,255,0.055)', pt: 2.25 }}>
        <Typography variant="overline" sx={{ display: 'block', mb: 0.875 }}>
          Priority
        </Typography>
        <PriorityLabel priority={task.priority} />
      </Box>

      <Box sx={{ borderTop: '1px solid rgba(255,255,255,0.055)', pt: 2.25 }}>
        <Typography variant="overline" sx={{ display: 'block', mb: 1.125 }}>
          Assignee
        </Typography>
        {assignee ? (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <UserAvatar name={`${assignee.firstName} ${assignee.lastName}`} size={24} />
            <Typography sx={{ fontSize: 13, color: 'text.secondary' }}>
              {assignee.firstName} {assignee.lastName}
            </Typography>
          </Box>
        ) : (
          <Typography sx={{ fontSize: 13, color: 'text.disabled' }}>Unassigned</Typography>
        )}
      </Box>

      <Box sx={{ borderTop: '1px solid rgba(255,255,255,0.055)', pt: 2.25 }}>
        <Typography variant="overline" sx={{ display: 'block', mb: 0.875 }}>
          Due Date
        </Typography>
        <Typography sx={{ fontSize: 13, color: 'text.secondary' }}>
          {formatDueDate(task.dueDate)}
        </Typography>
      </Box>

      <Box sx={{ borderTop: '1px solid rgba(255,255,255,0.055)', pt: 2.25 }}>
        <Typography variant="overline" sx={{ display: 'block', mb: 0.875 }}>
          Project
        </Typography>
        <Typography sx={{ fontSize: 13, color: project ? 'text.secondary' : 'text.disabled' }}>
          {project ? project.name : 'No project'}
        </Typography>
      </Box>

      <Box sx={{ borderTop: '1px solid rgba(255,255,255,0.055)', pt: 2.25 }}>
        <Typography variant="overline" sx={{ display: 'block', mb: 1.5 }}>
          History
        </Typography>
        <ActivityFeed
          items={taskActivity}
          isLoading={activityQuery.isLoading}
          emptyMessage="No activity yet."
        />
      </Box>

      <Box
        sx={{
          borderTop: '1px solid rgba(255,255,255,0.055)',
          pt: 2.25,
          display: 'flex',
          flexDirection: 'column',
          gap: 1,
        }}
      >
        <Button
          onClick={() => setEditModalOpen(true)}
          sx={{
            bgcolor: 'rgba(255,255,255,0.05)',
            color: 'text.primary',
            border: '1px solid rgba(255,255,255,0.09)',
          }}
        >
          Edit task
        </Button>
        {canDeleteTask && (
          <Button
            onClick={() => setDeleteConfirmOpen(true)}
            sx={{
              bgcolor: 'rgba(239,68,68,0.07)',
              color: 'error.main',
              border: '1px solid rgba(239,68,68,0.14)',
            }}
          >
            Delete task
          </Button>
        )}
      </Box>
    </Paper>
  )

  return (
    <Box sx={{ p: '36px 40px', maxWidth: 900 }}>
      <Button
        onClick={() => navigate('/tasks')}
        sx={{ color: 'text.disabled', fontSize: 13, mb: 3, pl: 0, minWidth: 0 }}
      >
        ← Back to tasks
      </Button>

      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: stacked ? '1fr' : '1fr 248px',
          gap: '22px',
          alignItems: 'start',
        }}
      >
        <Box>
          {project && (
            <Box
              component="span"
              sx={{
                display: 'inline-block',
                mb: 1.625,
                bgcolor: 'rgba(99, 102, 241, 0.1)',
                color: 'primary.light',
                fontSize: 11,
                fontWeight: 600,
                px: '9px',
                py: '3px',
                borderRadius: '5px',
              }}
            >
              {project.name}
            </Box>
          )}
          <Typography variant="h1" sx={{ fontSize: 23, mb: 1.625, lineHeight: 1.28 }}>
            {task.title}
          </Typography>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3.25 }}>
            <StatusChip status={task.status} />
            <PriorityLabel priority={task.priority} />
            <Typography sx={{ fontSize: 12.5, color: 'text.disabled' }}>
              Due {formatDueDate(task.dueDate)}
            </Typography>
          </Box>

          {stacked && metadataPanel}

          <Paper sx={{ borderRadius: '10px', p: '20px 22px', mb: 2.25 }}>
            <Typography variant="overline" sx={{ display: 'block', mb: 1.5 }}>
              Description
            </Typography>
            <Typography sx={{ fontSize: 14, color: 'text.secondary', lineHeight: 1.7 }}>
              {task.description || 'No description provided.'}
            </Typography>
          </Paper>

          <Paper sx={{ borderRadius: '10px', p: '20px 22px' }}>
            <Typography variant="overline" sx={{ display: 'block', mb: 2.25 }}>
              Comments
            </Typography>

            {commentsQuery.isLoading ? (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mb: 2.75 }}>
                {Array.from({ length: 2 }).map((_, index) => (
                  <Box key={index} sx={{ display: 'flex', gap: 1.25 }}>
                    <Skeleton variant="circular" width={29} height={29} />
                    <Skeleton variant="rounded" height={60} sx={{ flex: 1 }} />
                  </Box>
                ))}
              </Box>
            ) : (
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mb: 2.75 }}>
                {comments.length === 0 && (
                  <Typography variant="body2" sx={{ color: 'text.disabled' }}>
                    No comments yet.
                  </Typography>
                )}
                {comments.map((comment) => {
                  const canDeleteComment = isAdmin || comment.authorId === user?.id
                  return (
                    <Box key={comment.id} sx={{ display: 'flex', gap: 1.25 }}>
                      <UserAvatar name={comment.authorName} size={29} />
                      <Box
                        sx={{
                          flex: 1,
                          bgcolor: 'surface.input',
                          border: '1px solid rgba(255,255,255,0.06)',
                          borderRadius: '9px',
                          p: '12px 14px',
                        }}
                      >
                        <Box
                          sx={{
                            display: 'flex',
                            alignItems: 'center',
                            justifyContent: 'space-between',
                            mb: 0.75,
                          }}
                        >
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                            <Typography sx={{ fontSize: 13, fontWeight: 500 }}>
                              {comment.authorName}
                            </Typography>
                            <Typography sx={{ fontSize: 11, color: 'text.disabled' }}>
                              {formatRelativeTime(comment.createdAt)}
                            </Typography>
                          </Box>
                          {canDeleteComment && (
                            <Button
                              onClick={() =>
                                void deleteComment.mutateAsync(comment.id).catch(() => {})
                              }
                              sx={{ fontSize: 11.5, color: 'error.main', minWidth: 0, p: 0 }}
                            >
                              Delete
                            </Button>
                          )}
                        </Box>
                        <Typography
                          sx={{ fontSize: 13.5, color: 'text.secondary', lineHeight: 1.55 }}
                        >
                          {comment.body}
                        </Typography>
                      </Box>
                    </Box>
                  )
                })}
              </Box>
            )}

            <Box
              component="form"
              noValidate
              onSubmit={(event) => void commentForm.handleSubmit(onSubmitComment)(event)}
              sx={{ display: 'flex', gap: 1.25 }}
            >
              <UserAvatar name={`${user?.firstName ?? ''} ${user?.lastName ?? ''}`} size={29} />
              <Box sx={{ flex: 1 }}>
                {createComment.isError && (
                  <Alert severity="error" sx={{ mb: 1.25 }}>
                    {extractErrorMessage(createComment.error, 'Could not post the comment')}
                  </Alert>
                )}
                <TextField
                  {...commentForm.register('body')}
                  placeholder="Add a comment…"
                  multiline
                  rows={3}
                  fullWidth
                  error={!!commentForm.formState.errors.body}
                  helperText={commentForm.formState.errors.body?.message}
                />
                <Box sx={{ display: 'flex', justifyContent: 'flex-end', mt: 1 }}>
                  <Button
                    type="submit"
                    variant="contained"
                    disabled={commentForm.formState.isSubmitting}
                  >
                    Post comment
                  </Button>
                </Box>
              </Box>
            </Box>
          </Paper>
        </Box>

        {!stacked && metadataPanel}
      </Box>

      <TaskFormModal
        open={editModalOpen}
        onClose={() => setEditModalOpen(false)}
        members={members}
        projects={projectsQuery.data ?? []}
        mode="edit"
        task={task}
      />

      <Dialog open={deleteConfirmOpen} onClose={() => setDeleteConfirmOpen(false)}>
        <DialogTitle>Delete task</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Delete "{task.title}"? This will also remove its comments and cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirmOpen(false)}>Cancel</Button>
          <Button
            onClick={() => void confirmDeleteTask()}
            color="error"
            variant="contained"
            disabled={deleteTask.isPending}
          >
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
