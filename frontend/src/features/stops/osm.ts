import { distance } from '@/features/variants/variantMath'

/** Stajalište iz OpenStreetMapa, kandidat za novu stanicu. */
export interface OsmStop {
  id: number
  name: string
  /** OSM local_ref: oznaka perona. */
  platformCode: string | null
  /** OSM ref: broj stajališta za putnike. */
  publicCode: string | null
  latitude: number
  longitude: number
  /** GTFS wheelchair_boarding: 0 nepoznato, 1 da, 2 ne. */
  wheelchair: number
}

interface OverpassNode {
  type: string
  id: number
  lat: number
  lon: number
  tags?: Record<string, string>
}

/** Najviše ovoliko stupnjeva po strani područja; veće bi opteretilo javni Overpass. */
export const maxOsmArea = 0.25

/** Overpass upit za stajališta autobusa, tramvaja i vlaka unutar područja. */
export function overpassQuery(south: number, west: number, north: number, east: number): string {
  const box = [south, west, north, east].map((v) => v.toFixed(5)).join(',')
  return `[out:json][timeout:25];(
node["highway"="bus_stop"](${box});
node["railway"="tram_stop"](${box});
node["public_transport"="platform"](${box});
node["railway"="halt"](${box});
);out body 3000;`
}

/**
 * Stajališta iz Overpass odgovora: bez naziva se preskaču, a isti naziv unutar
 * 15 m (npr. stajalište i točka zaustavljanja) je jedno stajalište.
 */
export function parseOverpass(body: { elements: OverpassNode[] }): {
  stops: OsmStop[]
  unnamed: number
} {
  const stops: OsmStop[] = []
  let unnamed = 0
  for (const node of body.elements) {
    if (node.type !== 'node') {
      continue
    }
    const tags = node.tags ?? {}
    const name = tags.name?.trim()
    if (!name) {
      unnamed++
      continue
    }
    const duplicate = stops.some(
      (s) => s.name === name && distance([s.latitude, s.longitude], [node.lat, node.lon]) < 15,
    )
    if (duplicate) {
      continue
    }
    stops.push({
      id: node.id,
      name,
      platformCode: tags.local_ref?.trim() || null,
      publicCode: tags.ref?.trim() || null,
      latitude: node.lat,
      longitude: node.lon,
      wheelchair: tags.wheelchair === 'yes' ? 1 : tags.wheelchair === 'no' ? 2 : 0,
    })
  }
  return { stops, unnamed }
}

/** Stajališta kojima unutar 20 m nema postojeće stanice. */
export function withoutExisting(
  stops: OsmStop[],
  existing: { latitude: number | null; longitude: number | null }[],
): OsmStop[] {
  const placed = existing.filter((e) => e.latitude !== null && e.longitude !== null)
  return stops.filter(
    (s) =>
      !placed.some((e) => distance([s.latitude, s.longitude], [e.latitude!, e.longitude!]) < 20),
  )
}
