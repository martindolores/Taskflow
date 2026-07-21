import { useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Paper from '@mui/material/Paper'
import Typography from '@mui/material/Typography'
import { extractErrorMessage } from '@/api/errors'
import { LabeledField } from '@/components/LabeledField'
import { useAuth } from './useAuth'
import { acceptInvitationSchema, type AcceptInvitationFormValues } from './authSchemas'

export function AcceptInvitationScreen() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const { acceptInvitation } = useAuth()
  const navigate = useNavigate()

  const form = useForm<AcceptInvitationFormValues>({
    resolver: zodResolver(acceptInvitationSchema),
    defaultValues: { firstName: '', lastName: '', password: '' },
  })

  async function onSubmit(values: AcceptInvitationFormValues) {
    if (!token) {
      return
    }
    setErrorMessage(null)
    try {
      await acceptInvitation({ token, ...values })
      navigate('/tasks', { replace: true })
    } catch (error) {
      setErrorMessage(extractErrorMessage(error, 'This invitation link is invalid or has expired'))
    }
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        backgroundImage:
          'radial-gradient(ellipse 90% 50% at 50% -5%, rgba(99,102,241,0.09) 0%, transparent 65%)',
        p: 2,
      }}
    >
      <Box sx={{ width: 400, maxWidth: '100%' }}>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1.25,
            justifyContent: 'center',
            mb: 5.5,
          }}
        >
          <Box
            sx={{
              width: 34,
              height: 34,
              bgcolor: 'primary.main',
              borderRadius: '8px',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            <svg width="17" height="17" viewBox="0 0 16 16" fill="none">
              <rect x="2" y="2" width="5" height="5" rx="1.2" fill="white" />
              <rect x="9" y="2" width="5" height="5" rx="1.2" fill="white" opacity={0.55} />
              <rect x="2" y="9" width="5" height="5" rx="1.2" fill="white" opacity={0.55} />
              <rect x="9" y="9" width="5" height="5" rx="1.2" fill="white" />
            </svg>
          </Box>
          <Typography sx={{ fontSize: 19, fontWeight: 600, letterSpacing: '-0.4px' }}>
            Taskflow
          </Typography>
        </Box>

        <Paper sx={{ borderRadius: '14px', p: '32px 30px' }}>
          {!token ? (
            <Alert severity="error">
              This invitation link is invalid. Ask your organization admin to send a new one.
            </Alert>
          ) : (
            <>
              <Typography variant="h2" sx={{ mb: 0.625 }}>
                Accept your invitation
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.disabled', mb: 3.5 }}>
                Set your name and password to join the workspace
              </Typography>

              {errorMessage && (
                <Alert severity="error" sx={{ mb: 2.5 }} onClose={() => setErrorMessage(null)}>
                  {errorMessage}
                </Alert>
              )}

              <Box
                component="form"
                noValidate
                onSubmit={(event) => void form.handleSubmit(onSubmit)(event)}
                sx={{ display: 'flex', flexDirection: 'column', gap: 1.875 }}
              >
                <Box sx={{ display: 'flex', gap: 1.875 }}>
                  <LabeledField
                    label="First name"
                    {...form.register('firstName')}
                    error={!!form.formState.errors.firstName}
                    helperText={form.formState.errors.firstName?.message}
                  />
                  <LabeledField
                    label="Last name"
                    {...form.register('lastName')}
                    error={!!form.formState.errors.lastName}
                    helperText={form.formState.errors.lastName?.message}
                  />
                </Box>
                <LabeledField
                  label="Password"
                  placeholder="Minimum 8 characters"
                  type="password"
                  {...form.register('password')}
                  error={!!form.formState.errors.password}
                  helperText={form.formState.errors.password?.message}
                />
                <Button
                  type="submit"
                  variant="contained"
                  fullWidth
                  disabled={form.formState.isSubmitting}
                  sx={{ mt: 0.75, py: 1.375, fontSize: 14, fontWeight: 500 }}
                >
                  Join workspace
                </Button>
              </Box>
            </>
          )}
        </Paper>
      </Box>
    </Box>
  )
}
