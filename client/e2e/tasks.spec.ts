import { test, expect } from './fixtures'
import { uniqueSuffix } from './helpers/api'

test('creates, edits, comments on, and deletes a task', async ({ authenticatedPage: page }) => {
  const suffix = uniqueSuffix()
  const title = `Write onboarding doc ${suffix}`

  await page.goto('/tasks')
  await page.getByRole('button', { name: 'New task' }).click()
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Description').fill('Draft the onboarding checklist')
  await page.getByRole('button', { name: 'Create task' }).click()

  await expect(page.getByRole('dialog')).not.toBeVisible()
  await expect(page.getByText(title)).toBeVisible()

  await page.getByText(title).click()
  await expect(page).toHaveURL(/\/tasks\/[^/]+$/)
  await expect(page.getByRole('heading', { name: title })).toBeVisible()

  await page.getByRole('button', { name: 'Edit task' }).click()
  await page.getByLabel('Status').click()
  await page.getByRole('option', { name: 'In Progress' }).click()
  await page.getByRole('button', { name: 'Save changes' }).click()
  await expect(page.getByRole('dialog')).not.toBeVisible()
  await expect(page.getByText('In Progress')).toBeVisible()

  const commentBody = `Looks good ${suffix}`
  await page.getByPlaceholder('Add a comment…').fill(commentBody)
  await page.getByRole('button', { name: 'Post comment' }).click()
  await expect(page.getByText(commentBody)).toBeVisible()

  await page.getByRole('button', { name: 'Delete' }).click()
  await expect(page.getByText(commentBody)).not.toBeVisible()

  await page.getByRole('button', { name: 'Delete task' }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Delete' }).click()

  await expect(page).toHaveURL(/\/tasks$/)
  await expect(page.getByText(title)).not.toBeVisible()
})
