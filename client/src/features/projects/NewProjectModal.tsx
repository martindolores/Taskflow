import { useEffect, useRef } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Dialog from '@mui/material/Dialog'
import DialogActions from '@mui/material/DialogActions'
import DialogContent from '@mui/material/DialogContent'
import DialogTitle from '@mui/material/DialogTitle'
import Typography from '@mui/material/Typography'
import { applyFieldErrors, extractErrorMessage } from '@/api/errors'
import { LabeledField } from '@/components/LabeledField'
import { useIsMobile } from '@/hooks/useIsMobile'
import { useCreateProjectMutation } from './projectsQueries'
import { PROJECT_COLORS, projectFormSchema, type ProjectFormValues } from './projectSchemas'

const defaultValues: ProjectFormValues = {
  name: '',
  description: '',
  color: PROJECT_COLORS[0],
}

interface NewProjectModalProps {
  open: boolean
  onClose: () => void
}

export function NewProjectModal({ open, onClose }: NewProjectModalProps) {
  const isMobile = useIsMobile()
  const createProject = useCreateProjectMutation()
  const createProjectRef = useRef(createProject)
  createProjectRef.current = createProject

  const {
    control,
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<ProjectFormValues>({
    resolver: zodResolver(projectFormSchema),
    defaultValues,
  })

  useEffect(() => {
    if (open) {
      reset(defaultValues)
      createProjectRef.current.reset()
    }
  }, [open, reset])

  async function onSubmit(values: ProjectFormValues) {
    try {
      await createProject.mutateAsync({
        name: values.name,
        color: values.color,
        description: values.description || undefined,
      })
      onClose()
    } catch (error) {
      applyFieldErrors(error, setError)
    }
  }

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth fullScreen={isMobile}>
      <DialogTitle sx={{ fontSize: 17, fontWeight: 600, letterSpacing: '-0.3px' }}>
        New project
      </DialogTitle>
      <Box component="form" noValidate onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2.25 }}>
          {createProject.isError && (
            <Alert severity="error">
              {extractErrorMessage(createProject.error, 'Could not create the project')}
            </Alert>
          )}

          <LabeledField
            label="Project name"
            placeholder="e.g. Mobile App"
            {...register('name')}
            error={!!errors.name}
            helperText={errors.name?.message}
          />

          <LabeledField
            label="Description"
            placeholder="Optional — what's this project about?"
            {...register('description')}
            error={!!errors.description}
            helperText={errors.description?.message}
          />

          <Controller
            control={control}
            name="color"
            render={({ field }) => (
              <Box>
                <Typography variant="caption" sx={{ fontWeight: 500, display: 'block', mb: 1.25 }}>
                  Colour
                </Typography>
                <Box sx={{ display: 'flex', gap: 1.125 }}>
                  {PROJECT_COLORS.map((color) => (
                    <Box
                      key={color}
                      role="radio"
                      aria-checked={field.value === color}
                      aria-label={color}
                      onClick={() => field.onChange(color)}
                      sx={{
                        width: 28,
                        height: 28,
                        borderRadius: '7px',
                        cursor: 'pointer',
                        bgcolor: color,
                        outline:
                          field.value === color ? `2px solid ${color}` : '2px solid transparent',
                        outlineOffset: '2px',
                        '&:hover': { opacity: 0.85 },
                      }}
                    />
                  ))}
                </Box>
              </Box>
            )}
          />
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 3 }}>
          <Button onClick={onClose} sx={{ color: 'text.secondary' }}>
            Cancel
          </Button>
          <Button type="submit" variant="contained" disabled={isSubmitting}>
            Create project
          </Button>
        </DialogActions>
      </Box>
    </Dialog>
  )
}
