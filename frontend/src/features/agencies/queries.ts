import { queryOptions, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import { routeKeys } from '@/features/routes/queries'

export type Agency = components['schemas']['AgencyResponse']
export type AgencyRequest = components['schemas']['AgencyRequest']

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

/** Nov prijevoznik (bez Id-a) ili izmjena postojećeg. */
export function useSaveAgency(id?: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: AgencyRequest) =>
      id
        ? unwrap(await api.PUT('/api/agencies/{id}', { params: { path: { id } }, body }))
        : unwrap(await api.POST('/api/agencies', { body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: agencyKeys.all }),
    meta: { handlesErrors: true },
  })
}

/** Briše prijevoznika; uz moveRoutesTo njegove linije prvo preuzima taj prijevoznik. */
export function useDeleteAgency() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async ({ id, moveRoutesTo }: { id: string; moveRoutesTo?: string }) =>
      unwrap(
        await api.DELETE('/api/agencies/{id}', {
          params: { path: { id }, query: moveRoutesTo ? { moveRoutesTo } : undefined },
        }),
      ),
    onSuccess: () =>
      Promise.all([
        queryClient.invalidateQueries({ queryKey: agencyKeys.all }),
        queryClient.invalidateQueries({ queryKey: routeKeys.all }),
      ]),
    meta: { handlesErrors: true },
  })
}
