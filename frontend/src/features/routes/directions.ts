import type { RoutePattern } from './queries'

export interface DirectionOption {
  value: string
  label: string
}

/**
 * Smjerovi linije za odabir, prema glavnom (prvom) obrascu svakog smjera:
 * natpis smjera, a bez njega posljednja stanica. Linija bez smjerova u GTFS-u
 * ili sa samo jednim smjerom nema izbora.
 */
export function directionOptions(patterns: RoutePattern[]): DirectionOption[] {
  const main = new Map<number, RoutePattern>()
  for (const pattern of patterns) {
    if (pattern.directionId !== null && !main.has(pattern.directionId)) {
      main.set(pattern.directionId, pattern)
    }
  }

  if (main.size < 2) {
    return []
  }

  return [...main.entries()]
    .sort(([a], [b]) => a - b)
    .map(([directionId, pattern]) => ({
      value: String(directionId),
      label: `→ ${pattern.headsign ?? pattern.stops.at(-1)?.name ?? `Smjer ${directionId}`}`,
    }))
}
