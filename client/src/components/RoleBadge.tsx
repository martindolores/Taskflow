import Box from '@mui/material/Box'
import type { UserRole } from '@/api/authApi'

export function RoleBadge({ role }: { role: UserRole }) {
  return (
    <Box
      component="span"
      sx={(theme) => ({
        display: 'inline-block',
        width: 'fit-content',
        justifySelf: 'start',
        borderRadius: '5px',
        padding: '3px 9px',
        fontSize: 11.5,
        fontWeight: 500,
        whiteSpace: 'nowrap',
        bgcolor: role === 'Admin' ? 'rgba(99, 102, 241, 0.12)' : 'rgba(255, 255, 255, 0.06)',
        color: role === 'Admin' ? theme.palette.nav.activeText : theme.palette.text.secondary,
      })}
    >
      {role}
    </Box>
  )
}
