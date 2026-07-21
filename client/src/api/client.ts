import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { clearTokens, getAccessToken, getRefreshToken, setTokens } from './tokenStorage'

type RetryableRequestConfig = InternalAxiosRequestConfig & { _retry?: boolean }

interface RefreshTokenResponse {
  accessToken: string
  refreshToken: string
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
})

apiClient.interceptors.request.use((config) => {
  const accessToken = getAccessToken()
  if (accessToken) {
    config.headers.set('Authorization', `Bearer ${accessToken}`)
  }
  return config
})

function redirectToLogin(): void {
  clearTokens()
  window.location.href = '/login'
}

let refreshPromise: Promise<string> | null = null

async function refreshAccessToken(): Promise<string> {
  const refreshToken = getRefreshToken()
  if (!refreshToken) {
    throw new Error('No refresh token available')
  }

  // Plain axios, not apiClient, so this call never re-enters this interceptor.
  const response = await axios.post<RefreshTokenResponse>(
    `${import.meta.env.VITE_API_URL}/api/auth/refresh`,
    { refreshToken },
  )
  setTokens(response.data.accessToken, response.data.refreshToken)
  return response.data.accessToken
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined
    const isAuthRequest = originalRequest?.url?.startsWith('/api/auth/')

    if (error.response?.status !== 401 || !originalRequest || isAuthRequest) {
      return Promise.reject(error)
    }

    if (originalRequest._retry) {
      redirectToLogin()
      return Promise.reject(error)
    }

    originalRequest._retry = true

    try {
      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null
      })
      const accessToken = await refreshPromise
      originalRequest.headers.set('Authorization', `Bearer ${accessToken}`)
      return apiClient(originalRequest)
    } catch (refreshError) {
      redirectToLogin()
      return Promise.reject(refreshError)
    }
  },
)
