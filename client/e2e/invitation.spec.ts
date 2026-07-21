import { test, expect } from './fixtures'
import { uniqueSuffix } from './helpers/api'

test('an admin can invite a teammate who joins via the invite link', async ({
  authenticatedPage: page,
  browser,
}) => {
  const suffix = uniqueSuffix()
  const inviteeEmail = `member-${suffix}@example.com`

  await page.goto('/settings')

  const [inviteResponse] = await Promise.all([
    page.waitForResponse(
      (response) =>
        response.request().method() === 'POST' &&
        response.url().endsWith('/api/organization/invitations'),
    ),
    (async () => {
      await page.getByLabel('Email address').fill(inviteeEmail)
      await page.getByRole('button', { name: 'Send invite' }).click()
    })(),
  ])
  const invitation = await inviteResponse.json()
  await expect(page.getByText('Invitation created')).toBeVisible()

  const inviteeContext = await browser.newContext()
  const inviteePage = await inviteeContext.newPage()
  await inviteePage.goto(`/accept-invitation?token=${invitation.token}`)
  await inviteePage.getByLabel('First name').fill('Mem')
  await inviteePage.getByLabel('Last name').fill('Ber')
  await inviteePage.getByLabel('Password').fill('Password123!')
  await inviteePage.getByRole('button', { name: 'Join workspace' }).click()

  await expect(inviteePage).toHaveURL(/\/tasks$/)
  await inviteeContext.close()

  await page.reload()
  await expect(page.getByText(inviteeEmail)).toBeVisible()
})
