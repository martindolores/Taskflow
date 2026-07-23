import { test, expect } from '@playwright/test'
import { captureRequest, mockJson } from './support/api'
import { emptyTaskList, signInWithFakeSession } from './support/fixtures'

test('creating a project sends a request matching CreateProjectRequest', async ({ page }) => {
  await mockJson(page, 'GET', '/api/projects', [])
  await mockJson(page, 'GET', '/api/tasks', emptyTaskList)
  await signInWithFakeSession(page)
  await page.goto('/projects')

  const createdProject = {
    id: 'project-1',
    name: 'Mobile App',
    color: '#22c55e',
    description: 'Our new mobile app',
  }
  const requestBody = captureRequest(page, 'POST', '/api/projects', createdProject)

  // Two "New project" buttons exist at desktop width (the screen header and the sidebar "+"),
  // so scope to the main content area's button.
  await page.getByRole('main').getByRole('button', { name: 'New project' }).click()
  await page.getByLabel('Project name').fill(createdProject.name)
  await page.getByLabel('Description').fill(createdProject.description)
  await page.getByRole('radio', { name: createdProject.color }).click()
  await page.getByRole('button', { name: 'Create project' }).click()

  await expect(page.getByRole('dialog')).not.toBeVisible()
  // Mirrors CreateProjectRequest(Name, Color, Description).
  expect(await requestBody).toEqual({
    name: createdProject.name,
    color: createdProject.color,
    description: createdProject.description,
  })
})
