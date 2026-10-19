import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import { changes, fromResponse, maxSeconds, retimeTrip, type ScheduleVariant } from './model'

export type Schedule = components['schemas']['ScheduleResponse']
export type ScheduleStop = components['schemas']['ScheduleStopResponse']
export type ScheduleSaveRequest = components['schemas']['ScheduleSaveRequest']

export const scheduleKeys = {
  all: (routeId: string) => ['routes', routeId, 'schedule'] as const,
  detail: (routeId: string, direction: number | null, calendarId: string | null) =>
    [...scheduleKeys.all(routeId), direction, calendarId] as const,
}

/** Vožnje smjera jednog kalendara (tipa dana) s vremenima, za grafikon. */
export function useSchedule(
  routeId: string,
  direction: number | null,
  calendarId: string | null,
  enabled = true,
) {
  return useQuery({
    queryKey: scheduleKeys.detail(routeId, direction, calendarId),
    queryFn: async () =>
      unwrap(
        await api.GET('/api/routes/{id}/schedule', {
          params: {
            path: { id: routeId },
            query: {
              DirectionId: direction ?? undefined,
              CalendarId: calendarId ?? undefined,
            },
          },
        }),
      ),
    enabled,
    // Grafikon se ne smije sam osvježiti dok se uređuje.
    staleTime: Infinity,
  })
}

/**
 * Presnimava vremena varijante u sve njezine vožnje (svi tipovi dana), uz isti
 * polazak s prve stanice. Vožnja koja ne odgovara stanicama varijante ili bi
 * završila iza 48:00 se preskače.
 */
export function useRetimeTrips(routeId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (variant: ScheduleVariant) => {
      const schedule = unwrap(
        await api.GET('/api/routes/{id}/schedule', {
          params: {
            path: { id: routeId },
            query: { DirectionId: variant.directionId ?? undefined },
          },
        }),
      )
      const original = schedule.trips.filter((t) => t.variantId === variant.id).map(fromResponse)
      const updated = original.flatMap((trip) => {
        const retimed = retimeTrip(trip, variant)
        return retimed && retimed.stopTimes.at(-1)!.departureTime <= maxSeconds ? [retimed] : []
      })
      const { upserts } = changes(original, updated)
      if (upserts.length > 0) {
        unwrap(
          await api.PUT('/api/routes/{id}/schedule', {
            params: { path: { id: routeId } },
            body: { directionId: variant.directionId, upserts, deletes: [] },
          }),
        )
      }
      return { changed: upserts.length, skipped: original.length - updated.length }
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['routes', routeId] })
    },
    meta: { handlesErrors: true },
  })
}

export function useSaveSchedule(routeId: string) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (body: ScheduleSaveRequest) =>
      unwrap(
        await api.PUT('/api/routes/{id}/schedule', { params: { path: { id: routeId } }, body }),
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['routes', routeId] })
      void queryClient.invalidateQueries({ queryKey: ['seasons'] })
    },
    meta: { handlesErrors: true },
  })
}
