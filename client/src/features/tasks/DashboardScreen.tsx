import { useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import Box from '@mui/material/Box'
import Paper from '@mui/material/Paper'
import Skeleton from '@mui/material/Skeleton'
import Typography from '@mui/material/Typography'
import type { TaskListItem, TaskStatus } from '@/api/tasksApi'
import { StatusChip } from '@/components/StatusChip'
import { useAuth } from '@/features/auth/useAuth'
import { useTasksQuery } from './tasksQueries'

const OPEN_TASKS_LIMIT = 5

const statCards: { label: string; status: TaskStatus; caption: string; color?: string }[] = [
  { label: 'To Do', status: 'ToDo', caption: 'tasks pending' },
  { label: 'In Progress', status: 'InProgress', caption: 'active tasks', color: 'warning.main' },
  { label: 'Completed', status: 'Done', caption: 'this sprint', color: 'success.main' },
]

function greeting(): string {
  const hour = new Date().getHours()
  if (hour < 12) {
    return 'Good morning'
  }
  if (hour < 18) {
    return 'Good afternoon'
  }
  return 'Good evening'
}

function formatDueDate(dueDate: string | null): string {
  if (!dueDate) {
    return 'No due date'
  }
  return new Date(dueDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

function sortByDueDate(tasks: TaskListItem[]): TaskListItem[] {
  return [...tasks].sort((a, b) => {
    if (!a.dueDate && !b.dueDate) return 0
    if (!a.dueDate) return 1
    if (!b.dueDate) return -1
    return a.dueDate.localeCompare(b.dueDate)
  })
}

export function DashboardScreen() {
  const { user } = useAuth()
  const navigate = useNavigate()

  const todoQuery = useTasksQuery({ page: 1, pageSize: 100, status: 'ToDo' })
  const inProgressQuery = useTasksQuery({ page: 1, pageSize: 100, status: 'InProgress' })
  const doneQuery = useTasksQuery({ page: 1, pageSize: 1, status: 'Done' })

  const isLoading = todoQuery.isLoading || inProgressQuery.isLoading || doneQuery.isLoading

  const counts: Record<TaskStatus, number> = {
    ToDo: todoQuery.data?.total ?? 0,
    InProgress: inProgressQuery.data?.total ?? 0,
    Done: doneQuery.data?.total ?? 0,
  }

  const openTasks = useMemo(() => {
    const items = [...(todoQuery.data?.items ?? []), ...(inProgressQuery.data?.items ?? [])]
    return sortByDueDate(items).slice(0, OPEN_TASKS_LIMIT)
  }, [todoQuery.data, inProgressQuery.data])

  const today = new Date().toLocaleDateString(undefined, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  })

  return (
    <Box sx={{ p: '36px 40px', maxWidth: 1040 }}>
      <Box sx={{ mb: 4.25 }}>
        <Typography variant="h1" sx={{ fontSize: 23, mb: 0.5 }}>
          {greeting()}, {user?.firstName}
        </Typography>
        <Typography sx={{ fontSize: 13.5, color: 'text.disabled' }}>
          {today} · {user?.organizationName}
        </Typography>
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 1.75, mb: 3.5 }}>
        {statCards.map((card) => (
          <Paper
            key={card.status}
            onClick={() => navigate(`/tasks?status=${card.status}`)}
            sx={{
              borderRadius: '11px',
              p: '20px 22px',
              cursor: 'pointer',
              '&:hover': { borderColor: 'border.hover' },
            }}
          >
            <Typography variant="overline" sx={{ display: 'block', mb: 1.75 }}>
              {card.label}
            </Typography>
            {isLoading ? (
              <Skeleton variant="text" width={48} height={44} sx={{ mb: 0.375 }} />
            ) : (
              <Typography
                sx={{
                  fontSize: 36,
                  fontWeight: 600,
                  letterSpacing: '-2px',
                  mb: 0.375,
                  color: card.color ?? 'text.primary',
                }}
              >
                {counts[card.status]}
              </Typography>
            )}
            <Typography sx={{ fontSize: 12, color: 'text.disabled' }}>{card.caption}</Typography>
          </Paper>
        ))}
      </Box>

      <Paper sx={{ borderRadius: '11px', p: '22px' }}>
        <Box
          sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 2.25 }}
        >
          <Typography variant="subtitle1">Open tasks</Typography>
          <Typography
            onClick={() => navigate('/tasks')}
            sx={{ fontSize: 12, color: 'primary.light', cursor: 'pointer' }}
          >
            View all →
          </Typography>
        </Box>

        {isLoading ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.875 }}>
            {Array.from({ length: 3 }).map((_, index) => (
              <Skeleton key={index} variant="rounded" height={44} />
            ))}
          </Box>
        ) : openTasks.length === 0 ? (
          <Typography variant="body2" sx={{ color: 'text.disabled' }}>
            No open tasks — nice work.
          </Typography>
        ) : (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.875 }}>
            {openTasks.map((task) => (
              <Box
                key={task.id}
                onClick={() => navigate(`/tasks/${task.id}`)}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1.25,
                  padding: '9px 10px',
                  borderRadius: '8px',
                  border: '1px solid rgba(255,255,255,0.05)',
                  cursor: 'pointer',
                  bgcolor: 'rgba(255,255,255,0.015)',
                  '&:hover': {
                    bgcolor: 'rgba(255,255,255,0.04)',
                    borderColor: 'rgba(255,255,255,0.09)',
                  },
                }}
              >
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Typography
                    sx={{
                      fontSize: 13,
                      fontWeight: 500,
                      whiteSpace: 'nowrap',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                    }}
                  >
                    {task.title}
                  </Typography>
                  <Typography sx={{ fontSize: 11.5, color: 'text.disabled', mt: 0.125 }}>
                    {formatDueDate(task.dueDate)}
                  </Typography>
                </Box>
                <StatusChip status={task.status} />
              </Box>
            ))}
          </Box>
        )}
      </Paper>
    </Box>
  )
}
