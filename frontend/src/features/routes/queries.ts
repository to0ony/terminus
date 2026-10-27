import {
  keepPreviousData,
  queryOptions,
  type UseMutationResult,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components, paths } from '@/api/schema'
import { type DownloadedFile, fileNameOf } from '@/lib/download'

export type Route = components['schemas']['RouteResponse']
export type TimeMode = components['schemas']['TimeMode']
export type RouteListParams = NonNullable<paths['/api/routes']['get']['parameters']['query']>
export type RouteDiagramPdfParams = NonNullable<
  paths['/api/routes/{id}/diagram.pdf']['get']['parameters']['query']
>

/** Preuzimanje PDF sheme: jedne linije ili svih linija koje staju na stanici. */
export type RouteDiagramPdfMutation = UseMutationResult<
  DownloadedFile,
  Error,
  RouteDiagramPdfParams
>

export type RouteTableRow = components['schemas']['RouteTableRowResponse']
export type RouteTableParams = NonNullable<paths['/api/routes/table']['get']['parameters']['query']>
export type RouteSortField = components['schemas']['RouteSortField']

export const routeKeys = {
  all: ['routes'] as const,
  list: (params: RouteListParams) => [...routeKeys.all, 'list', params] as const,
  table: (params: RouteTableParams) => [...routeKeys.all, 'table', params] as const,
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

/** Stranica tablice linija; dok se učitava sljedeća, ostaje prikazana prethodna. */
export function useRouteTable(params: RouteTableParams) {
  return useQuery({
    queryKey: routeKeys.table(params),
    queryFn: async () => unwrap(await api.GET('/api/routes/table', { params: { query: params } })),
    placeholderData: keepPreviousData,
  })
}

/** Aktivira ili deaktivira odabrane linije. */
export function useSetRoutesActive() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: { ids: string[]; isActive: boolean }) =>
      unwrap(await api.POST('/api/routes/bulk/active', { body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: routeKeys.all }),
    meta: { handlesErrors: true },
  })
}

/** Premješta odabrane linije drugom prijevozniku; vožnje i vozni red ostaju. */
export function useSetRoutesAgency() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: { ids: string[]; agencyId: string }) =>
      unwrap(await api.POST('/api/routes/bulk/agency', { body })),
    // I broj linija na stranici Prijevoznici dolazi iz upita linija.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: routeKeys.all }),
    meta: { handlesErrors: true },
  })
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

/**
 * PDF shema linije. Bez varijante sa stanicama backend vraća 422, a
 * ApiError.problems kaže zašto.
 */
export function useRouteDiagramPdf(id: string) {
  return useMutation({
    mutationFn: async (params: RouteDiagramPdfParams): Promise<DownloadedFile> => {
      const result = await api.GET('/api/routes/{id}/diagram.pdf', {
        params: { path: { id }, query: params },
        parseAs: 'blob',
      })
      const blob = unwrap(result) as unknown as Blob
      return { blob, fileName: fileNameOf(result.response) ?? 'linija.pdf' }
    },
    meta: { handlesErrors: true },
  })
}
