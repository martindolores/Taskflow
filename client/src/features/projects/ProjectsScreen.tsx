import { useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Paper from '@mui/material/Paper'
import Skeleton from '@mui/material/Skeleton'
import Typography from '@mui/material/Typography'
import type { TaskListItem } from '@/api/tasksApi'
import type { Project } from '@/api/projectsApi'
import { UserAvatar } from '@/components/UserAvatar'
import { useAuth } from '@/features/auth/useAuth'
import { useTasksQuery } from '@/features/tasks/tasksQueries'
import { NewProjectModal } from './NewProjectModal'
import { useProjectsQuery } from './projectsQueries'

interface ProjectStats {
  total: number
  inProgress: number
  done: number
  pct: number
  assigneeNames: string[]
}

function statsFor(project: Project, tasks: TaskListItem[]): ProjectStats {
  const projectTasks = tasks.filter((task) => task.projectId === project.id)
  const done = projectTasks.filter((task) => task.status === 'Done').length
  const assigneeNames = [
    ...new Set(
      projectTasks.map((task) => task.assigneeName).filter((name): name is string => !!name),
    ),
  ].slice(0, 3)
  return {
    total: projectTasks.length,
    inProgress: projectTasks.filter((task) => task.status === 'InProgress').length,
    done,
    pct: projectTasks.length ? Math.round((done / projectTasks.length) * 100) : 0,
    assigneeNames,
  }
}

export function ProjectsScreen() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [createModalOpen, setCreateModalOpen] = useState(false)

  const projectsQuery = useProjectsQuery()
  const tasksQuery = useTasksQuery({ page: 1, pageSize: 100 })

  const projects = projectsQuery.data ?? []
  const tasks = useMemo(() => tasksQuery.data?.items ?? [], [tasksQuery.data])

  return (
    <Box sx={{ p: '36px 40px', maxWidth: 1040 }}>
      <Box
        sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', mb: 3.5 }}
      >
        <Box>
          <Typography variant="h1">Projects</Typography>
          <Typography variant="body2" sx={{ color: 'text.disabled', mt: 0.375 }}>
            {projects.length} projects · {user?.organizationName}
          </Typography>
        </Box>
        <Button variant="contained" onClick={() => setCreateModalOpen(true)}>
          New project
        </Button>
      </Box>

      {projectsQuery.isLoading ? (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
            gap: 1.75,
          }}
        >
          {Array.from({ length: 3 }).map((_, index) => (
            <Skeleton key={index} variant="rounded" height={220} sx={{ borderRadius: '11px' }} />
          ))}
        </Box>
      ) : projects.length === 0 ? (
        <Paper sx={{ borderRadius: '11px', p: '22px' }}>
          <Typography variant="body2" sx={{ color: 'text.disabled' }}>
            No projects yet — create your first project to group related tasks.
          </Typography>
        </Paper>
      ) : (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', md: 'repeat(3, 1fr)' },
            gap: 1.75,
          }}
        >
          {projects.map((project) => {
            const stats = statsFor(project, tasks)
            return (
              <Paper
                key={project.id}
                onClick={() => navigate(`/tasks?project=${project.id}`)}
                sx={{
                  borderRadius: '11px',
                  p: '22px',
                  cursor: 'pointer',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: 2,
                  '&:hover': { borderColor: 'border.hover' },
                }}
              >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25 }}>
                  <Box
                    sx={{
                      width: 32,
                      height: 32,
                      borderRadius: '8px',
                      flexShrink: 0,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      bgcolor: `${project.color}18`,
                    }}
                  >
                    <Box
                      sx={{ width: 10, height: 10, borderRadius: '2.5px', bgcolor: project.color }}
                    />
                  </Box>
                  <Typography sx={{ fontSize: 14, fontWeight: 600, letterSpacing: '-0.2px' }}>
                    {project.name}
                  </Typography>
                </Box>

                <Typography
                  sx={{ fontSize: 13, color: 'text.disabled', lineHeight: 1.5, minHeight: 36 }}
                >
                  {project.description || 'No description.'}
                </Typography>

                <Box sx={{ display: 'flex', gap: 2.25 }}>
                  <Box>
                    <Typography sx={{ fontSize: 20, fontWeight: 600, letterSpacing: '-1px' }}>
                      {stats.total}
                    </Typography>
                    <Typography sx={{ fontSize: 11.5, color: 'text.disabled', mt: 0.125 }}>
                      tasks
                    </Typography>
                  </Box>
                  <Box>
                    <Typography
                      sx={{
                        fontSize: 20,
                        fontWeight: 600,
                        letterSpacing: '-1px',
                        color: 'warning.main',
                      }}
                    >
                      {stats.inProgress}
                    </Typography>
                    <Typography sx={{ fontSize: 11.5, color: 'text.disabled', mt: 0.125 }}>
                      in progress
                    </Typography>
                  </Box>
                  <Box>
                    <Typography
                      sx={{
                        fontSize: 20,
                        fontWeight: 600,
                        letterSpacing: '-1px',
                        color: 'success.main',
                      }}
                    >
                      {stats.done}
                    </Typography>
                    <Typography sx={{ fontSize: 11.5, color: 'text.disabled', mt: 0.125 }}>
                      done
                    </Typography>
                  </Box>
                </Box>

                <Box
                  sx={{
                    height: 3,
                    bgcolor: 'rgba(255,255,255,0.06)',
                    borderRadius: '2px',
                    overflow: 'hidden',
                  }}
                >
                  <Box
                    sx={{
                      height: '100%',
                      borderRadius: '2px',
                      bgcolor: project.color,
                      width: `${stats.pct}%`,
                    }}
                  />
                </Box>

                <Box
                  sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}
                >
                  <Box sx={{ display: 'flex' }}>
                    {stats.assigneeNames.map((name) => (
                      <Box
                        key={name}
                        sx={{
                          mr: '-6px',
                          border: '1.5px solid',
                          borderColor: 'background.paper',
                          borderRadius: '50%',
                        }}
                      >
                        <UserAvatar name={name} size={22} />
                      </Box>
                    ))}
                  </Box>
                  <Typography sx={{ fontSize: 11.5, color: 'text.disabled' }}>
                    {stats.pct}% complete
                  </Typography>
                </Box>
              </Paper>
            )
          })}
        </Box>
      )}

      <NewProjectModal open={createModalOpen} onClose={() => setCreateModalOpen(false)} />
    </Box>
  )
}
