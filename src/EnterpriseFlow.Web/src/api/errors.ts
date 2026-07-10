import { isAxiosError } from 'axios'
import type { ProblemDetails } from '../types'

/** Backend errors are ProblemDetails (GlobalExceptionHandler) — surface the useful part of it. */
export function extractErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError<ProblemDetails>(error) && error.response) {
    const { data } = error.response

    if (data?.errors) {
      return Object.values(data.errors).flat().join(' ')
    }

    if (data?.title) {
      return data.title
    }
  }

  return fallback
}
