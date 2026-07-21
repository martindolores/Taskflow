import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import { extractErrorMessage } from './errors'
import { showToast } from '@/components/toast'

declare module '@tanstack/react-query' {
  interface Register {
    queryMeta: { suppressErrorToast?: boolean }
    mutationMeta: { suppressErrorToast?: boolean }
  }
}

function reportError(error: unknown, suppressErrorToast?: boolean) {
  if (suppressErrorToast) {
    return
  }
  showToast(extractErrorMessage(error, 'Something went wrong'), 'error')
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error, query) => reportError(error, query.meta?.suppressErrorToast),
  }),
  mutationCache: new MutationCache({
    onError: (error, _variables, _context, mutation) =>
      reportError(error, mutation.meta?.suppressErrorToast),
  }),
})
