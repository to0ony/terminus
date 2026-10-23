import { distance } from './variantMath'

/** Stanica varijante s vremenom vožnje od prethodne i stajanjem (sekunde). */
export interface ReversibleStop {
  stopId: string
  name: string
  platformCode: string | null
  latitude: number | null
  longitude: number | null
  travelTime: number
  dwellTime: number
  /** Neaktivnu stanicu vožnje preskaču; bez vrijednosti je aktivna. */
  isActive?: boolean
}

/** Stanica s karte koja može biti par s druge strane ceste. */
export interface PairCandidate {
  id: string
  name: string
  platformCode: string | null
  latitude: number | null
  longitude: number | null
  locationType: number
  isActive: boolean
}

export interface ReversedStop extends ReversibleStop {
  /** Za stanicu nije nađen par, pa je ostala ista (npr. okretište); treba je provjeriti. */
  unpaired: boolean
}

/** Najveća udaljenost para s druge strane ceste ili na drugom peronu. */
export const maxPairDistance = 400

const normalized = (name: string) =>
  name
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, ' ')
    .trim()

/**
 * Povratni smjer varijante: obrnut redoslijed, a svaka stanica zamijenjena
 * najbližom aktivnom stanicom istog naziva (do 400 m) koja nije u polaznom smjeru.
 * Vrijeme vožnje dionice ostaje isto, samo u suprotnom smjeru.
 */
export function reverseStops(stops: ReversibleStop[], candidates: PairCandidate[]): ReversedStop[] {
  const used = new Set(stops.map((s) => s.stopId))
  const byName = new Map<string, PairCandidate[]>()
  for (const candidate of candidates) {
    // Vožnje staju na stajalištima i peronima, ne na kolodvoru.
    if (candidate.locationType !== 0 || !candidate.isActive || used.has(candidate.id)) {
      continue
    }
    const key = normalized(candidate.name)
    byName.set(key, [...(byName.get(key) ?? []), candidate])
  }

  const pair = (stop: ReversibleStop): PairCandidate | null => {
    if (stop.latitude === null || stop.longitude === null) {
      return null
    }
    const from: [number, number] = [stop.latitude, stop.longitude]
    let best: PairCandidate | null = null
    let bestDistance = maxPairDistance
    for (const candidate of byName.get(normalized(stop.name)) ?? []) {
      if (candidate.latitude === null || candidate.longitude === null) {
        continue
      }
      const meters = distance(from, [candidate.latitude, candidate.longitude])
      if (meters <= bestDistance) {
        best = candidate
        bestDistance = meters
      }
    }
    return best
  }

  const n = stops.length
  return stops
    .slice()
    .reverse()
    .map((stop, k) => {
      const match = pair(stop)
      return {
        stopId: match?.id ?? stop.stopId,
        name: match?.name ?? stop.name,
        platformCode: match ? match.platformCode : stop.platformCode,
        latitude: match ? match.latitude : stop.latitude,
        longitude: match ? match.longitude : stop.longitude,
        // Dionica do k-te stanice povratnog smjera je dionica iza nje u polaznom.
        travelTime: k === 0 ? 0 : stops[n - k].travelTime,
        dwellTime: stop.dwellTime,
        isActive: match ? true : stop.isActive,
        unpaired: match === null,
      }
    })
}
