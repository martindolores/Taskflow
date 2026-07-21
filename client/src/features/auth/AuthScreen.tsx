import { useId, useState, type ReactNode } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { isAxiosError } from 'axios'
import Alert from '@mui/material/Alert'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Link from '@mui/material/Link'
import Paper from '@mui/material/Paper'
import TextField, { type TextFieldProps } from '@mui/material/TextField'
import Typography from '@mui/material/Typography'
import { useAuth } from './useAuth'
import {
  loginSchema,
  registerSchema,
  type LoginFormValues,
  type RegisterFormValues,
} from './authSchemas'

type Mode = 'signin' | 'register'

function extractErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError<{ title?: string }>(error) && typeof error.response?.data?.title === 'string') {
    return error.response.data.title
  }
  return fallback
}

interface LabeledFieldProps extends Omit<TextFieldProps, 'label'> {
  label: string
  labelExtra?: ReactNode
}

function LabeledField({ label, labelExtra, id, ...textFieldProps }: LabeledFieldProps) {
  const generatedId = useId()
  const inputId = id ?? generatedId

  return (
    <Box>
      <Box
        sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', mb: 0.75 }}
      >
        <Typography component="label" htmlFor={inputId} variant="caption" sx={{ fontWeight: 500 }}>
          {label}
        </Typography>
        {labelExtra}
      </Box>
      <TextField id={inputId} fullWidth {...textFieldProps} />
    </Box>
  )
}

export function AuthScreen() {
  const [mode, setMode] = useState<Mode>('signin')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const { login, register: registerAccount } = useAuth()
  const navigate = useNavigate()

  const signInForm = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  const registerForm = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { organizationName: '', firstName: '', lastName: '', email: '', password: '' },
  })

  function toggleMode() {
    setErrorMessage(null)
    setMode((previous) => (previous === 'signin' ? 'register' : 'signin'))
  }

  async function onSignIn(values: LoginFormValues) {
    setErrorMessage(null)
    try {
      await login(values)
      navigate('/tasks', { replace: true })
    } catch (error) {
      setErrorMessage(extractErrorMessage(error, 'Invalid email or password'))
    }
  }

  async function onRegister(values: RegisterFormValues) {
    setErrorMessage(null)
    try {
      await registerAccount(values)
      navigate('/tasks', { replace: true })
    } catch (error) {
      setErrorMessage(extractErrorMessage(error, 'Could not create your account'))
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
          {errorMessage && (
            <Alert severity="error" sx={{ mb: 2.5 }} onClose={() => setErrorMessage(null)}>
              {errorMessage}
            </Alert>
          )}

          {mode === 'signin' ? (
            <>
              <Typography variant="h2" sx={{ mb: 0.625 }}>
                Welcome back
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.disabled', mb: 3.5 }}>
                Sign in to your Taskflow workspace
              </Typography>
              <Box
                component="form"
                noValidate
                onSubmit={(event) => void signInForm.handleSubmit(onSignIn)(event)}
                sx={{ display: 'flex', flexDirection: 'column', gap: 1.875 }}
              >
                <LabeledField
                  label="Email address"
                  placeholder="you@company.com"
                  type="email"
                  {...signInForm.register('email')}
                  error={!!signInForm.formState.errors.email}
                  helperText={signInForm.formState.errors.email?.message}
                />
                <LabeledField
                  label="Password"
                  placeholder="••••••••"
                  type="password"
                  labelExtra={
                    <Typography sx={{ fontSize: 12, color: 'primary.light', cursor: 'pointer' }}>
                      Forgot password?
                    </Typography>
                  }
                  {...signInForm.register('password')}
                  error={!!signInForm.formState.errors.password}
                  helperText={signInForm.formState.errors.password?.message}
                />
                <Button
                  type="submit"
                  variant="contained"
                  fullWidth
                  disabled={signInForm.formState.isSubmitting}
                  sx={{ mt: 0.75, py: 1.375, fontSize: 14, fontWeight: 500 }}
                >
                  Sign in
                </Button>
              </Box>
              <Typography variant="body2" align="center" sx={{ mt: 2.5, color: 'text.disabled' }}>
                No account?{' '}
                <Link
                  component="button"
                  type="button"
                  onClick={toggleMode}
                  underline="none"
                  color="primary.light"
                  sx={{ fontWeight: 500 }}
                >
                  Create one free
                </Link>
              </Typography>
            </>
          ) : (
            <>
              <Typography variant="h2" sx={{ mb: 0.625 }}>
                Create your account
              </Typography>
              <Typography variant="body2" sx={{ color: 'text.disabled', mb: 3.5 }}>
                Set up your Taskflow workspace
              </Typography>
              <Box
                component="form"
                noValidate
                onSubmit={(event) => void registerForm.handleSubmit(onRegister)(event)}
                sx={{ display: 'flex', flexDirection: 'column', gap: 1.875 }}
              >
                <LabeledField
                  label="Organization name"
                  placeholder="Acme Inc."
                  {...registerForm.register('organizationName')}
                  error={!!registerForm.formState.errors.organizationName}
                  helperText={registerForm.formState.errors.organizationName?.message}
                />
                <Box sx={{ display: 'flex', gap: 1.875 }}>
                  <LabeledField
                    label="First name"
                    {...registerForm.register('firstName')}
                    error={!!registerForm.formState.errors.firstName}
                    helperText={registerForm.formState.errors.firstName?.message}
                  />
                  <LabeledField
                    label="Last name"
                    {...registerForm.register('lastName')}
                    error={!!registerForm.formState.errors.lastName}
                    helperText={registerForm.formState.errors.lastName?.message}
                  />
                </Box>
                <LabeledField
                  label="Work email"
                  placeholder="you@company.com"
                  type="email"
                  {...registerForm.register('email')}
                  error={!!registerForm.formState.errors.email}
                  helperText={registerForm.formState.errors.email?.message}
                />
                <LabeledField
                  label="Password"
                  placeholder="Minimum 8 characters"
                  type="password"
                  {...registerForm.register('password')}
                  error={!!registerForm.formState.errors.password}
                  helperText={registerForm.formState.errors.password?.message}
                />
                <Button
                  type="submit"
                  variant="contained"
                  fullWidth
                  disabled={registerForm.formState.isSubmitting}
                  sx={{ mt: 0.75, py: 1.375, fontSize: 14, fontWeight: 500 }}
                >
                  Create account
                </Button>
              </Box>
              <Typography variant="body2" align="center" sx={{ mt: 2.5, color: 'text.disabled' }}>
                Already have an account?{' '}
                <Link
                  component="button"
                  type="button"
                  onClick={toggleMode}
                  underline="none"
                  color="primary.light"
                  sx={{ fontWeight: 500 }}
                >
                  Sign in
                </Link>
              </Typography>
            </>
          )}
        </Paper>
        <Typography
          variant="body2"
          align="center"
          sx={{ mt: 2.75, fontSize: 11.5, color: '#3f3f52' }}
        >
          By continuing you agree to our Terms of Service and Privacy Policy
        </Typography>
      </Box>
    </Box>
  )
}
