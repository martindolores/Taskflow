import type { Page, Route } from '@playwright/test'

function pathMatches(route: Route, method: string, pathname: string | RegExp): boolean {
  const request = route.request()
  if (request.method() !== method) {
    return false
  }
  const actual = new URL(request.url()).pathname
  return typeof pathname === 'string' ? actual === pathname : pathname.test(actual)
}

/**
 * Mocks a read endpoint the app hits as a side effect of the flow under test (e.g. the members
 * list a form's Assignee dropdown depends on) so the screen renders without a real backend.
 * Multiple calls stack — the most recently registered match wins, falling back to earlier ones
 * for any request it doesn't recognize.
 */
export async function mockJson(
  page: Page,
  method: string,
  pathname: string | RegExp,
  body: unknown,
  status = 200,
): Promise<void> {
  await page.route('**/api/**', async (route) => {
    if (pathMatches(route, method, pathname)) {
      await route.fulfill({ json: body, status })
      return
    }
    await route.fallback()
  })
}

/**
 * Mocks the write endpoint under test and resolves with the JSON body the app actually sent, for
 * asserting it matches the backend's request DTO shape.
 */
export function captureRequest(
  page: Page,
  method: string,
  pathname: string | RegExp,
  respondWith: unknown,
  status = 200,
): Promise<unknown> {
  return new Promise((resolve) => {
    void page.route('**/api/**', async (route) => {
      if (pathMatches(route, method, pathname)) {
        resolve(route.request().postDataJSON())
        await route.fulfill({ json: respondWith, status })
        return
      }
      await route.fallback()
    })
  })
}
