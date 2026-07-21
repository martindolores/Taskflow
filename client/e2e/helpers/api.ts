import type { APIRequestContext, Page } from '@playwright/test'

export interface RegisteredOrg {
  organizationName: string
  email: string
  password: string
  firstName: string
  lastName: string
  accessToken: string
  refreshToken: string
  userId: string
  organizationId: string
}

/** Matches the keys in src/api/tokenStorage.ts. */
const ACCESS_TOKEN_KEY = 'taskflow.accessToken'
const REFRESH_TOKEN_KEY = 'taskflow.refreshToken'

export function uniqueSuffix(): string {
  return `${Date.now()}-${Math.floor(Math.random() * 1_000_000)}`
}

/** Seeds a fresh org + Admin user straight through the real API (no UI, no mocking). */
export async function registerOrganization(request: APIRequestContext): Promise<RegisteredOrg> {
  const suffix = uniqueSuffix()
  const payload = {
    organizationName: `E2E Org ${suffix}`,
    email: `owner-${suffix}@example.com`,
    password: 'Password123!',
    firstName: 'Ada',
    lastName: 'Admin',
  }

  const response = await request.post('/api/auth/register', { data: payload })
  if (!response.ok()) {
    throw new Error(`registerOrganization failed: ${response.status()} ${await response.text()}`)
  }
  const body = await response.json()

  return {
    ...payload,
    accessToken: body.accessToken,
    refreshToken: body.refreshToken,
    userId: body.userId,
    organizationId: body.organizationId,
  }
}

/** Drops API-issued tokens into localStorage so the app hydrates an authenticated session on load. */
export async function signInAs(
  page: Page,
  tokens: Pick<RegisteredOrg, 'accessToken' | 'refreshToken'>,
): Promise<void> {
  await page.goto('/login')
  await page.evaluate(
    ([accessToken, refreshToken, accessKey, refreshKey]) => {
      localStorage.setItem(accessKey, accessToken)
      localStorage.setItem(refreshKey, refreshToken)
    },
    [tokens.accessToken, tokens.refreshToken, ACCESS_TOKEN_KEY, REFRESH_TOKEN_KEY],
  )
  await page.goto('/tasks')
}
