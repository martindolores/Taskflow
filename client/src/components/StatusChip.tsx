import Box from '@mui/material/Box'
import type { TaskStatus } from '@/api/tasksApi'

const statusMeta: Record<TaskStatus, { label: string; bg: string; color: string }> = {
  ToDo: { label: 'To Do', bg: 'rgba(255, 255, 255, 0.06)', color: '#9898b0' },
  InProgress: { label: 'In Progress', bg: 'rgba(245, 158, 11, 0.1)', color: '#f59e0b' },
  Done: { label: 'Done', bg: 'rgba(34, 197, 94, 0.1)', color: '#22c55e' },
}

export function StatusChip({ status }: { status: TaskStatus }) {
  const meta = statusMeta[status]
  return (
    <Box
      component="span"
      sx={{
        display: 'inline-block',
        width: 'fit-content',
        justifySelf: 'start',
        borderRadius: '5px',
        padding: '3px 9px',
        fontSize: 11.5,
        fontWeight: 500,
        whiteSpace: 'nowrap',
        bgcolor: meta.bg,
        color: meta.color,
      }}
    >
      {meta.label}
    </Box>
  )
}
