/** [dužina, širina] */
export type LngLat = [number, number]

/** Vrijeme na stanici vožnje, u sekundama od ponoći dana karte. */
export interface TrackStopTime {
  stopId: string
  arrivalTime: number
  departureTime: number
}

/** Crta s prijeđenom udaljenošću (u metrima) do svake točke. */
export interface Path {
  points: LngLat[]
  cumulative: number[]
}

/** Vožnja spremna za simulaciju: crta i udaljenost svake stanice duž nje. */
export interface Track {
  stopTimes: TrackStopTime[]
  path: Path
  stopDistances: number[]
}

/** Gdje je vozilo u trenutku: na stanici ili između dvije. */
export interface VehicleState {
  position: LngLat
  /** Zadnja stanica na kojoj je vozilo bilo ili je sada. */
  stopIndex: number
  /** Vozilo stoji na stanici stopIndex. */
  atStop: boolean
}

const earthRadius = 6_371_000

/** Udaljenost u metrima; za kratke dionice dovoljno je ravno preslikavanje. */
function distance(a: LngLat, b: LngLat): number {
  const latitude = (((a[1] + b[1]) / 2) * Math.PI) / 180
  const dx = (((b[0] - a[0]) * Math.PI) / 180) * Math.cos(latitude) * earthRadius
  const dy = (((b[1] - a[1]) * Math.PI) / 180) * earthRadius
  return Math.hypot(dx, dy)
}

export function makePath(points: LngLat[]): Path {
  const cumulative = [0]
  for (let i = 1; i < points.length; i++) {
    cumulative.push(cumulative[i - 1] + distance(points[i - 1], points[i]))
  }
  return { points, cumulative }
}

/**
 * Udaljenost duž crte do točke crte najbliže zadanoj, ne prije `from`; tako
 * stanice idu redom i kad crta prolazi istim mjestom dvaput (odvojak tamo i
 * natrag). Kod jednako bliskih bira raniju.
 */
export function project(path: Path, point: LngLat, from = 0): number {
  let best = Infinity
  let bestAlong = Math.max(from, 0)
  for (let i = 0; i < path.points.length - 1; i++) {
    const start = path.cumulative[i]
    const length = path.cumulative[i + 1] - start
    if (start + length < from) {
      continue
    }
    const a = path.points[i]
    const b = path.points[i + 1]
    // Projekcija u lokalnim metrima oko početka dionice.
    const scale = Math.cos((a[1] * Math.PI) / 180)
    const bx = (b[0] - a[0]) * scale
    const by = b[1] - a[1]
    const px = (point[0] - a[0]) * scale
    const py = point[1] - a[1]
    const squared = bx * bx + by * by
    let t = squared === 0 ? 0 : Math.min(Math.max((px * bx + py * by) / squared, 0), 1)
    if (start + t * length < from) {
      t = length === 0 ? 0 : (from - start) / length
    }
    const gap = (px - t * bx) ** 2 + (py - t * by) ** 2
    if (gap < best) {
      best = gap
      bestAlong = start + t * length
    }
  }
  return bestAlong
}

/** Točka crte na zadanoj udaljenosti od početka. */
export function pointAt(path: Path, along: number): LngLat {
  const { points, cumulative } = path
  if (along <= 0 || points.length === 1) {
    return points[0]
  }
  for (let i = 1; i < points.length; i++) {
    if (cumulative[i] >= along) {
      const length = cumulative[i] - cumulative[i - 1]
      const t = length === 0 ? 0 : (along - cumulative[i - 1]) / length
      const [a, b] = [points[i - 1], points[i]]
      return [a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t]
    }
  }
  return points[points.length - 1]
}

/**
 * Vožnja po trasi; bez trase ravno od stanice do stanice. Stanice se
 * postavljaju na trasu redom (najbliža točka iza prethodne stanice).
 */
export function buildTrack(
  stopTimes: TrackStopTime[],
  stops: LngLat[],
  shape: LngLat[] | null,
): Track {
  if (!shape || shape.length < 2) {
    const path = makePath(stops)
    return { stopTimes, path, stopDistances: path.cumulative }
  }
  const path = makePath(shape)
  const stopDistances: number[] = []
  let from = 0
  for (const stop of stops) {
    from = project(path, stop, from)
    stopDistances.push(from)
  }
  return { stopTimes, path, stopDistances }
}

/**
 * Položaj vozila u trenutku `time` (sekunde od ponoći dana karte): između
 * polaska s jedne i dolaska na sljedeću stanicu jednoliko duž trase. Null
 * prije dolaska na prvu i nakon dolaska na zadnju stanicu.
 */
export function vehicleAt(track: Track, time: number): VehicleState | null {
  const { stopTimes, stopDistances, path } = track
  const last = stopTimes.length - 1
  if (last < 1 || time < stopTimes[0].arrivalTime || time > stopTimes[last].arrivalTime) {
    return null
  }
  for (let i = 0; i <= last; i++) {
    const stop = stopTimes[i]
    if (time <= stop.departureTime) {
      if (time >= stop.arrivalTime || i === 0) {
        return { position: pointAt(path, stopDistances[i]), stopIndex: i, atStop: true }
      }
      const previous = stopTimes[i - 1]
      const duration = stop.arrivalTime - previous.departureTime
      const t = duration <= 0 ? 1 : (time - previous.departureTime) / duration
      const along = stopDistances[i - 1] + (stopDistances[i] - stopDistances[i - 1]) * t
      return { position: pointAt(path, along), stopIndex: i - 1, atStop: false }
    }
  }
  return { position: pointAt(path, stopDistances[last]), stopIndex: last, atStop: true }
}
