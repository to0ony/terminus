/**
 * Trasa po cestama i prugama iz OpenStreetMapa, bez rutera: Overpass vrati
 * ceste (ili pruge) u koridoru oko stanica, a najkraći put računa se ovdje.
 * Od pravila se poštuju samo jednosmjerne ulice.
 */

/** Točka [dužina, širina], kao u MapLibreu i GeoJSON-u. */
export type LngLat = [number, number]

/** Po čemu vozi vrsta prijevoza. */
export type NetworkKind = 'road' | 'tram' | 'rail'

/** Koridor oko stanica u kojem se traže ceste (m). */
export const corridorRadius = 300

/** Stanica dalje od ovoga od najbliže ceste ne spaja se na mrežu (m). */
export const maxSnapDistance = 150

/** Koliko bliže (m) mora biti servisni put od ulice da se stanica spoji na njega. */
const minorPenalty = 30

/** GTFS route_type (osnovni i prošireni) → mreža. */
export function networkKind(routeType: number): NetworkKind {
  const basic = routeType >= 100 ? Math.floor(routeType / 100) * 100 : routeType
  switch (basic) {
    case 0:
    case 900:
      return 'tram'
    case 1:
    case 2:
    case 12:
    case 100:
    case 400:
      return 'rail'
    default:
      return 'road'
  }
}

// Ceste po kojima smije vozilo; pješačke staze, poljski putevi i sl. ne.
const roadTypes = [
  'motorway',
  'motorway_link',
  'trunk',
  'trunk_link',
  'primary',
  'primary_link',
  'secondary',
  'secondary_link',
  'tertiary',
  'tertiary_link',
  'unclassified',
  'residential',
  'living_street',
  'service',
  'road',
  'busway',
  'bus_guideway',
]

const selectors: Record<NetworkKind, string> = {
  road: `way["highway"~"^(${roadTypes.join('|')})$"]`,
  tram: 'way["railway"~"^(tram|light_rail)$"]',
  rail: 'way["railway"~"^(rail|light_rail|narrow_gauge|subway|monorail)$"]',
}

/** Overpass upit za mrežu u koridorima oko crta (npr. stanica varijante). */
export function networkQuery(
  kind: NetworkKind,
  corridors: { points: LngLat[]; radius: number }[],
): string {
  const parts = corridors.map(({ points, radius }) => {
    const line = points.map(([lon, lat]) => `${lat.toFixed(6)},${lon.toFixed(6)}`).join(',')
    return `${selectors[kind]}(around:${radius},${line});`
  })
  return `[out:json][timeout:90];(${parts.join('')});out body geom qt;`
}

/** Odgovori preopterećenog Overpassa. */
const busy = new Set([429, 502, 503, 504])

/** Mreža iz javnog Overpassa (kao i prijedlozi stanica). */
export async function fetchNetwork(
  kind: NetworkKind,
  corridors: { points: LngLat[]; radius: number }[],
  signal?: AbortSignal,
): Promise<OverpassWay[]> {
  const request = () =>
    fetch('https://overpass-api.de/api/interpreter', {
      method: 'POST',
      body: new URLSearchParams({ data: networkQuery(kind, corridors) }),
      signal,
    })
  // Javni Overpass zna biti pun; jedan ponovni pokušaj nakon kratke stanke često prođe.
  let response = await request()
  if (busy.has(response.status)) {
    await new Promise((resolve) => setTimeout(resolve, 3000))
    response = await request()
  }
  if (!response.ok) {
    throw new Error(
      busy.has(response.status)
        ? 'OpenStreetMap (Overpass) je trenutno preopterećen. Pokušaj ponovno za minutu.'
        : `OpenStreetMap (Overpass) je vratio grešku ${response.status}.`,
    )
  }
  const body = (await response.json()) as { elements: OverpassWay[] }
  return body.elements
}

export interface OverpassWay {
  type: string
  id: number
  nodes?: number[]
  geometry?: { lat: number; lon: number }[]
  tags?: Record<string, string>
}

interface Edge {
  to: number
  meters: number
}

/** Usmjereni graf: čvorovi su OSM čvorovi i točke na koje su spojene stanice. */
export class Network {
  readonly positions = new Map<number, LngLat>()
  readonly edges = new Map<number, Edge[]>()
  /** Dijelovi cesta [od, do] za spajanje točke na najbližu cestu; sporedni su servisni putevi. */
  private readonly segments: { ends: [number, number]; minor: boolean }[] = []
  private readonly seen = new Set<number>()
  /** Točke spojene na cestu: dio ceste i udio duljine na njemu. */
  private readonly snapped = new Map<number, { segment: [number, number]; t: number }>()
  private nextVirtual = -1

