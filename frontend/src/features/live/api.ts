import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { createContext, useContext } from 'react'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import {
  type GeoBounds,
  type StopAreaParams,
  useStopDepartures,
  useStopRoutes,
  useStopsExtent,
  useStopsInArea,
} from '@/features/stops/queries'
import { type LiveRouteDay, useLiveRouteDay } from './queries'

export type LiveNetworkDay = components['schemas']['LiveNetworkDayResponse']

/**
 * Odakle karta uživo dohvaća podatke: dashboard (s prijavom) ili javna karta
 * (/api/public/map, bez prijave). Karta je ista, mijenja se samo izvor.
 */
export interface LiveApi {
  isPublic: boolean
  useStopsInArea: (params: StopAreaParams | null) => {
    data?: components['schemas']['StopsInAreaResponse']
  }
  useStopsExtent: () => { data?: GeoBounds | null; isSuccess: boolean }
  useStopDepartures: (
    id: string,
    date: string,
  ) => { data?: components['schemas']['StopDayDeparturesResponse']; isPending: boolean }
  useStopRoutes: (id: string) => { data?: components['schemas']['RouteResponse'][] }
  useLiveRouteDay: (
    routeId: string | null,
    date: string,
  ) => { data?: LiveRouteDay; isPending: boolean }
  useLiveNetwork: (enabled: boolean, date: string) => { data?: LiveNetworkDay; isPending: boolean }
}

const adminApi: LiveApi = {
  isPublic: false,
  useStopsInArea,
  useStopsExtent,
  useStopDepartures,
  useStopRoutes,
  useLiveRouteDay,
  useLiveNetwork: (enabled, date) =>
    useQuery({
      queryKey: ['routes', 'live', date],
      queryFn: async () =>
        unwrap(await api.GET('/api/routes/live', { params: { query: { Date: date } } })),
      enabled,
      staleTime: 60_000,
    }),
}

const publicKey = 'public-map'

export const publicApi: LiveApi = {
  isPublic: true,
  useStopsInArea: (params) =>
    useQuery({
      queryKey: [publicKey, 'area', params],
      queryFn: async () =>
        unwrap(await api.GET('/api/public/map/stops/area', { params: { query: params! } })),
      placeholderData: keepPreviousData,
      enabled: params !== null,
    }),
  useStopsExtent: () =>
    useQuery({
      queryKey: [publicKey, 'extent'],
      queryFn: async () =>
        (unwrap(await api.GET('/api/public/map/stops/extent')) as GeoBounds | undefined) ?? null,
      staleTime: 5 * 60_000,
    }),
  useStopDepartures: (id, date) =>
    useQuery({
      queryKey: [publicKey, 'stops', id, 'departures', date],
      queryFn: async () =>
        unwrap(
          await api.GET('/api/public/map/stops/{id}/departures', {
            params: { path: { id }, query: { Date: date } },
          }),
        ),
      placeholderData: keepPreviousData,
    }),
  useStopRoutes: (id) =>
    useQuery({
      queryKey: [publicKey, 'stops', id, 'routes'],
      queryFn: async () =>
        unwrap(await api.GET('/api/public/map/stops/{id}/routes', { params: { path: { id } } })),
    }),
  useLiveRouteDay: (routeId, date) =>
    useQuery({
      queryKey: [publicKey, 'routes', routeId, 'live', date],
      queryFn: async () =>
        unwrap(
          await api.GET('/api/public/map/routes/{id}/live', {
            params: { path: { id: routeId! }, query: { Date: date } },
          }),
        ),
      enabled: routeId !== null,
      staleTime: 60_000,
    }),
  useLiveNetwork: (enabled, date) =>
    useQuery({
      queryKey: [publicKey, 'live', date],
      queryFn: async () =>
        unwrap(await api.GET('/api/public/map/live', { params: { query: { Date: date } } })),
      enabled,
      staleTime: 60_000,
    }),
}

export const LiveApiContext = createContext<LiveApi>(adminApi)

export function useLiveApi(): LiveApi {
  return useContext(LiveApiContext)
}
