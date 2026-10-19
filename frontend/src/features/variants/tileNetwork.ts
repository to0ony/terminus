import { VectorTile } from '@mapbox/vector-tile'
import { PbfReader } from 'pbf'
import type { LngLat, NetworkKind, OsmWay } from './osmNetwork'

/**
 * Ceste i pruge iz vektorskih pločica OpenFreeMapa (sloj transportation,
 * OpenMapTiles shema), iste one iz kojih se crta podloga karte. Pločice dolaze
 * s CDN-a za sekundu-dvije, a javni Overpass za isti koridor zna trajati pola
 * minute ili pasti.
 */

/** Id-evi puteva iz pločica, jedinstveni i kad se dohvati još pločica. */
let nextWayId = 1

/** Pločice s najviše detalja; dovoljno za ulice i pruge. */
export const tileZoom = 14

/** Linija iz pločice u globalnim cjelobrojnim koordinatama na zoomu 14 (extent 4096). */
export interface TileLine {
  coords: [number, number][]
  props: Record<string, string | number | boolean>
}

/** Točke koje dijele jednu cjelobrojnu koordinatu pločice su isti čvor. */
const nodeId = ([x, y]: [number, number]) => x * 2 ** 27 + y

/** Kraj ulice ovoliko blizu druge ulice (jedinice pločice, ~0,4 m) spaja se na nju. */
const junctionTolerance = 4

const roadClasses: Record<string, string> = {
  motorway: 'motorway',
  trunk: 'trunk',
  primary: 'primary',
  secondary: 'secondary',
  tertiary: 'tertiary',
  minor: 'residential',
  service: 'service',
  busway: 'busway',
}

const railSubclasses: Record<NetworkKind, string[]> = {
  road: [],
  tram: ['tram', 'light_rail'],
  rail: ['rail', 'light_rail', 'narrow_gauge', 'subway', 'monorail'],
}

/** Globalna koordinata pločice → [dužina, širina]. */
export function toLngLat([x, y]: [number, number], extent = 4096): LngLat {
  const size = 2 ** tileZoom * extent
  const lon = (x / size) * 360 - 180
  const n = Math.PI - (2 * Math.PI * y) / size
  return [lon, (180 / Math.PI) * Math.atan(Math.sinh(n))]
}

/** Pločice (x, y) na zoomu 14 koje pokrivaju pravokutnik. */
export function tilesFor(west: number, south: number, east: number, north: number): string[] {
  const n = 2 ** tileZoom
  const x = (lon: number) => Math.floor(((lon + 180) / 360) * n)
  const y = (lat: number) => {
    const r = (lat * Math.PI) / 180
    return Math.floor(((1 - Math.log(Math.tan(r) + 1 / Math.cos(r)) / Math.PI) / 2) * n)
  }
  const result: string[] = []
  for (let tx = x(west); tx <= x(east); tx++) {
    for (let ty = y(north); ty <= y(south); ty++) {
      result.push(`${tx}/${ty}`)
    }
  }
  return result
}

/** Oznake kao u OSM-u, da mreža ima ista pravila za pločice i za Overpass. */
function tags(props: TileLine['props'], kind: NetworkKind): Record<string, string> | null {
  const cls = String(props.class ?? '')
  const subclass = String(props.subclass ?? '')
  if (kind !== 'road') {
    return (cls === 'rail' || cls === 'transit') && railSubclasses[kind].includes(subclass)
      ? { railway: subclass, oneway: oneway(props.oneway) }
      : null
  }
  const highway = roadClasses[cls]
  if (!highway) {
    return null
  }
  const result: Record<string, string> = { highway, oneway: oneway(props.oneway) }
  if (props.service) {
    result.service = String(props.service)
  }
  if (props.access) {
    result.access = String(props.access)
  }
  return result
}

const oneway = (value: TileLine['props'][string] | undefined) =>
  value === 1 ? 'yes' : value === -1 ? '-1' : 'no'

/**
 * Ceste iz pločica kao OSM putevi. Čvor je cjelobrojna koordinata pločice, pa
 * se ulice koje dijele točku spajaju same. Pločice su pojednostavljene, pa kraj
 * sporedne ulice ne mora biti točka glavne: takav kraj umeće se u najbližu
 * ulicu do ~1,5 m, inače bi raskrižje bilo prekinuto.
 */
