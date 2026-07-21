import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import Box from '@mui/material/Box'
import Button from '@mui/material/Button'
import Typography from '@mui/material/Typography'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { theme } from '@/theme'
import { queryClient } from '@/api/queryClient'
import { AuthProvider } from '@/features/auth/AuthContext'
import { AuthScreen } from '@/features/auth/AuthScreen'
import { useAuth } from '@/features/auth/useAuth'
import { ProtectedRoute } from '@/routes/ProtectedRoute'
import { PublicRoute } from '@/routes/PublicRoute'

function TasksPlaceholder() {
  const { user, logout } = useAuth()

  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        height: '100%',
        gap: 2,
      }}
    >
      <Typography variant="h1">Welcome, {user?.firstName}</Typography>
      <Button variant="outlined" onClick={() => void logout()}>
        Log out
      </Button>
    </Box>
  )
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <BrowserRouter>
          <AuthProvider>
            <Routes>
              <Route path="/" element={<Navigate to="/tasks" replace />} />
              <Route element={<PublicRoute />}>
                <Route path="/login" element={<AuthScreen />} />
              </Route>
              <Route element={<ProtectedRoute />}>
                <Route path="/tasks" element={<TasksPlaceholder />} />
              </Route>
              <Route path="*" element={<Navigate to="/tasks" replace />} />
            </Routes>
          </AuthProvider>
        </BrowserRouter>
      </ThemeProvider>
    </QueryClientProvider>
  )
}

export default App
