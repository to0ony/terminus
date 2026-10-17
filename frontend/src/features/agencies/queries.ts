import { queryOptions, useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type Agency = components['schemas']['AgencyResponse']

export const agencyKeys = {
  all: ['agencies'] as const,
}

/**
 * Svi prijevoznici (za filtre i nazive uz linije). Instanca ih ima malo, pa je
 * dovoljna jedna stranica najveće veličine.
 */
export const agenciesQuery = queryOptions({
  queryKey: agencyKeys.all,
  queryFn: async () =>
    unwrap(await api.GET('/api/agencies', { params: { query: { PageSize: 100 } } })).items,
})

export function useAgencies() {
  return useQuery(agenciesQuery)
}
