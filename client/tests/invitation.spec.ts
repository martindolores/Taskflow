import { test, expect } from '@playwright/test'
import { captureRequest, mockJson } from './support/api'
import { emptyTaskList, fakeMembers, fakeUser, signInWithFakeSession } from './support/fixtures'

const fakeOrganization = {
  id: fakeUser.organizationId,
  name: fakeUser.organizationName,
  slug: 'acme-inc',
  memberCount: fakeMembers.length,
}

test(
  'inviting a teammate sends a request matching CreateInvitationRequest',
  { tag: '@mobile' },
  async ({ page }) => {
    await mockJson(page, 'GET', '/api/organization', fakeOrganization)
    await mockJson(page, 'GET', '/api/organization/members', fakeMembers)
    await mockJson(page, 'GET', '/api/organization/invitations', [])
    await signInWithFakeSession(page)
    await page.goto('/settings')

    const requestBody = captureRequest(page, 'POST', '/api/organization/invitations', {
      id: 'invitation-1',
      email: 'member@example.com',
      role: 'Member',
      status: 'Pending',
      expiresAt: '2026-08-01T00:00:00Z',
      token: 'invite-token-1',
    })

    await page.getByLabel('Email address').fill('member@example.com')
    await page.getByRole('button', { name: 'Send invite' }).click()

    await expect(page.getByText('Invitation created')).toBeVisible()
    // Mirrors CreateInvitationRequest(Email, Role).
    expect(await requestBody).toEqual({ email: 'member@example.com', role: 'Member' })
  },
)

test(
  'accepting an invitation sends a request matching AcceptInvitationRequest',
  { tag: '@mobile' },
  async ({ page }) => {
    const requestBody = captureRequest(page, 'POST', '/api/auth/accept-invitation', {
      accessToken: 'fake-access-token',
      refreshToken: 'fake-refresh-token',
    })
    await mockJson(page, 'GET', '/api/users/me', fakeUser)
    await mockJson(page, 'GET', '/api/tasks', emptyTaskList)
    await mockJson(page, 'GET', '/api/organization/members', fakeMembers)

    await page.goto('/accept-invitation?token=invite-token-1')
    await page.getByLabel('First name').fill('Mem')
    await page.getByLabel('Last name').fill('Ber')
    await page.getByLabel('Password').fill('Password123!')
    await page.getByRole('button', { name: 'Join workspace' }).click()

    await expect(page).toHaveURL(/\/tasks$/)
    // Mirrors AcceptInvitationRequest(Token, Password, FirstName, LastName).
    expect(await requestBody).toEqual({
      token: 'invite-token-1',
      password: 'Password123!',
      firstName: 'Mem',
      lastName: 'Ber',
    })
  },
)
