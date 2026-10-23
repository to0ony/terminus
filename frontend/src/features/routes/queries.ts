import {
  keepPreviousData,
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components, paths } from '@/api/schema'

export type Route = components['schemas']['RouteResponse']
export type TimeMode = components['schemas']['TimeMode']
export type RouteListParams = NonNullable<paths['/api/routes']['get']['parameters']['query']>

export const routeKeys = {
  all: ['routes'] as const,
  list: (params: RouteListParams) => [...routeKeys.all, 'list', params] as const,
}

export function routeListQuery(params: RouteListParams) {
  return queryOptions({
    queryKey: routeKeys.list(params),
    queryFn: async () => unwrap(await api.GET('/api/routes', { params: { query: params } })),
  })
}

/** Stranica popisa linija; dok se učitava sljedeća, ostaje prikazana prethodna. */
export function useRoutes(params: RouteListParams) {
  return useQuery({ ...routeListQuery(params), placeholderData: keepPreviousData })
}

export type RoutePattern = components['schemas']['RoutePatternResponse']
export type RouteTimetable = components['schemas']['RouteTimetableResponse']
export type TimetableParams = NonNullable<
  paths['/api/routes/{id}/timetable']['get']['parameters']['query']
>

export function useRoute(id: string) {
  return useQuery({
    queryKey: [...routeKeys.all, id],
    queryFn: async () => unwrap(await api.GET('/api/routes/{id}', { params: { path: { id } } })),
  })
}

/** Redoslijedi stanica po smjeru, od najčešćeg. */
export function useRoutePatterns(id: string) {
  return useQuery({
    queryKey: [...routeKeys.all, id, 'patterns'],
    queryFn: async () =>
      unwrap(await api.GET('/api/routes/{id}/patterns', { params: { path: { id } } })),
  })
}

export function useRouteTimetable(id: string, params: TimetableParams, enabled = true) {
  return useQuery({
    queryKey: [...routeKeys.all, id, 'timetable', params],
    queryFn: async () =>
      unwrap(
        await api.GET('/api/routes/{id}/timetable', {
          params: { path: { id }, query: params },
        }),
      ),
    placeholderData: keepPreviousData,
    enabled,
  })
}

export type RouteRequest = components['schemas']['RouteRequest']

/** Nova linija (bez Id-a) ili izmjena postojeće. */
export function useSaveRoute(id?: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: RouteRequest) =>
      id
        ? unwrap(await api.PUT('/api/routes/{id}', { params: { path: { id } }, body }))
        : unwrap(await api.POST('/api/routes', { body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: routeKeys.all }),
    meta: { handlesErrors: true },
  })
}

/** Briše liniju sa svim varijantama, vožnjama i pravilima cijena (kaskadno u bazi). */
export function useDeleteRoute() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) =>
      unwrap(await api.DELETE('/api/routes/{id}', { params: { path: { id } } })),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: routeKeys.all })
      // Linije stanica i polasci više ne uključuju obrisanu liniju.
      void queryClient.invalidateQueries({ queryKey: ['stops'] })
    },
    meta: { handlesErrors: true },
  })
}
