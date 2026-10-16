import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query'
import { notifications } from '@mantine/notifications'
import { ApiError, errorMessage } from './errors'

declare module '@tanstack/react-query' {
  interface Register {
    queryMeta: AppMeta
    mutationMeta: AppMeta
  }
}

interface AppMeta extends Record<string, unknown> {
  /** Greška se prikazuje na samoj stranici (npr. u formi), bez obavijesti. */
  handlesErrors?: boolean
}

function notifyError(error: unknown, handlesErrors?: boolean) {
  // 401 vodi na prijavu (vidi client.ts), pa obavijest nije potrebna.
  if (handlesErrors || (error instanceof ApiError && error.status === 401)) {
    return
  }
  notifications.show({ color: 'red', title: 'Greška', message: errorMessage(error) })
}

export const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error, query) => notifyError(error, query.meta?.handlesErrors),
  }),
  mutationCache: new MutationCache({
    onError: (error, _variables, _context, mutation) =>
      notifyError(error, mutation.meta?.handlesErrors),
  }),
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      // Greške klijenta (4xx) se ne ponavljaju; mrežne i 5xx jednom.
      retry: (failureCount, error) =>
        !(error instanceof ApiError && error.status < 500) && failureCount < 1,
    },
  },
})
