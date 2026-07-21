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
  // The Status <Select> has no accessible name at all (LabeledField's <label htmlFor> lands on its
  // hidden form input, not the clickable combobox div — getByLabel/getByRole({ name }) can't reach
  // it). It's the first combobox in the dialog — Title/Description above it are plain textboxes.
  await page.getByRole('dialog').getByRole('combobox').first().click()
  await page.getByRole('option', { name: 'In Progress' }).click()
  await page.getByRole('button', { name: 'Save changes' }).click()
  await expect(page.getByRole('dialog')).not.toBeVisible()
  // "In Progress" renders twice (header badge + sidebar Status panel); either proves the edit
  // took, so just check the first match instead of over-specifying which one.
  await expect(page.getByText('In Progress').first()).toBeVisible()

  const commentBody = `Looks good ${suffix}`
  await page.getByPlaceholder('Add a comment…').fill(commentBody)
  await page.getByRole('button', { name: 'Post comment' }).click()
  await expect(page.getByText(commentBody)).toBeVisible()

  // Unqualified "Delete" name-matching is substring by default, so it'd also match "Delete task".
  await page.getByRole('button', { name: 'Delete', exact: true }).click()
  await expect(page.getByText(commentBody)).not.toBeVisible()

  await page.getByRole('button', { name: 'Delete task' }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Delete' }).click()

  await expect(page).toHaveURL(/\/tasks$/)
  await expect(page.getByText(title)).not.toBeVisible()
})
