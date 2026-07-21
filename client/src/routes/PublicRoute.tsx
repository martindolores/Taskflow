import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/features/auth/useAuth'

export function PublicRoute() {
  const { user, isLoading } = useAuth()

  if (isLoading) {
    return null
  }

  if (user) {
    return <Navigate to="/tasks" replace />
  }

  return <Outlet />
}
