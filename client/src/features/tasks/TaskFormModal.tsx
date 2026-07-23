import { useEffect, useRef } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Autocomplete from '@mui/material/Autocomplete'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import MenuItem from '@mui/material/MenuItem'
import TextField from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { applyFieldErrors, extractErrorMessage } from '@/api/errors'
import type { TaskDetail } from '@/api/tasksApi'
import type { Member } from '@/api/organizationApi'
import type { Project } from '@/api/projectsApi'
import { LabeledField } from '@/components/LabeledField'
import { useIsMobile } from '@/hooks/useIsMobile'
import { useCreateTaskMutation, useUpdateTaskMutation } from './tasksQueries'
import { taskFormSchema, type TaskFormValues } from './taskSchemas'

function defaultValues(task?: TaskDetail): TaskFormValues {
  return {
    title: task?.title ?? '',
    description: task?.description ?? '',
    status: task?.status ?? 'ToDo',
    priority: task?.priority ?? 'Medium',
    assigneeId: task?.assigneeId ?? null,
    dueDate: task?.dueDate ?? '',
    projectId: task?.projectId ?? null,
  }
}

interface TaskFormModalProps {
  open: boolean
  onClose: () => void
  members: Member[]
  projects: Project[]
  mode: 'create' | 'edit'
  task?: TaskDetail
}

export function TaskFormModal({
  open,
  onClose,
  members,
  projects,
  mode,
  task,
}: TaskFormModalProps) {
  const isMobile = useIsMobile()
  const createTask = useCreateTaskMutation()
  const updateTask = useUpdateTaskMutation()
  const mutation = mode === 'edit' ? updateTask : createTask
  const mutationRef = useRef(mutation)
  mutationRef.current = mutation

  const {
    control,
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<TaskFormValues>({
    resolver: zodResolver(taskFormSchema),
    defaultValues: defaultValues(task),
  })

  useEffect(() => {
    if (open) {
      reset(defaultValues(task))
      mutationRef.current.reset()
    }
  }, [open, task, reset])

  async function onSubmit(values: TaskFormValues) {
    const payload = {
      title: values.title,
      description: values.description || undefined,
      priority: values.priority,
      assigneeId: values.assigneeId ?? undefined,
      dueDate: values.dueDate || undefined,
      projectId: values.projectId ?? undefined,
    }
    try {
      if (mode === 'edit' && task) {
        await updateTask.mutateAsync({
          id: task.id,
          payload: { ...payload, status: values.status },
        })
      } else {
        await createTask.mutateAsync(payload)
      }
      onClose()
    } catch (error) {
      applyFieldErrors(error, setError)
    }
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth fullScreen={isMobile}>
      <DialogTitle sx={{ fontSize: 17, fontWeight: 600, letterSpacing: '-0.3px' }}>
        {mode === 'edit' ? 'Edit task' : 'New task'}
      </DialogTitle>
      <Box component="form" noValidate onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
          {mutation.isError && (
            <Alert severity="error">
              {extractErrorMessage(mutation.error, 'Could not save the task')}
            </Alert>
          )}

          <LabeledField
            label="Title"
            placeholder="What needs to be done?"
            {...register('title')}
            error={!!errors.title}
            helperText={errors.title?.message}
          />

          <LabeledField
            label="Description"
            placeholder="Optional — add more context…"
            multiline
            rows={3}
            {...register('description')}
            error={!!errors.description}
            helperText={errors.description?.message}
          />

          {mode === 'edit' && (
            <Controller
              control={control}
              name="status"
              render={({ field }) => (
                <LabeledField label="Status" select {...field}>
                  <MenuItem value="ToDo">To Do</MenuItem>
                  <MenuItem value="InProgress">In Progress</MenuItem>
                  <MenuItem value="Done">Done</MenuItem>
                </LabeledField>
              )}
            />
          )}

          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: isMobile ? '1fr' : '1fr 1fr 1fr',
              gap: 1.5,
            }}
          >
            <Controller
              control={control}
              name="assigneeId"
              render={({ field }) => (
                <Box>
                  <Typography
                    variant="caption"
                    sx={{ fontWeight: 500, display: 'block', mb: 0.75 }}
                  >
                    Assignee
                  </Typography>
                  <Autocomplete
                    options={members}
                    value={members.find((member) => member.id === field.value) ?? null}
                    onChange={(_, option) => field.onChange(option?.id ?? null)}
                    getOptionLabel={(member) => `${member.firstName} ${member.lastName}`}
                    isOptionEqualToValue={(option, value) => option.id === value.id}
                    renderInput={(params) => <TextField {...params} placeholder="Unassigned" />}
                  />
                </Box>
              )}
            />
            <Controller
              control={control}
              name="priority"
              render={({ field }) => (
                <LabeledField label="Priority" select {...field}>
                  <MenuItem value="Low">Low</MenuItem>
                  <MenuItem value="Medium">Medium</MenuItem>
                  <MenuItem value="High">High</MenuItem>
                </LabeledField>
              )}
            />
            <LabeledField
              label="Due date"
              type="date"
              slotProps={{ inputLabel: { shrink: true } }}
              {...register('dueDate')}
            />
          </Box>

          <Controller
            control={control}
            name="projectId"
            render={({ field }) => (
              <LabeledField
                label="Project"
                select
                value={field.value ?? ''}
                onChange={(event) => field.onChange(event.target.value || null)}
                onBlur={field.onBlur}
                ref={field.ref}
              >
                <MenuItem value="">No project</MenuItem>
                {projects.map((project) => (
                  <MenuItem key={project.id} value={project.id}>
                    {project.name}
                  </MenuItem>
                ))}
              </LabeledField>
            )}
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button onClick={onClose} sx={{ color: 'text.secondary' }}>
            Cancel
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            {mode === 'edit' ? 'Save changes' : 'Create task'}
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}
