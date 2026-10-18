import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import { calendarKeys } from '@/features/calendars/queries'

export type Season = components['schemas']['SeasonResponse']
export type DayType = components['schemas']['DayTypeResponse']
export type SeasonDay = components['schemas']['SeasonDayResponse']
export type SeasonDayMode = components['schemas']['SeasonDayMode']
export type CreateSeasonRequest = components['schemas']['CreateSeasonRequest']
export type UpdateSeasonRequest = components['schemas']['UpdateSeasonRequest']
export type DayTypeRequest = components['schemas']['DayTypeRequest']
export type SeasonDayRequest = components['schemas']['SeasonDayRequest']

export const seasonKeys = {
  all: ['seasons'] as const,
  detail: (id: string) => [...seasonKeys.all, id] as const,
  holidayCountries: ['holiday-countries'] as const,
}

export function useSeasons() {
  return useQuery({
    queryKey: seasonKeys.all,
    queryFn: async () => unwrap(await api.GET('/api/seasons')),
  })
}

export function useSeason(id: string) {
  return useQuery({
    queryKey: seasonKeys.detail(id),
    queryFn: async () => unwrap(await api.GET('/api/seasons/{id}', { params: { path: { id } } })),
  })
}

/** Države s popisom praznika (OpenHolidays) i prijedlog prema zoni prijevoznika. */
export function useHolidayCountries() {
  return useQuery({
    queryKey: seasonKeys.holidayCountries,
    queryFn: async () => unwrap(await api.GET('/api/seasons/holiday-countries')),
    staleTime: 24 * 60 * 60_000,
    meta: { handlesErrors: true },
  })
}

/**
 * Nakon svake izmjene sezona dolazi natrag cijela; kalendari i pregled
 * voznog reda se osvježavaju jer su tipovi dana GTFS kalendari.
 */
function useSeasonMutation<T>(request: (body: T) => Promise<Season>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSuccess: (season) => {
      queryClient.setQueryData(seasonKeys.detail(season.id), season)
      void queryClient.invalidateQueries({ queryKey: seasonKeys.all, exact: true })
      void queryClient.invalidateQueries({ queryKey: calendarKeys.all })
    },
    meta: { handlesErrors: true },
  })
}

export function useCreateSeason() {
  return useSeasonMutation(async (body: CreateSeasonRequest) =>
    unwrap(await api.POST('/api/seasons', { body })),
  )
}

export function useUpdateSeason(id: string) {
  return useSeasonMutation(async (body: UpdateSeasonRequest) =>
    unwrap(await api.PUT('/api/seasons/{id}', { params: { path: { id } }, body })),
  )
}

export type CopySeasonRequest = components['schemas']['CopySeasonRequest']

/** Kopija sezone (s vožnjama ili bez njih) za drugo razdoblje. */
export function useCopySeason(id: string) {
  return useSeasonMutation(async (body: CopySeasonRequest) =>
    unwrap(await api.POST('/api/seasons/{id}/copy', { params: { path: { id } }, body })),
  )
}

export function useSaveDayTypes(id: string) {
  return useSeasonMutation(async (dayTypes: DayTypeRequest[]) =>
    unwrap(
      await api.PUT('/api/seasons/{id}/day-types', {
        params: { path: { id } },
        body: { dayTypes },
      }),
    ),
  )
}

export function useSetSeasonDay(id: string) {
  return useSeasonMutation(async ({ date, ...body }: SeasonDayRequest & { date: string }) =>
    unwrap(
      await api.PUT('/api/seasons/{id}/days/{date}', {
        params: { path: { id, date } },
        body,
      }),
    ),
  )
}

export function useResetSeasonDay(id: string) {
  return useSeasonMutation(async (date: string) =>
    unwrap(await api.DELETE('/api/seasons/{id}/days/{date}', { params: { path: { id, date } } })),
  )
}

export function useDeleteSeason() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (id: string) =>
      unwrap(await api.DELETE('/api/seasons/{id}', { params: { path: { id } } })),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: seasonKeys.all })
      void queryClient.invalidateQueries({ queryKey: calendarKeys.all })
    },
    meta: { handlesErrors: true },
  })
}
