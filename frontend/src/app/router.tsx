import { createBrowserRouter } from 'react-router'
import { RequireAuth } from '@/features/auth/RequireAuth'
import { RequireSetup } from '@/features/setup/RequireSetup'
import { PageLoader } from '@/components/PageLoader'
import { AppLayout } from './layout/AppLayout'
import { NotFoundPage } from './NotFoundPage'
import { RouteError } from './RouteError'

// Stranice se učitavaju zasebno (lazy), pa javni zaslon stanice ne učitava
// dashboard i obrnuto. Čuvari ruta i layout su u glavnom paketu.
export const router = createBrowserRouter([
  {
    errorElement: <RouteError />,
    hydrateFallbackElement: <PageLoader />,
    children: [
      {
        path: '/login',
        lazy: async () => ({ Component: (await import('@/features/auth/LoginPage')).LoginPage }),
      },
      {
        path: '/board/:stopCode',
        lazy: async () => ({ Component: (await import('@/features/board/BoardPage')).BoardPage }),
      },
      {
        element: <RequireAuth />,
        children: [
          {
            path: '/setup',
            lazy: async () => ({
              Component: (await import('@/features/setup/SetupPage')).SetupPage,
            }),
          },
          {
            element: <RequireSetup />,
            children: [
              {
                element: <AppLayout />,
                children: [
                  {
                    index: true,
                    lazy: async () => ({
                      Component: (await import('@/features/dashboard/DashboardPage')).DashboardPage,
                    }),
                  },
                  {
                    path: 'routes',
                    lazy: async () => ({
                      Component: (await import('@/features/routes/RoutesPage')).RoutesPage,
                    }),
                  },
                  {
                    path: 'stops',
                    lazy: async () => ({
                      Component: (await import('@/features/stops/StopsPage')).StopsPage,
                    }),
                  },
                  {
                    path: 'calendars',
                    lazy: async () => ({
                      Component: (await import('@/features/calendars/CalendarsPage')).CalendarsPage,
                    }),
                  },
                  {
                    path: 'agencies',
                    lazy: async () => ({
                      Component: (await import('@/features/agencies/AgenciesPage')).AgenciesPage,
                    }),
                  },
                  {
                    path: 'gtfs',
                    lazy: async () => ({
                      Component: (await import('@/features/gtfs/GtfsPage')).GtfsPage,
                    }),
                  },
                  {
                    path: 'settings',
                    lazy: async () => ({
                      Component: (await import('@/features/settings/SettingsPage')).SettingsPage,
                    }),
                  },
                  { path: '*', element: <NotFoundPage /> },
                ],
              },
            ],
          },
        ],
      },
    ],
  },
])
