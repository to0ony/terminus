import { MantineProvider } from '@mantine/core'
import { DatesProvider } from '@mantine/dates'
import { ModalsProvider } from '@mantine/modals'
import { Notifications } from '@mantine/notifications'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { useMemo } from 'react'
import { RouterProvider } from 'react-router'
import { queryClient } from '@/api/queryClient'
import { router } from './router'
import { useDensity } from '@/features/appearance/density'
import { appTheme } from './theme'

export function App() {
  const [density] = useDensity()
  const theme = useMemo(() => appTheme(density), [density])

  return (
    <MantineProvider theme={theme} defaultColorScheme="auto">
      <DatesProvider settings={{ locale: 'hr', firstDayOfWeek: 1 }}>
        <QueryClientProvider client={queryClient}>
          <ModalsProvider>
            <Notifications position="top-right" />
            <RouterProvider router={router} />
          </ModalsProvider>
          <ReactQueryDevtools buttonPosition="bottom-left" />
        </QueryClientProvider>
      </DatesProvider>
    </MantineProvider>
  )
}
