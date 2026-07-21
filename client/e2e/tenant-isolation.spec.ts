import { test, expect } from './fixtures'
import { registerOrganization, signInAs, uniqueSuffix } from './helpers/api'

test('a task created in one org is invisible to another org', async ({ page, api, browser }) => {
  const orgA = await registerOrganization(api)
  const orgB = await registerOrganization(api)

  await signInAs(page, orgA)
  const title = `Org A only task ${uniqueSuffix()}`

  await page.getByRole('button', { name: 'New task' }).click()
  await page.getByLabel('Title').fill(title)

  const [createResponse] = await Promise.all([
    page.waitForResponse(
      (response) => response.request().method() === 'POST' && response.url().endsWith('/api/tasks'),
    ),
    page.getByRole('button', { name: 'Create task' }).click(),
  ])
  const orgATask = await createResponse.json()
  await expect(page.getByText(title)).toBeVisible()

  const contextB = await browser.newContext()
  const pageB = await contextB.newPage()
  await signInAs(pageB, orgB)

  await pageB.goto('/tasks')
  await expect(pageB.getByRole('heading', { name: 'Tasks' })).toBeVisible()
  await expect(pageB.getByText(title)).not.toBeVisible()

  await pageB.goto(`/tasks/${orgATask.id}`)
  // The tenant-scoped 404 goes through TanStack Query's default retry (3x, exponential backoff)
  // before the query settles into an error and the "not found" copy renders — give it room.
  await expect(pageB.getByText('Task not found.')).toBeVisible({ timeout: 15_000 })

  await contextB.close()
})
