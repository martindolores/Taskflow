import { isAxiosError } from 'axios'

export function extractErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError<{ title?: string }>(error) && typeof error.response?.data?.title === 'string') {
    return error.response.data.title
  }
  return fallback
}
