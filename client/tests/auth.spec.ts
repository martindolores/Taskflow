import { test, expect } from '@playwright/test'
import { captureRequest, mockJson } from './support/api'
import { emptyTaskList, fakeMembers, fakeUser, signInWithFakeSession } from './support/fixtures'

const fakeTokens = { accessToken: 'fake-access-token', refreshToken: 'fake-refresh-token' }

test('register sends a request matching RegisterRequest', async ({ page }) => {
  const requestBody = captureRequest(page, 'POST', '/api/auth/register', fakeTokens)
  await mockJson(page, 'GET', '/api/users/me', fakeUser)
  await mockJson(page, 'GET', '/api/tasks', emptyTaskList)
  await mockJson(page, 'GET', '/api/organization/members', fakeMembers)

  await page.goto('/login')
  await page.getByText('Create one free').click()

  await page.getByLabel('Organization name').fill('Acme Inc.')
  await page.getByLabel('First name').fill('Ada')
  await page.getByLabel('Last name').fill('Admin')
  await page.getByLabel('Work email').fill('ada@example.com')
  await page.getByLabel('Password').fill('Password123!')
  await page.getByRole('button', { name: 'Create account' }).click()

  await expect(page).toHaveURL(/\/tasks$/)
  // Mirrors RegisterRequest(OrganizationName, Email, Password, FirstName, LastName).
  expect(await requestBody).toEqual({
    organizationName: 'Acme Inc.',
    email: 'ada@example.com',
    password: 'Password123!',
    firstName: 'Ada',
    lastName: 'Admin',
  })
})

test('login sends a request matching LoginRequest', async ({ page }) => {
  const requestBody = captureRequest(page, 'POST', '/api/auth/login', fakeTokens)
  await mockJson(page, 'GET', '/api/users/me', fakeUser)
  await mockJson(page, 'GET', '/api/tasks', emptyTaskList)
  await mockJson(page, 'GET', '/api/organization/members', fakeMembers)

  await page.goto('/login')
  await page.getByLabel('Email address').fill('ada@example.com')
  await page.getByLabel('Password').fill('Password123!')
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page).toHaveURL(/\/tasks$/)
  // Mirrors LoginRequest(Email, Password).
  expect(await requestBody).toEqual({
    email: 'ada@example.com',
    password: 'Password123!',
  })
})

test('logout sends a request matching LogoutRequest', async ({ page }) => {
  await mockJson(page, 'GET', '/api/tasks', emptyTaskList)
  await mockJson(page, 'GET', '/api/organization/members', fakeMembers)
  await signInWithFakeSession(page)
  await page.goto('/tasks')

  const requestBody = captureRequest(page, 'POST', '/api/auth/logout', {})

  await page.getByText(fakeUser.email).click()
  await page.getByRole('menuitem', { name: 'Log out' }).click()

  await expect(page).toHaveURL(/\/login$/)
  // Mirrors LogoutRequest(RefreshToken).
  expect(await requestBody).toEqual({ refreshToken: fakeTokens.refreshToken })
})
