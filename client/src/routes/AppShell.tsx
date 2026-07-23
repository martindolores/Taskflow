import { useState, type MouseEvent, type ReactNode } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { alpha } from '@mui/material/styles'
import AppBar from '@mui/material/AppBar'
import BottomNavigation from '@mui/material/BottomNavigation'
import BottomNavigationAction from '@mui/material/BottomNavigationAction'
import Box from '@mui/material/Box'
import Menu from '@mui/material/Menu'
import MenuItem from '@mui/material/MenuItem'
import Toolbar from '@mui/material/Toolbar'
import Typography from '@mui/material/Typography'
import { useAuth } from '@/features/auth/useAuth'
import { NewProjectModal } from '@/features/projects/NewProjectModal'
import { useProjectsQuery } from '@/features/projects/projectsQueries'
import { useTasksQuery } from '@/features/tasks/tasksQueries'
import { useIsMobile } from '@/hooks/useIsMobile'

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
    label: 'Projects',
    to: '/projects',
    icon: (
      <svg width="15" height="15" viewBox="0 0 16 16" fill="none">
        <rect x="1.5" y="1.5" width="5.5" height="7" rx="1.5" fill="currentColor" opacity={0.9} />
        <rect x="9" y="1.5" width="5.5" height="4" rx="1.5" fill="currentColor" opacity={0.4} />
        <rect x="9" y="7.5" width="5.5" height="7" rx="1.5" fill="currentColor" opacity={0.4} />
        <rect x="1.5" y="10.5" width="5.5" height="4" rx="1.5" fill="currentColor" opacity={0.4} />
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
  const isMobile = useIsMobile()
  const location = useLocation()
  const navigate = useNavigate()
  const [menuAnchor, setMenuAnchor] = useState<HTMLElement | null>(null)
  const [newProjectOpen, setNewProjectOpen] = useState(false)

  // The desktop sidebar's Projects section — skipped on mobile where there's no persistent sidebar.
  const projectsQuery = useProjectsQuery({ enabled: !isMobile })
  const tasksQuery = useTasksQuery({ page: 1, pageSize: 100 }, { enabled: !isMobile })

  const taskCountByProject = new Map<string, number>()
  for (const task of tasksQuery.data?.items ?? []) {
    if (task.projectId) {
      taskCountByProject.set(task.projectId, (taskCountByProject.get(task.projectId) ?? 0) + 1)
    }
  }

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

  const profileMenu = (
    <Menu
      anchorEl={menuAnchor}
      open={!!menuAnchor}
      onClose={closeMenu}
      anchorOrigin={{ vertical: 'top', horizontal: 'left' }}
      transformOrigin={{ vertical: 'bottom', horizontal: 'left' }}
    >
      <MenuItem onClick={() => void handleLogout()}>Log out</MenuItem>
    </Menu>
  )

  if (isMobile) {
    const activeNavValue =
      navItems.find((item) => location.pathname.startsWith(item.to))?.to ?? navItems[0].to

    return (
      <Box sx={{ display: 'flex', flexDirection: 'column', height: '100vh', overflow: 'hidden' }}>
        <AppBar
          position="static"
          sx={{
            bgcolor: '#0c0c10',
            borderBottom: '1px solid rgba(255, 255, 255, 0.055)',
          }}
        >
          <Toolbar sx={{ gap: 1.25, minHeight: 52 }}>
            <Box
              sx={{
                width: 26,
                height: 26,
                bgcolor: 'primary.main',
                borderRadius: '7px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                flexShrink: 0,
              }}
            >
              <svg width="13" height="13" viewBox="0 0 16 16" fill="none">
                <rect x="2" y="2" width="5" height="5" rx="1.2" fill="white" />
                <rect x="9" y="2" width="5" height="5" rx="1.2" fill="white" opacity={0.55} />
                <rect x="2" y="9" width="5" height="5" rx="1.2" fill="white" opacity={0.55} />
                <rect x="9" y="9" width="5" height="5" rx="1.2" fill="white" />
              </svg>
            </Box>
            <Typography sx={{ fontSize: 14, fontWeight: 600, letterSpacing: '-0.2px', flex: 1 }}>
              Taskflow
            </Typography>
            <Box
              onClick={openMenu}
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
                cursor: 'pointer',
              }}
            >
              {initials}
            </Box>
          </Toolbar>
        </AppBar>

        <Box component="main" sx={{ flex: 1, overflowY: 'auto', position: 'relative' }}>
          <Outlet />
        </Box>

        <BottomNavigation
          showLabels
          value={activeNavValue}
          onChange={(_event, value: string) => navigate(value)}
          sx={{
            bgcolor: '#0c0c10',
            borderTop: '1px solid rgba(255, 255, 255, 0.055)',
            height: 58,
          }}
        >
          {navItems.map((item) => (
            <BottomNavigationAction
              key={item.to}
              label={item.label}
              value={item.to}
              icon={item.icon}
              sx={{
                color: '#71717a',
                '&.Mui-selected': { color: (theme) => theme.palette.nav.activeText },
              }}
            />
          ))}
        </BottomNavigation>

        {profileMenu}
      </Box>
    )
  }

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

        <Box sx={{ mt: 2.75, px: 1 }}>
          <Box
            sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 1 }}
          >
            <Typography
              sx={{
                fontSize: 10.5,
                fontWeight: 600,
                color: '#3f3f52',
                textTransform: 'uppercase',
                letterSpacing: '0.7px',
              }}
            >
              Projects
            </Typography>
            <Box
              component="button"
              type="button"
              aria-label="New project"
              title="New project"
              onClick={() => setNewProjectOpen(true)}
              sx={{
                background: 'none',
                border: 'none',
                color: '#3f3f52',
                cursor: 'pointer',
                p: 0,
                lineHeight: 1,
                fontSize: 16,
                display: 'flex',
                alignItems: 'center',
                '&:hover': { color: '#9898b0' },
              }}
            >
              +
            </Box>
          </Box>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: '2px' }}>
            {projectsQuery.data?.map((project) => (
              <Box
                key={project.id}
                onClick={() => navigate(`/tasks?project=${project.id}`)}
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1,
                  padding: '6px 4px',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  '&:hover': { backgroundColor: 'rgba(255, 255, 255, 0.04)' },
                }}
              >
                <Box
                  sx={{
                    width: 7,
                    height: 7,
                    borderRadius: '2px',
                    flexShrink: 0,
                    bgcolor: project.color,
                  }}
                />
                <Typography
                  sx={{
                    fontSize: 13,
                    flex: 1,
                    whiteSpace: 'nowrap',
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                  }}
                >
                  {project.name}
                </Typography>
                <Typography sx={{ fontSize: 11, color: '#3f3f52' }}>
                  {taskCountByProject.get(project.id) ?? 0}
                </Typography>
              </Box>
            ))}
          </Box>
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
          {profileMenu}
        </Box>
      </Box>

      <Box component="main" sx={{ flex: 1, overflowY: 'auto', position: 'relative' }}>
        <Outlet />
      </Box>

      <NewProjectModal open={newProjectOpen} onClose={() => setNewProjectOpen(false)} />
    </Box>
  )
}
