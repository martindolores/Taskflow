import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import * as authApi from '@/api/authApi'
import type { CurrentUser, LoginPayload, RegisterPayload } from '@/api/authApi'
import { clearTokens, getAccessToken, getRefreshToken, setTokens } from '@/api/tokenStorage'
import { AuthContext } from './context'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    async function hydrate() {
      if (!getAccessToken()) {
        setIsLoading(false)
        return
      }
      try {
        setUser(await authApi.getCurrentUser())
      } catch {
        clearTokens()
      } finally {
        setIsLoading(false)
      }
    }
    void hydrate()
  }, [])

  const login = useCallback(async (payload: LoginPayload) => {
    const tokens = await authApi.login(payload)
    setTokens(tokens.accessToken, tokens.refreshToken)
    setUser(await authApi.getCurrentUser())
  }, [])

  const register = useCallback(async (payload: RegisterPayload) => {
    const tokens = await authApi.register(payload)
    setTokens(tokens.accessToken, tokens.refreshToken)
    setUser(await authApi.getCurrentUser())
  }, [])

  const logout = useCallback(async () => {
    const refreshToken = getRefreshToken()
    clearTokens()
    setUser(null)
    if (refreshToken) {
      try {
        await authApi.logout(refreshToken)
      } catch {
        // best-effort server-side revocation; local session is already cleared
      }
    }
  }, [])

  const value = useMemo(
    () => ({ user, isLoading, login, register, logout }),
    [user, isLoading, login, register, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
