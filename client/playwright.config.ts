import { defineConfig, devices } from '@playwright/test'

// Must match `Cors:AllowedOrigins` in server/src/TaskFlow.Api/appsettings.Development.json —
// the backend only allows http://localhost:5173, so the browser-served app has to run there too.
const PORT = 5173
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? `http://localhost:${PORT}`

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: 'html',
  use: {
    baseURL,
    trace: 'on-first-retry',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  // Skipped when PLAYWRIGHT_BASE_URL points at an already-running build (e.g. a Vercel preview) —
  // otherwise this serves the local production build (`npm run build` first) on PORT.
  webServer: process.env.PLAYWRIGHT_BASE_URL
    ? undefined
    : {
        command: `npm run preview -- --port ${PORT} --strictPort`,
        url: baseURL,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
      },
})
