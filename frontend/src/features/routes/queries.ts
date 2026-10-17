import { keepPreviousData, queryOptions, useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components, paths } from '@/api/schema'

export type Route = components['schemas']['RouteResponse']
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
