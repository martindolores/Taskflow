import { ThemeProvider } from '@mui/material/styles'
import CssBaseline from '@mui/material/CssBaseline'
import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { theme } from '@/theme'
import { queryClient } from '@/api/queryClient'
import { AcceptInvitationScreen } from '@/features/auth/AcceptInvitationScreen'
import { AuthProvider } from '@/features/auth/AuthContext'
import { AuthScreen } from '@/features/auth/AuthScreen'
import { OrganizationSettingsScreen } from '@/features/organization/OrganizationSettingsScreen'
import { DashboardScreen } from '@/features/tasks/DashboardScreen'
import { TaskDetailScreen } from '@/features/tasks/TaskDetailScreen'
import { TaskListScreen } from '@/features/tasks/TaskListScreen'
import { AppShell } from '@/routes/AppShell'
import { ProtectedRoute } from '@/routes/ProtectedRoute'
import { PublicRoute } from '@/routes/PublicRoute'

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
                <Route path="/accept-invitation" element={<AcceptInvitationScreen />} />
              </Route>
              <Route element={<ProtectedRoute />}>
                <Route element={<AppShell />}>
                  <Route path="/dashboard" element={<DashboardScreen />} />
                  <Route path="/tasks" element={<TaskListScreen />} />
                  <Route path="/tasks/:id" element={<TaskDetailScreen />} />
                  <Route path="/settings" element={<OrganizationSettingsScreen />} />
                </Route>
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
