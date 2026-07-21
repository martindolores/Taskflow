import Box from '@mui/material/Box'
import type { TaskPriority } from '@/api/tasksApi'

const priorityMeta: Record<TaskPriority, { label: string; color: string }> = {
  Low: { label: 'Low', color: '#52526a' },
  Medium: { label: 'Medium', color: '#f59e0b' },
  High: { label: 'High', color: '#ef4444' },
}

export function PriorityLabel({ priority }: { priority: TaskPriority }) {
  const meta = priorityMeta[priority]
  return (
    <Box component="span" sx={{ fontSize: 12.5, fontWeight: 500, color: meta.color }}>
      {meta.label}
    </Box>
  )
}
