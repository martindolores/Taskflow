import { useState, type MouseEvent, type ReactNode } from 'react'
import { NavLink, Outlet } from 'react-router-dom'
import { alpha } from '@mui/material/styles'
import Box from '@mui/material/Box'
import Menu from '@mui/material/Menu'
import MenuItem from '@mui/material/MenuItem'
import Typography from '@mui/material/Typography'
import { useAuth } from '@/features/auth/useAuth'

const navItems: { label: string; to: string; icon: ReactNode }[] = [
  {
    label: 'Dashboard',
    to: '/dashboard',
    icon: (
      <svg width="15" height="15" viewBox="0 0 16 16" fill="none">
        <rect x="1.5" y="1.5" width="5.5" height="5.5" rx="1.5" fill="currentColor" />
        <rect x="9" y="1.5" width="5.5" height="5.5" rx="1.5" fill="currentColor" opacity={0.4} />
        <rect x="1.5" y="9" width="5.5" height="5.5" rx="1.5" fill="currentColor" opacity={0.4} />
        <rect x="9" y="9" width="5.5" height="5.5" rx="1.5" fill="currentColor" />
      </svg>
    ),
  },
  {
    label: 'Tasks',
    to: '/tasks',
    icon: (
      <svg width="15" height="15" viewBox="0 0 16 16" fill="none">
        <path
          d="M2 4.5h12M2 8h9M2 11.5h6.5"
          stroke="currentColor"
          strokeWidth={1.5}
          strokeLinecap="round"
        />
      </svg>
    ),
  },
  {
    label: 'Settings',
    to: '/settings',
    icon: (
      <svg width="15" height="15" viewBox="0 0 16 16" fill="none">
        <circle cx={8} cy={8} r={2.2} fill="none" stroke="currentColor" strokeWidth={1.4} />
        <path
          d="M8 2v1.5M8 12.5V14M2 8h1.5M12.5 8H14M3.76 3.76l1.06 1.06M11.18 11.18l1.06 1.06M12.24 3.76l-1.06 1.06M4.82 11.18l-1.06 1.06"
          stroke="currentColor"
          strokeWidth={1.4}
          strokeLinecap="round"
        />
      </svg>
    ),
  },
]

export function AppShell() {
  const { user, logout } = useAuth()
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null)

  function openMenu(event: MouseEvent<HTMLElement>) {
    setMenuAnchor(event.currentTarget)
  }

  function closeMenu() {
    setMenuAnchor(null)
  }

  async function handleLogout() {
    closeMenu()
    await logout()
  }

  const initials = user ? `${user.firstName[0]}${user.lastName[0]}`.toUpperCase() : ''

  return (
    <Box sx={{ display: 'flex', height: '100vh', overflow: 'hidden' }}>
      <Box
        component="aside"
        sx={{
          width: 224,
          minWidth: 224,
          height: '100%',
          bgcolor: '#0c0c10',
          borderRight: '1px solid rgba(255, 255, 255, 0.055)',
          display: 'flex',
          flexDirection: 'column',
          padding: '14px 10px',
        }}
      >
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: '9px',
            padding: '6px 8px',
            mb: 2.75,
          }}
        >
          <Box
            sx={{
              width: 28,
              height: 28,
              bgcolor: 'primary.main',
              borderRadius: '7px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none">
              <rect x="2" y="2" width="5" height="5" rx="1.2" fill="white" />
              <rect x="9" y="2" width="5" height="5" rx="1.2" fill="white" opacity={0.55} />
              <rect x="2" y="9" width="5" height="5" rx="1.2" fill="white" opacity={0.55} />
              <rect x="9" y="9" width="5" height="5" rx="1.2" fill="white" />
            </svg>
          </Box>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography
              sx={{
                fontSize: 14,
                fontWeight: 600,
                letterSpacing: '-0.2px',
                whiteSpace: 'nowrap',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
              }}
            >
              Taskflow
            </Typography>
            <Typography
              sx={{
                fontSize: 11,
                color: '#52526a',
                whiteSpace: 'nowrap',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
              }}
            >
              {user?.organizationName}
            </Typography>
          </Box>
          <Box
            component="svg"
            width="14"
            height="14"
            viewBox="0 0 16 16"
            fill="none"
            sx={{ color: '#52526a', flexShrink: 0 }}
          >
            <path
              d="M5 7l3-3 3 3M5 9l3 3 3-3"
              stroke="currentColor"
              strokeWidth={1.4}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </Box>
        </Box>

        <Box component="nav" sx={{ display: 'flex', flexDirection: 'column', gap: '1px' }}>
          {navItems.map((item) => (
            <Box
              key={item.to}
              component={NavLink}
              to={item.to}
              sx={(theme) => ({
                display: 'flex',
                alignItems: 'center',
                gap: '9px',
                padding: '7px 10px',
                borderRadius: '7px',
                fontSize: '13.5px',
                fontWeight: 400,
                color: '#71717a',
                textDecoration: 'none',
                '&.active': {
                  backgroundColor: alpha(theme.palette.primary.main, 0.1),
                  color: theme.palette.nav.activeText,
                  fontWeight: 500,
                },
              })}
            >
              {item.icon}
              {item.label}
            </Box>
          ))}
        </Box>

        <Box sx={{ flex: 1 }} />

        <Box
          sx={{
            borderTop: '1px solid rgba(255, 255, 255, 0.055)',
            pt: 1.5,
            mt: 1,
          }}
        >
          <Box
            onClick={openMenu}
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: '9px',
              padding: '8px',
              borderRadius: '8px',
              cursor: 'pointer',
              '&:hover': { backgroundColor: 'rgba(255, 255, 255, 0.04)' },
            }}
          >
            <Box
              sx={{
                width: 30,
                height: 30,
                borderRadius: '50%',
                backgroundImage: (theme) => theme.palette.avatarGradient,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                fontSize: 11,
                fontWeight: 600,
                flexShrink: 0,
              }}
            >
              {initials}
            </Box>
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
                {user?.firstName} {user?.lastName}
              </Typography>
              <Typography
                sx={{
                  fontSize: 11,
                  color: '#52526a',
                  whiteSpace: 'nowrap',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                }}
              >
                {user?.email}
              </Typography>
            </Box>
            <Box
              component="svg"
              width="13"
              height="13"
              viewBox="0 0 16 16"
              fill="none"
              sx={{ color: '#52526a', flexShrink: 0 }}
            >
              <path
                d="M4 6l4 4 4-4"
                stroke="currentColor"
                strokeWidth={1.5}
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </Box>
          </Box>
          <Menu
            anchorEl={menuAnchor}
            open={!!menuAnchor}
            onClose={closeMenu}
            anchorOrigin={{ vertical: 'top', horizontal: 'left' }}
            transformOrigin={{ vertical: 'bottom', horizontal: 'left' }}
          >
            <MenuItem onClick={() => void handleLogout()}>Log out</MenuItem>
          </Menu>
        </Box>
      </Box>

      <Box component="main" sx={{ flex: 1, overflowY: 'auto', position: 'relative' }}>
        <Outlet />
      </Box>
    </Box>
  )
}
