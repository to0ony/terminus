import { useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type BoardDeparture = components['schemas']['DepartureResponse']
export type PublicSettings = components['schemas']['PublicSettingsResponse']

/** Koliko često zaslon traži nove polaske. */
export const refreshMs = 30_000

export interface Board {
  departures: components['schemas']['DeparturesResponse']
  /**
   * Razlika sata poslužitelja i preglednika (ms). Zasloni na stanicama često
   * imaju krivo namješten sat, pa se minute do polaska računaju po poslužitelju.
   */
  clockSkewMs: number
}

export const boardKeys = {
  departures: (stopCode: string, limit: number) =>
    ['board', 'departures', stopCode, limit] as const,
  settings: ['board', 'settings'] as const,
}

export function useBoard(stopCode: string, limit: number) {
  return useQuery({
    queryKey: boardKeys.departures(stopCode, limit),
    queryFn: async (): Promise<Board> => {
      const result = await api.GET('/api/public/departures/{stopCode}', {
        params: { path: { stopCode }, query: { Limit: limit } },
      })
      const departures = unwrap(result)
      // Odgovor iz cachea poslužitelja je star Age sekundi.
      const ageMs = Number(result.response.headers.get('Age') ?? 0) * 1000
      return {
        departures,
        clockSkewMs: Date.parse(departures.now) + ageMs - Date.now(),
      }
    },
    refetchInterval: refreshMs,
    refetchIntervalInBackground: true,
    staleTime: 0,
    meta: { handlesErrors: true },
  })
}

export function usePublicSettings() {
  return useQuery({
    queryKey: boardKeys.settings,
    queryFn: async () => unwrap(await api.GET('/api/public/settings')),
    staleTime: 5 * 60_000,
    refetchInterval: 5 * 60_000,
    meta: { handlesErrors: true },
  })
}
