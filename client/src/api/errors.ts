import { isAxiosError } from 'axios'
import type { FieldValues, Path, UseFormSetError } from 'react-hook-form'

export function extractErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError<{ title?: string }>(error) && typeof error.response?.data?.title === 'string') {
    return error.response.data.title
  }
  return fallback
}

function toFieldName(pascalCaseName: string): string {
  return pascalCaseName.charAt(0).toLowerCase() + pascalCaseName.slice(1)
}

/**
 * Maps ASP.NET Core's ValidationProblemDetails `errors` dictionary (PascalCase
 * property names, e.g. `{ Email: ["..."] }`) onto React Hook Form field errors.
 * Returns true if at least one field error was applied.
 */
export function applyFieldErrors<T extends FieldValues>(
  error: unknown,
  setError: UseFormSetError<T>,
): boolean {
  if (!isAxiosError<{ errors?: Record<string, string[]> }>(error)) {
    return false
  }
  const fieldErrors = error.response?.data?.errors
  if (!fieldErrors) {
    return false
  }
  let applied = false
  for (const [field, messages] of Object.entries(fieldErrors)) {
    if (!messages?.length) {
      continue
    }
    setError(toFieldName(field) as Path<T>, { type: 'server', message: messages[0] })
    applied = true
  }
  return applied
}
