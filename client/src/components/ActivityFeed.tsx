import Box from '@mui/material/Box'
import Skeleton from '@mui/material/Skeleton'
import Typography from '@mui/material/Typography'
import type { ActivityItem } from '@/api/activityApi'
import { formatRelativeTime } from '@/utils/relativeTime'
import { UserAvatar } from './UserAvatar'

export function ActivityFeed({
  items,
  isLoading,
  emptyMessage = 'No activity yet.',
}: {
  items: ActivityItem[]
  isLoading: boolean
  emptyMessage?: string
}) {
  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.25 }}>
        {Array.from({ length: 3 }).map((_, index) => (
          <Box key={index} sx={{ display: 'flex', gap: 1.25 }}>
            <Skeleton variant="circular" width={27} height={27} />
            <Skeleton variant="rounded" height={32} sx={{ flex: 1 }} />
          </Box>
        ))}
      </Box>
    )
  }

  if (items.length === 0) {
    return (
      <Typography variant="body2" sx={{ color: 'text.disabled' }}>
        {emptyMessage}
      </Typography>
    )
  }

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.25 }}>
      {items.map((item) => (
        <Box key={item.id} sx={{ display: 'flex', alignItems: 'flex-start', gap: 1.25 }}>
          <UserAvatar name={item.actorName} size={27} />
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography sx={{ fontSize: 12.5, color: '#9898b0', lineHeight: 1.45 }}>
              <Box component="span" sx={{ color: 'text.primary', fontWeight: 500 }}>
                {item.actorName}
              </Box>{' '}
              {item.summary}
            </Typography>
            <Typography sx={{ fontSize: 11, color: 'text.disabled', mt: 0.25 }}>
              {formatRelativeTime(item.createdAt)}
            </Typography>
          </Box>
        </Box>
      ))}
    </Box>
  )
}