  readonly kind: NetworkKind

  constructor(kind: NetworkKind) {
    this.kind = kind
  }

  get isEmpty() {
    return this.segments.length === 0
  }

  /** Dodaje ceste iz Overpass odgovora; ista cesta dvaput se ne dodaje. */
  add(ways: OverpassWay[]) {
    for (const way of ways) {
      if (way.type !== 'way' || !way.nodes || !way.geometry || this.seen.has(way.id)) {
        continue
      }
      const tags = way.tags ?? {}
      if (!usable(tags, this.kind)) {
        continue
      }
      this.seen.add(way.id)
      const direction = oneway(tags, this.kind)
      const minor = tags.highway === 'service'
      for (let i = 0; i < way.nodes.length; i++) {
        this.positions.set(way.nodes[i], [way.geometry[i].lon, way.geometry[i].lat])
      }
      for (let i = 1; i < way.nodes.length; i++) {
        const a = way.nodes[i - 1]
        const b = way.nodes[i]
        const meters = metersBetween(this.positions.get(a)!, this.positions.get(b)!)
        if (direction >= 0) {
          this.link(a, b, meters)
        }
        if (direction <= 0) {
          this.link(b, a, meters)
        }
        this.segments.push({ ends: [a, b], minor })
      }
    }
  }

  /**
   * Spaja točku na najbližu cestu: nova točka na toj cesti, povezana s krajevima
   * dijela ceste kao i sama cesta. Null ako je cesta dalje od <paramref name="limit"/>.
   * Stajalište je obično uz ulicu, ne uz parkiralište pokraj nje, pa servisni put
   * mora biti bliži za <see cref="minorPenalty"/>; uz <paramref name="mainOnly"/> se preskače.
   */
  snap(point: LngLat, limit = maxSnapDistance, mainOnly = false): number | null {
    let best: { segment: [number, number]; t: number; score: number } | null = null
    for (const { ends, minor } of this.segments) {
      if (mainOnly && minor) {
        continue
      }
      const { t, meters } = project(
        point,
        this.positions.get(ends[0])!,
        this.positions.get(ends[1])!,
      )
      const score = meters + (minor ? minorPenalty : 0)
      if (meters <= limit && (!best || score < best.score)) {
        best = { segment: ends, t, score }
      }
    }
    if (!best) {
      return null
    }
    const [a, b] = best.segment
    const pa = this.positions.get(a)!
    const pb = this.positions.get(b)!
    const id = this.nextVirtual--
    const at: LngLat = [pa[0] + (pb[0] - pa[0]) * best.t, pa[1] + (pb[1] - pa[1]) * best.t]
    this.positions.set(id, at)
    this.snapped.set(id, { segment: best.segment, t: best.t })
    const toA = metersBetween(at, pa)
    const toB = metersBetween(at, pb)
    // Smjer dijela ceste ostaje: kamo se moglo s a na b, može se i preko nove točke.
    if (this.hasEdge(a, b)) {
      this.link(a, id, toA)
      this.link(id, b, toB)
    }
    if (this.hasEdge(b, a)) {
      this.link(b, id, toB)
      this.link(id, a, toA)
    }
    return id
  }

  /** Najkraći put (Dijkstra) između čvorova, kao niz točaka; null ako ga nema. */
  route(from: number, to: number): LngLat[] | null {
    if (from === to) {
      return [this.positions.get(from)!]
    }
    // Obje točke na istom dijelu ceste: ravno, ako smjer ceste dopušta.
    const start = this.snapped.get(from)
    const end = this.snapped.get(to)
    if (start && end && start.segment === end.segment) {
      const [a, b] = start.segment
      if (end.t >= start.t ? this.hasEdge(a, b) : this.hasEdge(b, a)) {
        return [this.positions.get(from)!, this.positions.get(to)!]
      }
    }
    const distances = new Map<number, number>([[from, 0]])
    const previous = new Map<number, number>()
    const heap = new MinHeap()
    heap.push(from, 0)
    while (heap.size > 0) {
      const [node, cost] = heap.pop()
      if (node === to) {
        break
      }
      if (cost > (distances.get(node) ?? Infinity)) {
        continue
      }
      for (const edge of this.edges.get(node) ?? []) {
        const next = cost + edge.meters
        if (next < (distances.get(edge.to) ?? Infinity)) {
          distances.set(edge.to, next)
          previous.set(edge.to, node)
          heap.push(edge.to, next)
        }
      }
    }
    if (!distances.has(to)) {
      return null
    }
    const path: LngLat[] = []
    for (let node: number | undefined = to; node !== undefined; node = previous.get(node)) {
      path.push(this.positions.get(node)!)
    }
    return path.reverse()
  }

