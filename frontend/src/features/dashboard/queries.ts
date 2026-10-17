import { useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import { calendarKeys } from '@/features/calendars/queries'

export type ServiceOverview = components['schemas']['ServiceOverviewResponse']

/** Kalendari koji danas voze i zadnji dan s voznim redom. */
export function useServiceOverview() {
  return useQuery({
    queryKey: [...calendarKeys.all, 'overview'],
    queryFn: async () => unwrap(await api.GET('/api/calendars/overview')),
  })
}
