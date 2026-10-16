import { MantineProvider } from '@mantine/core'
import { DatesProvider } from '@mantine/dates'
import { ModalsProvider } from '@mantine/modals'
import { Notifications } from '@mantine/notifications'
import { QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { RouterProvider } from 'react-router'
import { queryClient } from '@/api/queryClient'
import { router } from './router'
import { theme } from './theme'

export function App() {
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
