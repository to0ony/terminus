import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components, paths } from '@/api/schema'
import { type DownloadedFile, fileNameOf } from '@/lib/download'

export type Stop = components['schemas']['StopResponse']
export type MapStop = components['schemas']['TimetableStopResponse']
export type StopDeparture = components['schemas']['StopDayDepartureResponse']
export type GeoBounds = components['schemas']['GeoBoundsResponse']
export type StopListParams = NonNullable<paths['/api/stops']['get']['parameters']['query']>
export type StopAreaParams = paths['/api/stops/area']['get']['parameters']['query']
export type TimetablePdfParams = NonNullable<
  paths['/api/stops/{id}/timetable.pdf']['get']['parameters']['query']
>

export const stopKeys = {
  all: ['stops'] as const,
  list: (params: StopListParams) => [...stopKeys.all, 'list', params] as const,
  area: (params: StopAreaParams) => [...stopKeys.all, 'area', params] as const,
  extent: () => [...stopKeys.all, 'extent'] as const,
  detail: (id: string) => [...stopKeys.all, id] as const,
}

export function useStops(params: StopListParams) {
  return useQuery({
    queryKey: stopKeys.list(params),
    queryFn: async () => unwrap(await api.GET('/api/stops', { params: { query: params } })),
    placeholderData: keepPreviousData,
  })
}

/** Stanice unutar vidljivog dijela karte (najviše 2000, inače truncated). */
export function useStopsInArea(params: StopAreaParams | null) {
  return useQuery({
    queryKey: stopKeys.area(params!),
    queryFn: async () => unwrap(await api.GET('/api/stops/area', { params: { query: params! } })),
    placeholderData: keepPreviousData,
    enabled: params !== null,
  })
}

/** Područje svih stanica; null ako nijedna nema koordinate (204). */
export function useStopsExtent() {
  return useQuery({
    queryKey: stopKeys.extent(),
    queryFn: async () =>
      (unwrap(await api.GET('/api/stops/extent')) as GeoBounds | undefined) ?? null,
    staleTime: 5 * 60_000,
  })
}

export function useStop(id: string) {
  return useQuery({
    queryKey: stopKeys.detail(id),
    queryFn: async () => unwrap(await api.GET('/api/stops/{id}', { params: { path: { id } } })),
  })
}

export function useStopRoutes(id: string) {
  return useQuery({
    queryKey: [...stopKeys.detail(id), 'routes'],
    queryFn: async () =>
      unwrap(await api.GET('/api/stops/{id}/routes', { params: { path: { id } } })),
  })
}

/** Polasci jednog dana voznog reda; bez datuma današnji (u zoni prijevoznika). */
export function useStopDepartures(id: string, date: string | null) {
  return useQuery({
    queryKey: [...stopKeys.detail(id), 'departures', date],
    queryFn: async () =>
      unwrap(
        await api.GET('/api/stops/{id}/departures', {
          params: { path: { id }, query: { Date: date ?? undefined } },
        }),
      ),
    placeholderData: keepPreviousData,
  })
}

/**
 * PDF vozni red stanice. Bez polazaka u razdoblju backend vraća 422, a
 * ApiError.problems kaže zašto.
 */
export function useTimetablePdf(id: string) {
  return useMutation({
    mutationFn: async (params: TimetablePdfParams): Promise<DownloadedFile> => {
      const result = await api.GET('/api/stops/{id}/timetable.pdf', {
        params: { path: { id }, query: params },
        parseAs: 'blob',
      })
      const blob = unwrap(result) as unknown as Blob
      return { blob, fileName: fileNameOf(result.response) ?? 'vozni-red.pdf' }
    },
    meta: { handlesErrors: true },
  })
}

export type StopRequest = components['schemas']['StopRequest']

/** Nova stanica (bez Id-a) ili izmjena postojeće; osvježava kartu i popise. */
export function useSaveStop(id?: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: StopRequest) =>
      id
        ? unwrap(await api.PUT('/api/stops/{id}', { params: { path: { id } }, body }))
        : unwrap(await api.POST('/api/stops', { body })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: stopKeys.all }),
    meta: { handlesErrors: true },
  })
}

export function useDeleteStop() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) =>
      unwrap(await api.DELETE('/api/stops/{id}', { params: { path: { id } } })),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: stopKeys.all }),
    meta: { handlesErrors: true },
  })
}
