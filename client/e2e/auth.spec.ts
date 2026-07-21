import { test, expect } from './fixtures'
import { uniqueSuffix } from './helpers/api'

test('registers a new organization, logs out, and logs back in', async ({ page }) => {
  const suffix = uniqueSuffix()
  const email = `owner-${suffix}@example.com`
  const password = 'Password123!'

  await page.goto('/login')
  await page.getByText('Create one free').click()

  await page.getByLabel('Organization name').fill(`Acme ${suffix}`)
  await page.getByLabel('First name').fill('Ada')
  await page.getByLabel('Last name').fill('Admin')
  await page.getByLabel('Work email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Create account' }).click()

  await expect(page).toHaveURL(/\/tasks$/)
  await expect(page.getByRole('heading', { name: 'Tasks' })).toBeVisible()

  await page.getByText(email).click()
  await page.getByRole('menuitem', { name: 'Log out' }).click()
  await expect(page).toHaveURL(/\/login$/)

  await page.getByLabel('Email address').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page).toHaveURL(/\/tasks$/)
  await expect(page.getByRole('heading', { name: 'Tasks' })).toBeVisible()
})
