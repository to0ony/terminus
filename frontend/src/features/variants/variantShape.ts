import { corridorRadius, type LngLat, metersBetween, type Network } from './osmNetwork'

/**
 * Dionica trase od stanice do stanice. Po mreži ide kroz međutočke najkraćim
 * putem po cestama ili prugama; ručna dionica je ravna crta kroz točke.
 */
export interface ShapeSegment {
  mode: 'network' | 'manual'
  /** Međutočke bez krajeva (stanica): kroz njih ide put ili ravna crta. */
  via: LngLat[]
  /** Točke dionice od prve do druge stanice. */
  points: LngLat[]
  /** Po mreži nije nađen put, pa je dionica ravna crta. */
  failed: boolean
}

/** Nova dionica bez puta: ravna crta dok se ne izračuna. */
export function straightSegment(from: LngLat, to: LngLat, via: LngLat[] = []): ShapeSegment {
  return { mode: 'network', via, points: [from, ...via, to], failed: false }
}

/**
 * Spremljena trasa podijeljena po stanicama: granica dionice je točka trase
 * najbliža stanici, tražena redom (kružna linija prolazi pored iste stanice dvaput).
 */
export function splitShape(shape: LngLat[], stops: LngLat[]): ShapeSegment[] {
  if (shape.length < 2 || stops.length < 2) {
    return []
  }
  const indexes: number[] = []
  let from = 0
  for (let s = 0; s < stops.length; s++) {
    // Zadnja stanica je na kraju trase, prva na početku.
    if (s === 0) {
      indexes.push(0)
      continue
    }
    if (s === stops.length - 1) {
      indexes.push(shape.length - 1)
      continue
    }
    // Do sljedeće stanice: inače bi se stanica "zalijepila" za kasniji prolaz.
    let best = from
    let bestMeters = Infinity
    const next = stops[s + 1]
    for (let i = from; i < shape.length; i++) {
      const meters = metersBetween(shape[i], stops[s])
      if (meters < bestMeters) {
        best = i
        bestMeters = meters
      }
      // Trasa se već jako primaknula sljedećoj stanici: dalje se ne traži.
      if (i > best && metersBetween(shape[i], next) < 20) {
        break
      }
    }
    indexes.push(best)
    from = best
  }
  return indexes.slice(1).map((end, i) => ({
    mode: 'network',
    via: [],
    points: shape.slice(indexes[i], end + 1),
    failed: false,
  }))
}

/** Cijela trasa iz dionica, bez ponovljene točke na spoju. */
export function joinSegments(segments: ShapeSegment[]): LngLat[] {
  const result: LngLat[] = []
  for (const segment of segments) {
    for (const point of segment.points) {
      const last = result.at(-1)
      if (!last || last[0] !== point[0] || last[1] !== point[1]) {
        result.push(point)
      }
    }
  }
  return result
}

/**
 * Dionica po mreži od stanice do stanice kroz međutočke. Ako se neka točka ne
 * može spojiti na cestu ili puta nema, dionica je ravna crta i označena.
 */
export function routeSegment(
  network: Network,
  from: LngLat,
  to: LngLat,
  via: LngLat[],
): ShapeSegment {
  const waypoints = [from, ...via, to]
  // Drugi pokušaj bez servisnih puteva: stanica spojena na parkiralište ne vodi nikamo.
  const points = connect(network, waypoints, false) ?? connect(network, waypoints, true)
  return points
    ? { mode: 'network', via, points, failed: false }
    : { mode: 'network', via, points: waypoints, failed: true }
}

function connect(network: Network, waypoints: LngLat[], mainOnly: boolean): LngLat[] | null {
  const nodes = waypoints.map(
    (p) => (mainOnly ? network.snap(p, undefined, true) : null) ?? network.snap(p),
  )
  if (nodes.some((n) => n === null)) {
    return null
  }
  const points: LngLat[] = []
  for (let i = 1; i < nodes.length; i++) {
    const path = network.route(nodes[i - 1]!, nodes[i]!)
    if (!path) {
      return null
    }
    points.push(...(i === 1 ? path : path.slice(1)))
  }
  return points
}

/** Ručna dionica: ravne crte kroz točke. */
export function manualSegment(from: LngLat, to: LngLat, via: LngLat[]): ShapeSegment {
  return { mode: 'manual', via, points: [from, ...via, to], failed: false }
}

/** Položaj točke uzduž crte: indeks najbliže točke crte. */
function along(points: LngLat[], point: LngLat): number {
  let best = 0
  let bestMeters = Infinity
  points.forEach((p, i) => {
    const meters = metersBetween(p, point)
    if (meters < bestMeters) {
      best = i
      bestMeters = meters
    }
  })
  return best
}

/**
 * Nova međutočka na mjestu gdje je crta povučena: ide između postojećih
 * međutočaka prema tome gdje je na crti povlačenje počelo.
 */
export function insertVia(segment: ShapeSegment, grabbed: LngLat, dropped: LngLat): LngLat[] {
  const at = along(segment.points, grabbed)
  const index = segment.via.filter((v) => along(segment.points, v) <= at).length
  return [...segment.via.slice(0, index), dropped, ...segment.via.slice(index)]
}

/** Dionica najbliža točki (npr. mjestu gdje je crta uhvaćena mišem). */
export function nearestSegment(segments: ShapeSegment[], point: LngLat): number {
  let best = 0
  let bestMeters = Infinity
  segments.forEach((segment, i) => {
    for (const p of segment.points) {
      const meters = metersBetween(p, point)
      if (meters < bestMeters) {
        best = i
        bestMeters = meters
      }
    }
  })
  return best
}

/**
 * Overpass koridor za dionice: oko svake dionice to širi što je dionica dulja,
 * jer ceste između udaljenih stanica (npr. međugradska linija) skreću od ravne crte.
 */
export function corridors(waypoints: LngLat[][], minRadius = corridorRadius): Corridor[] {
  const result: Corridor[] = []
  for (const points of waypoints) {
    if (points.length < 2) {
      continue
    }
    let length = 0
    for (let i = 1; i < points.length; i++) {
      length += metersBetween(points[i - 1], points[i])
    }
    const radius = Math.round(Math.min(3000, Math.max(minRadius, length * 0.3)))
    // Uzastopne dionice istog koridora (gradske, kratke) idu kao jedna crta.
    const last = result.at(-1)
    if (last && last.radius === radius && last.points.at(-1) === points[0]) {
      last.points.push(...points.slice(1))
    } else {
      result.push({ points: [...points], radius })
    }
  }
  return result
}

export interface Corridor {
  points: LngLat[]
  /** Metara oko crte. */
  radius: number
}
