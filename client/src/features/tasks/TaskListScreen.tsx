import { useMemo, useState } from 'react'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import CircularProgress from '@mui/material/CircularProgress'
import Pagination from '@mui/material/Pagination'
import Paper from '@mui/material/Paper'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import type { TaskStatus } from '@/api/tasksApi'
import { PriorityLabel } from '@/components/PriorityLabel'
import { StatusChip } from '@/components/StatusChip'
import { UserAvatar } from '@/components/UserAvatar'
import { useAuth } from '@/features/auth/useAuth'
import { useMembersQuery } from '@/features/organization/organizationQueries'
import { TaskFormModal } from './TaskFormModal'
import { useTasksQuery } from './tasksQueries'

const PAGE_SIZE = 20

const filters: { label: string; value: 'all' | TaskStatus }[] = [
  { label: 'All', value: 'all' },
  { label: 'To Do', value: 'ToDo' },
  { label: 'In Progress', value: 'InProgress' },
  { label: 'Done', value: 'Done' },
]

const gridColumns = '1fr 148px 118px 90px 100px'

function formatDueDate(dueDate: string | null): string {
  if (!dueDate) {
    return '—'
  }
  return new Date(dueDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
}

export function TaskListScreen() {
  const { user } = useAuth()
  const [page, setPage] = useState(1)
  const [statusFilter, setStatusFilter] = useState<'all' | TaskStatus>('all')
  const [search, setSearch] = useState('')
  const [createModalOpen, setCreateModalOpen] = useState(false)

  const tasksQuery = useTasksQuery({ page, pageSize: PAGE_SIZE })
  const membersQuery = useMembersQuery()

  const filteredItems = useMemo(() => {
    const items = tasksQuery.data?.items ?? []
    const query = search.trim().toLowerCase()
    return items.filter((task) => {
      if (statusFilter !== 'all' && task.status !== statusFilter) {
        return false
      }
      if (!query) {
        return true
      }
      return (
        task.title.toLowerCase().includes(query) ||
        (task.assigneeName ?? '').toLowerCase().includes(query)
      )
    })
  }, [tasksQuery.data, statusFilter, search])

  const total = tasksQuery.data?.total ?? 0
  const pageCount = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <Box sx={{ p: '36px 40px' }}>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'flex-start',
          justifyContent: 'space-between',
          mb: 3,
        }}
      >
        <Box>
          <Typography variant="h1">Tasks</Typography>
          <Typography variant="body2" sx={{ color: 'text.disabled', mt: 0.375 }}>
            {total} tasks · {user?.organizationName}
          </Typography>
        </Box>
        <Button variant="contained" onClick={() => setCreateModalOpen(true)}>
          New task
        </Button>
      </Box>

      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, mb: 2.25, flexWrap: 'wrap' }}>
        <Box
          sx={{
            display: 'flex',
            bgcolor: 'background.paper',
            border: '1px solid',
            borderColor: 'border.default',
            borderRadius: '8px',
            padding: '3px',
            gap: '2px',
          }}
        >
          {filters.map((filter) => (
            <Button
              key={filter.value}
              onClick={() => setStatusFilter(filter.value)}
              sx={{
                px: 1.375,
                py: 0.625,
                borderRadius: '5px',
                fontSize: 12.5,
                fontWeight: 500,
                minWidth: 0,
                color: statusFilter === filter.value ? 'nav.activeText' : 'text.disabled',
                bgcolor: statusFilter === filter.value ? 'rgba(99, 102, 241, 0.12)' : 'transparent',
              }}
            >
              {filter.label}
            </Button>
          ))}
        </Box>
        <TextField
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search tasks…"
          size="small"
          sx={{ flex: 1, minWidth: 180, maxWidth: 260 }}
        />
      </Box>

      <Paper sx={{ borderRadius: '11px', overflow: 'hidden' }}>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: gridColumns,
            padding: '10px 18px',
            borderBottom: '1px solid rgba(255,255,255,0.06)',
            bgcolor: 'rgba(255,255,255,0.018)',
          }}
        >
          {['Title', 'Assignee', 'Status', 'Priority', 'Due'].map((label) => (
            <Typography key={label} variant="overline" sx={{ fontSize: 11 }}>
              {label}
            </Typography>
          ))}
        </Box>

        {tasksQuery.isLoading ? (
          <Box sx={{ p: 4, display: 'flex', justifyContent: 'center' }}>
            <CircularProgress size={22} />
          </Box>
        ) : filteredItems.length === 0 ? (
          <Box sx={{ p: '22px' }}>
            <Typography variant="body2" sx={{ color: 'text.disabled' }}>
              No tasks found.
            </Typography>
          </Box>
        ) : (
          filteredItems.map((task) => (
            <Box
              key={task.id}
              sx={{
                display: 'grid',
                gridTemplateColumns: gridColumns,
                padding: '13px 18px',
                borderBottom: '1px solid rgba(255,255,255,0.04)',
                alignItems: 'center',
              }}
            >
              <Typography
                sx={{
                  fontSize: 13.5,
                  fontWeight: 500,
                  color: 'text.primary',
                  pr: 2.25,
                  whiteSpace: 'nowrap',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                }}
              >
                {task.title}
              </Typography>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                {task.assigneeName ? (
                  <>
                    <UserAvatar name={task.assigneeName} size={22} />
                    <Typography
                      sx={{
                        fontSize: 12.5,
                        color: 'text.secondary',
                        whiteSpace: 'nowrap',
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                      }}
                    >
                      {task.assigneeName}
                    </Typography>
                  </>
                ) : (
                  <Typography sx={{ fontSize: 12.5, color: 'text.disabled' }}>
                    Unassigned
                  </Typography>
                )}
              </Box>
              <StatusChip status={task.status} />
              <PriorityLabel priority={task.priority} />
              <Typography sx={{ fontSize: 12.5, color: 'text.disabled' }}>
                {formatDueDate(task.dueDate)}
              </Typography>
            </Box>
          ))
        )}
      </Paper>

      {pageCount > 1 && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 3 }}>
          <Pagination
            count={pageCount}
            page={page}
            onChange={(_, value) => setPage(value)}
            shape="rounded"
          />
        </Box>
      )}

      <TaskFormModal
        open={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
        members={membersQuery.data ?? []}
        mode="create"
      />
    </Box>
  )
}
