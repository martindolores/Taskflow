import { test as base, expect, type APIRequestContext, type Page } from '@playwright/test'
import { registerOrganization, signInAs, type RegisteredOrg } from './helpers/api'

interface TaskflowFixtures {
  /** APIRequestContext pointed at the real backend, for fixture-only setup (never mocked). */
  api: APIRequestContext
  /** A freshly registered org + Admin user, seeded via the real API. */
  org: RegisteredOrg
  /** `page`, pre-authenticated as `org`'s Admin user (session seeded via localStorage, not the login form). */
  authenticatedPage: Page
}

export const test = base.extend<TaskflowFixtures>({
  api: async ({ playwright }, use) => {
    const baseURL = process.env.E2E_API_URL ?? 'http://localhost:5151'
    const context = await playwright.request.newContext({ baseURL })
    await use(context)
    await context.dispose()
  },

  org: async ({ api }, use) => {
    await use(await registerOrganization(api))
  },

  authenticatedPage: async ({ page, org }, use) => {
    await signInAs(page, org)
    await use(page)
  },
})

export { expect }