  private link(from: number, to: number, meters: number) {
    const list = this.edges.get(from)
    if (list) {
      list.push({ to, meters })
    } else {
      this.edges.set(from, [{ to, meters }])
    }
  }

  private hasEdge(from: number, to: number) {
    return (this.edges.get(from) ?? []).some((e) => e.to === to)
  }
}

/** Cesta po kojoj javni prijevoz ne smije (privatna, zatvorena, parkiralište) ne ulazi u mrežu. */
function usable(tags: Record<string, string>, kind: NetworkKind): boolean {
  if (kind !== 'road') {
    return true
  }
  if (tags.area === 'yes') {
    return false
  }
  if (
    tags.highway === 'service' &&
    ['parking_aisle', 'driveway', 'drive-through'].includes(tags.service)
  ) {
    return false
  }
  const allowed = (value?: string) =>
    value === 'yes' || value === 'designated' || value === 'permissive'
  const blocked = (value?: string) => value === 'no' || value === 'private'
  if (allowed(tags.bus) || allowed(tags.psv)) {
    return true
  }
  return !blocked(tags.access) && !blocked(tags.motor_vehicle) && !blocked(tags.vehicle)
}

/** 1 samo u smjeru crtanja ceste, -1 suprotno, 0 oba smjera. */
function oneway(tags: Record<string, string>, kind: NetworkKind): number {
  if (kind === 'road' && (tags['oneway:bus'] === 'no' || tags['oneway:psv'] === 'no')) {
    return 0
  }
  const value = tags.oneway
  if (value === '-1' || value === 'reverse') {
    return -1
  }
  if (value === 'yes' || value === '1' || value === 'true') {
    return 1
  }
  // Kružni tok je jednosmjeran i bez oznake.
  if (kind === 'road' && tags.junction === 'roundabout' && value !== 'no') {
    return 1
  }
  return 0
}

/** Udaljenost u metrima; na udaljenostima unutar grada ravna aproksimacija je dovoljna. */
export function metersBetween(a: LngLat, b: LngLat): number {
  const lat = (((a[1] + b[1]) / 2) * Math.PI) / 180
  const dx = (b[0] - a[0]) * 111_320 * Math.cos(lat)
  const dy = (b[1] - a[1]) * 110_540
  return Math.hypot(dx, dy)
}

/** Projekcija točke na dužinu ab: udio duljine (0–1) i udaljenost u metrima. */
function project(p: LngLat, a: LngLat, b: LngLat): { t: number; meters: number } {
  const cos = Math.cos((p[1] * Math.PI) / 180)
  const ax = (a[0] - p[0]) * 111_320 * cos
  const ay = (a[1] - p[1]) * 110_540
  const bx = (b[0] - p[0]) * 111_320 * cos
  const by = (b[1] - p[1]) * 110_540
  const dx = bx - ax
  const dy = by - ay
  const length = dx * dx + dy * dy
  const t = length === 0 ? 0 : Math.min(1, Math.max(0, -(ax * dx + ay * dy) / length))
  return { t, meters: Math.hypot(ax + dx * t, ay + dy * t) }
}

/** Binarna hrpa čvorova po cijeni, za Dijkstru. */
class MinHeap {
  private readonly items: [number, number][] = []

  get size() {
    return this.items.length
  }

  push(node: number, cost: number) {
    const items = this.items
    items.push([node, cost])
    let i = items.length - 1
    while (i > 0) {
      const parent = (i - 1) >> 1
      if (items[parent][1] <= items[i][1]) {
        break
      }
      ;[items[parent], items[i]] = [items[i], items[parent]]
      i = parent
    }
  }

  pop(): [number, number] {
    const items = this.items
    const top = items[0]
    const last = items.pop()!
    if (items.length > 0) {
      items[0] = last
      let i = 0
      for (;;) {
        const left = 2 * i + 1
        const right = left + 1
        let smallest = i
        if (left < items.length && items[left][1] < items[smallest][1]) {
          smallest = left
        }
        if (right < items.length && items[right][1] < items[smallest][1]) {
          smallest = right
        }
        if (smallest === i) {
          break
        }
        ;[items[smallest], items[i]] = [items[i], items[smallest]]
        i = smallest
      }
    }
    return top
  }
}
