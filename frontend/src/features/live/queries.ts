import { useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type LiveRouteDay = components['schemas']['LiveRouteDayResponse']
export type LiveTrip = components['schemas']['LiveTripResponse']

/** Vožnje linije jednog dana s vremenima, trasama i stanicama, za simulaciju. */
export function useLiveRouteDay(routeId: string | null, date: string) {
  return useQuery({
    queryKey: ['routes', routeId, 'live', date],
    queryFn: async () =>
      unwrap(
        await api.GET('/api/routes/{id}/live', {
          params: { path: { id: routeId! }, query: { Date: date } },
        }),
      ),
    enabled: routeId !== null,
    staleTime: 60_000,
  })
}
