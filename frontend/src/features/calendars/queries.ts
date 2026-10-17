import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components, paths } from '@/api/schema'

export type ServiceCalendar = components['schemas']['ServiceCalendarResponse']
export type CalendarDate = components['schemas']['CalendarDateResponse']
export type CalendarListParams = NonNullable<paths['/api/calendars']['get']['parameters']['query']>

export const calendarKeys = {
  all: ['calendars'] as const,
  list: (params: CalendarListParams) => [...calendarKeys.all, 'list', params] as const,
  detail: (id: string) => [...calendarKeys.all, id] as const,
}

export function useCalendars(params: CalendarListParams) {
  return useQuery({
    queryKey: calendarKeys.list(params),
    queryFn: async () => unwrap(await api.GET('/api/calendars', { params: { query: params } })),
    placeholderData: keepPreviousData,
  })
}

export function useCalendar(id: string) {
  return useQuery({
    queryKey: calendarKeys.detail(id),
    queryFn: async () => unwrap(await api.GET('/api/calendars/{id}', { params: { path: { id } } })),
  })
}

/** Iznimke kalendara (calendar_dates), po datumu. */
export function useCalendarDates(id: string) {
  return useQuery({
    queryKey: [...calendarKeys.detail(id), 'dates'],
    queryFn: async () =>
      unwrap(await api.GET('/api/calendars/{id}/dates', { params: { path: { id } } })),
  })
}
