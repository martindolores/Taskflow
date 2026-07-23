import { test, expect } from '@playwright/test'
import { captureRequest, mockJson } from './support/api'
import { emptyTaskList, fakeMembers, fakeUser, signInWithFakeSession } from './support/fixtures'

const taskId = 'task-1'

const fakeTask = {
  id: taskId,
  title: 'Write onboarding doc',
  description: 'Draft the onboarding checklist',
  status: 'ToDo',
  priority: 'Medium',
  assigneeId: null,
  dueDate: null,
  projectId: null,
  createdById: fakeUser.id,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
}

test('creating a task sends a request matching CreateTaskRequest', async ({ page }) => {
  await mockJson(page, 'GET', '/api/tasks', emptyTaskList)
  await mockJson(page, 'GET', '/api/organization/members', fakeMembers)
  await mockJson(page, 'GET', '/api/projects', [])
  await signInWithFakeSession(page)
  await page.goto('/tasks')

  const requestBody = captureRequest(page, 'POST', '/api/tasks', fakeTask)

  await page.getByRole('button', { name: 'New task' }).click()
  await page.getByLabel('Title').fill(fakeTask.title)
  await page.getByLabel('Description').fill(fakeTask.description)
  await page.getByRole('button', { name: 'Create task' }).click()

  await expect(page.getByRole('dialog')).not.toBeVisible()
  // Mirrors CreateTaskRequest(Title, Description, Priority, AssigneeId, DueDate) — Priority
  // defaults to Medium and Assignee/Due date are left blank (omitted, not sent as null).
  expect(await requestBody).toEqual({
    title: fakeTask.title,
    description: fakeTask.description,
    priority: 'Medium',
    assigneeId: undefined,
    dueDate: undefined,
  })
})

test('editing a task sends a request matching UpdateTaskRequest', async ({ page }) => {
  await mockJson(page, 'GET', '/api/tasks/task-1', fakeTask)
  await mockJson(page, 'GET', '/api/tasks/task-1/comments', [])
  await mockJson(page, 'GET', '/api/organization/members', fakeMembers)
  await mockJson(page, 'GET', '/api/projects', [])
  await signInWithFakeSession(page)
  await page.goto(`/tasks/${taskId}`)

  const requestBody = captureRequest(page, 'PUT', `/api/tasks/${taskId}`, {
    ...fakeTask,
    status: 'InProgress',
  })

  await page.getByRole('button', { name: 'Edit task' }).click()
  // The Status <Select> has no accessible name (LabeledField's <label htmlFor> lands on its
  // hidden form input, not the clickable combobox div) — it's the first combobox in the dialog,
  // ahead of the Assignee Autocomplete and Priority select.
  await page.getByRole('dialog').getByRole('combobox').first().click()
  await page.getByRole('option', { name: 'In Progress' }).click()
  await page.getByRole('button', { name: 'Save changes' }).click()

  await expect(page.getByRole('dialog')).not.toBeVisible()
  // Mirrors UpdateTaskRequest(Title, Description, Status, Priority, AssigneeId, DueDate).
  expect(await requestBody).toEqual({
    title: fakeTask.title,
    description: fakeTask.description,
    status: 'InProgress',
    priority: fakeTask.priority,
    assigneeId: undefined,
    dueDate: undefined,
  })
})

test('posting a comment sends a request matching CreateCommentRequest', async ({ page }) => {
  await mockJson(page, 'GET', '/api/tasks/task-1', fakeTask)
  await mockJson(page, 'GET', '/api/tasks/task-1/comments', [])
  await mockJson(page, 'GET', '/api/organization/members', fakeMembers)
  await mockJson(page, 'GET', '/api/projects', [])
  await signInWithFakeSession(page)
  await page.goto(`/tasks/${taskId}`)

  const commentBody = 'Looks good, thanks!'
  const requestBody = captureRequest(page, 'POST', `/api/tasks/${taskId}/comments`, {
    id: 'comment-1',
    body: commentBody,
    authorId: fakeUser.id,
    authorName: `${fakeUser.firstName} ${fakeUser.lastName}`,
    createdAt: '2026-01-01T00:00:00Z',
  })

  await page.getByPlaceholder('Add a comment…').fill(commentBody)
  await page.getByRole('button', { name: 'Post comment' }).click()

  // The comment form only resets on a successful mutation, so an empty box confirms the request
  // was accepted rather than rejected.
  await expect(page.getByPlaceholder('Add a comment…')).toHaveValue('')
  // Mirrors CreateCommentRequest(Body).
  expect(await requestBody).toEqual({ body: commentBody })
})
