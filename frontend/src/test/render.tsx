import { MantineProvider } from '@mantine/core'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render as testingLibraryRender } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router'
import { appTheme } from '@/app/theme'

interface RenderOptions {
  /** Početna adresa routera. */
  route?: string
  /** Podaci unaprijed u cacheu, npr. `[[authKeys.me, null]]`. */
  queryData?: [readonly unknown[], unknown][]
}

/** Render s Mantine, TanStack Query i routerom; komponente bez njih ne rade. */
export function render(ui: ReactNode, { route = '/', queryData = [] }: RenderOptions = {}) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: Infinity } },
  })
  for (const [key, data] of queryData) {
    queryClient.setQueryData(key, data)
  }

  return testingLibraryRender(ui, {
    wrapper: ({ children }) => (
      <MantineProvider theme={appTheme('comfortable')}>
        <QueryClientProvider client={queryClient}>
          <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
        </QueryClientProvider>
      </MantineProvider>
    ),
  })
}
