import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

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