export function waysFromTiles(lines: TileLine[], kind: NetworkKind, extent = 4096): OsmWay[] {
  const kept = lines
    .map((line) => ({ coords: [...line.coords], tags: tags(line.props, kind) }))
    .filter(
      (line): line is { coords: [number, number][]; tags: Record<string, string> } =>
        line.tags !== null && line.coords.length > 1,
    )

  // Koliko linija prolazi kroz svaku točku.
  const users = new Map<number, number>()
  for (const line of kept) {
    for (const id of new Set(line.coords.map(nodeId))) {
      users.set(id, (users.get(id) ?? 0) + 1)
    }
  }

  // Mreža za brzo traženje dijelova linija blizu točke.
  const cell = 64
  const grid = new Map<string, { line: number; index: number }[]>()
  kept.forEach((line, l) => {
    for (let i = 1; i < line.coords.length; i++) {
      const [a, b] = [line.coords[i - 1], line.coords[i]]
      for (
        let cx = Math.floor(Math.min(a[0], b[0]) / cell);
        cx <= Math.floor(Math.max(a[0], b[0]) / cell);
        cx++
      ) {
        for (
          let cy = Math.floor(Math.min(a[1], b[1]) / cell);
          cy <= Math.floor(Math.max(a[1], b[1]) / cell);
          cy++
        ) {
          const key = `${cx}/${cy}`
          const list = grid.get(key)
          if (list) list.push({ line: l, index: i })
          else grid.set(key, [{ line: l, index: i }])
        }
      }
    }
  })

  // Točke koje treba umetnuti u druge linije: linija → [indeks dijela, točka].
  const inserts = new Map<number, { index: number; point: [number, number]; t: number }[]>()
  kept.forEach((line, l) => {
    for (const end of [line.coords[0], line.coords.at(-1)!]) {
      if ((users.get(nodeId(end)) ?? 0) > 1) {
        continue
      }
      const candidates = grid.get(`${Math.floor(end[0] / cell)}/${Math.floor(end[1] / cell)}`) ?? []
      let best: { line: number; index: number; t: number; d: number } | null = null
      for (const { line: other, index } of candidates) {
        if (other === l) {
          continue
        }
        const a = kept[other].coords[index - 1]
        const b = kept[other].coords[index]
        const dx = b[0] - a[0]
        const dy = b[1] - a[1]
        const length = dx * dx + dy * dy
        const t =
          length === 0
            ? 0
            : Math.max(0, Math.min(1, ((end[0] - a[0]) * dx + (end[1] - a[1]) * dy) / length))
        const d = Math.hypot(a[0] + dx * t - end[0], a[1] + dy * t - end[1])
        if (d <= junctionTolerance && (!best || d < best.d)) {
          best = { line: other, index, t, d }
        }
      }
      if (best && best.t > 0 && best.t < 1) {
        const list = inserts.get(best.line) ?? []
        list.push({ index: best.index, point: end, t: best.t })
        inserts.set(best.line, list)
      } else if (best) {
        // Kraj je uz krajnju točku dijela: ista točka.
        const target =
          best.t === 0 ? kept[best.line].coords[best.index - 1] : kept[best.line].coords[best.index]
        if (line.coords[0] === end) line.coords[0] = target
        else line.coords[line.coords.length - 1] = target
      }
    }
  })
  for (const [l, list] of inserts) {
    const coords = kept[l].coords
    // Od kraja prema početku, da indeksi ostanu točni.
    list.sort((a, b) => b.index - a.index || b.t - a.t)
    for (const { index, point } of list) {
      coords.splice(index, 0, point)
    }
  }

  return kept.map((line) => ({
    type: 'way',
    id: nextWayId++,
    nodes: line.coords.map(nodeId),
    geometry: line.coords.map((p) => {
      const [lon, lat] = toLngLat(p, extent)
      return { lon, lat }
    }),
    tags: line.tags,
  }))
}

/** TileJSON podloge karte; adresa pločica sadrži verziju, pa se čita odande. */
const tileJsonUrl = 'https://tiles.openfreemap.org/planet'
let tileTemplate: Promise<string> | null = null

function template(): Promise<string> {
  tileTemplate ??= fetch(tileJsonUrl)
    .then((response) => {
      if (!response.ok) {
        throw new Error(`OpenFreeMap je vratio grešku ${response.status}.`)
      }
      return response.json() as Promise<{ tiles: string[] }>
    })
    .then((json) => json.tiles[0])
    .catch((error: unknown) => {
      // Sljedeći pokušaj ide ispočetka.
      tileTemplate = null
      throw error
    })
  return tileTemplate
}

/** Najviše pločica odjednom, da preglednik ne otvori stotinu zahtjeva. */
const parallel = 6

/** Ceste i pruge iz pločica (x/y na zoomu 14), s globalnim koordinatama. */
export async function fetchTileLines(keys: string[], signal?: AbortSignal): Promise<TileLine[]> {
  let url: string
  try {
    url = await template()
  } catch {
    throw new Error('Karta s cestama (OpenFreeMap) trenutno nije dostupna. Pokušaj ponovno.')
  }
  const lines: TileLine[] = []
  const queue = [...keys]
  const worker = async () => {
    for (let key = queue.pop(); key; key = queue.pop()) {
      const [x, y] = key.split('/').map(Number)
      const response = await fetch(
        url.replace('{z}', String(tileZoom)).replace('{x}', String(x)).replace('{y}', String(y)),
        { signal },
      )
      // Pločica bez ičega (npr. more) zna doći kao 204 ili 404.
      if (response.status === 204 || response.status === 404) {
        continue
      }
      if (!response.ok) {
        throw new Error(`Karta s cestama (OpenFreeMap) je vratila grešku ${response.status}.`)
      }
      const layer = new VectorTile(new PbfReader(await response.arrayBuffer())).layers
        .transportation
      if (!layer) {
        continue
      }
      for (let i = 0; i < layer.length; i++) {
        const feature = layer.feature(i)
        // Samo linije (ceste, pruge), ne površine.
        if (feature.type !== 2) {
          continue
        }
        const scale = 4096 / layer.extent
        for (const ring of feature.loadGeometry()) {
          lines.push({
            coords: ring.map((p): [number, number] => [
              Math.round((x * layer.extent + p.x) * scale),
              Math.round((y * layer.extent + p.y) * scale),
            ]),
            props: feature.properties,
          })
        }
      }
    }
  }
  await Promise.all(Array.from({ length: Math.min(parallel, keys.length) }, worker))
  return lines
}
