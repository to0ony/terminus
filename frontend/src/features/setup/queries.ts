import { queryOptions, useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type SetupStatus = components['schemas']['SetupStatusResponse']

export const setupKeys = {
  status: ['setup'] as const,
}

/** Je li čarobnjak prvog pokretanja završen (prijevoznik stvoren ili GTFS uvezen). */
export const setupStatusQuery = queryOptions({
  queryKey: setupKeys.status,
  queryFn: async () => unwrap(await api.GET('/api/setup')),
})

export function useSetupStatus() {
  return useQuery(setupStatusQuery)
}
