import type { RoutePattern } from './queries'

export interface DirectionOption {
  value: string
  label: string
}

/** Redoslijed stanica jednog smjera s brojem vožnji (obrazac ili varijanta). */
export interface DirectionSequence {
  directionId: number | null
  headsign: string | null
  tripCount: number
  stops: { name: string }[]
}

/** Cilj redoslijeda: natpis smjera, a bez njega posljednja stanica. */
const targetOf = (sequence: DirectionSequence) =>
  sequence.headsign ?? sequence.stops.at(-1)?.name ?? null

/**
 * Smjerovi za odabir, nazvani po najčešćem cilju: cilj kojim ide najviše
 * vožnji smjera, a bez vožnji cilj najduljeg redoslijeda.
 */
export function directionLabels(sequences: DirectionSequence[]): DirectionOption[] {
  const directions = new Map<number, Map<string, { trips: number; stops: number }>>()
  for (const sequence of sequences) {
    const target = targetOf(sequence)
    if (sequence.directionId === null || target === null) {
      continue
    }
    const targets = directions.get(sequence.directionId) ?? new Map()
    const current = targets.get(target) ?? { trips: 0, stops: 0 }
    targets.set(target, {
      trips: current.trips + sequence.tripCount,
      stops: Math.max(current.stops, sequence.stops.length),
    })
    directions.set(sequence.directionId, targets)
  }

  return [...directions.entries()]
    .sort(([a], [b]) => a - b)
    .map(([direction, targets]) => {
      const [target] = [...targets.entries()].reduce((best, next) =>
        next[1].trips > best[1].trips ||
        (next[1].trips === best[1].trips && next[1].stops > best[1].stops)
          ? next
          : best,
      )
      return { value: String(direction), label: `→ ${target}` }
    })
}

/**
 * Smjerovi linije za odabir prema obrascima vožnji. Linija bez smjerova u
 * GTFS-u ili sa samo jednim smjerom nema izbora.
 */
export function directionOptions(patterns: RoutePattern[]): DirectionOption[] {
  const options = directionLabels(patterns)
  return options.length < 2 ? [] : options
}
