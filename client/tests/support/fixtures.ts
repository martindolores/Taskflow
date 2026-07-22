import type { Page } from '@playwright/test'
import { mockJson } from './api'

/** Matches the keys in src/api/tokenStorage.ts. */
const ACCESS_TOKEN_KEY = 'taskflow.accessToken'
const REFRESH_TOKEN_KEY = 'taskflow.refreshToken'
const FAKE_ACCESS_TOKEN = 'fake-access-token'
export const FAKE_REFRESH_TOKEN = 'fake-refresh-token'

/** Shape of GET /api/users/me — src/api/authApi.ts's CurrentUser. */
export const fakeUser = {
  id: 'user-1',
  email: 'ada@example.com',
  firstName: 'Ada',
  lastName: 'Admin',
  role: 'Admin' as const,
  organizationId: 'org-1',
  organizationName: 'Acme Inc.',
}

/** Shape of GET /api/organization/members items — src/api/organizationApi.ts's Member. */
export const fakeMembers = [
  {
    id: fakeUser.id,
    email: fakeUser.email,
    firstName: fakeUser.firstName,
    lastName: fakeUser.lastName,
    role: fakeUser.role,
    status: 'Active',
  },
]

export const emptyTaskList = { items: [], total: 0, page: 1, pageSize: 20 }

/**
 * Seeds fake tokens into localStorage and mocks GET /api/users/me, so AuthContext hydrates a
 * logged-in session on load without a real backend.
 */
export async function signInWithFakeSession(page: Page): Promise<void> {
  await mockJson(page, 'GET', '/api/users/me', fakeUser)
  await page.goto('/login')
  await page.evaluate(
    ([accessKey, refreshKey, accessToken, refreshToken]) => {
      localStorage.setItem(accessKey, accessToken)
      localStorage.setItem(refreshKey, refreshToken)
    },
    [ACCESS_TOKEN_KEY, REFRESH_TOKEN_KEY, FAKE_ACCESS_TOKEN, FAKE_REFRESH_TOKEN],
  )
}
