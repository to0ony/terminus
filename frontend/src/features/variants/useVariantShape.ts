import { useMemo, useRef, useState } from 'react'
import type { DrawnSegment } from './ShapeLayers'
import { fetchNetwork, type LngLat, Network, type NetworkKind } from './osmNetwork'
import {
  corridors,
  insertVia,
  joinSegments,
  manualSegment,
  routeSegment,
  type ShapeSegment,
  splitShape,
} from './variantShape'

/** Stanica varijante: ključ razlikuje istu stanicu dvaput na kružnoj liniji. */
interface ShapeStop {
  key: number
  latitude: number | null
  longitude: number | null
}

interface Pair {
  key: string
  from: LngLat
  to: LngLat
}

/** Koridor za dionicu za koju put nije nađen u običnom (m). */
const widerCorridor = 1000

const pairKey = (a: ShapeStop, b: ShapeStop) => `${a.key}-${b.key}`
const position = (s: ShapeStop): LngLat | null =>
  s.latitude !== null && s.longitude !== null ? [s.longitude, s.latitude] : null

function pairsOf(stops: ShapeStop[]): Pair[] | null {
  const positions = stops.map(position)
  if (positions.some((p) => p === null)) {
    return null
  }
  return stops.slice(1).map((stop, i) => ({
    key: pairKey(stops[i], stop),
    from: positions[i]!,
    to: positions[i + 1]!,
  }))
}

interface Target {
  index: number
  via: LngLat[]
  mode: ShapeSegment['mode']
}

/**
 * Trasa varijante po dionicama između uzastopnih stanica. Dionica ostaje dok
 * postoji isti par stanica, pa promjena redoslijeda ponovno računa samo nove
 * dionice, a ručni popravci ostalih ostaju.
 */
export function useVariantShape(options: {
  stops: ShapeStop[]
  kind: NetworkKind
  /** Spremljena trasa i stanice za koje je spremljena. */
  initial: { points: LngLat[]; stops: ShapeStop[] } | null
}) {
  const { stops, kind, initial } = options
  const [store, setStore] = useState(() => {
    const pairs = initial && pairsOf(initial.stops)
    if (!initial || !pairs) {
      return new Map<string, ShapeSegment>()
    }
    const segments = splitShape(
      initial.points,
      initial.stops.map((s) => position(s)!),
    )
    return new Map(pairs.map((pair, i) => [pair.key, segments[i]]))
  })
  const [active, setActive] = useState(() => store.size > 0)
  const [changed, setChanged] = useState(false)
  const [computing, setComputing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const network = useRef<Network | null>(null)
  const fetched = useRef(new Set<string>())

  const pairs = useMemo(() => pairsOf(stops), [stops])
  const segments = useMemo(
    () => (pairs ?? []).map((pair) => store.get(pair.key) ?? null),
    [pairs, store],
  )
  const pending = segments.filter((s) => s === null).length
  // Isti niz dok se trasa ne promijeni, da karta ne postavlja točke pri svakom crtanju.
  const drawn = useMemo(
    () =>
      (pairs ?? []).map((pair, i): DrawnSegment => ({
        points: segments[i]?.points ?? [pair.from, pair.to],
        failed: segments[i]?.failed ?? false,
        pending: segments[i] === null,
        manual: segments[i]?.mode === 'manual',
        via: segments[i]?.via ?? [],
      })),
    [pairs, segments],
  )

  /** Dohvaća mrežu za koridore koji još nisu dohvaćeni; false ako nema ništa novo. */
  const load = async (waypoints: LngLat[][], minRadius?: number) => {
    const list = corridors(waypoints, minRadius)
    const signature = JSON.stringify(list)
    if (list.length === 0 || fetched.current.has(signature)) {
      return false
    }
    const ways = await fetchNetwork(kind, list)
    fetched.current.add(signature)
    network.current ??= new Network(kind)
    network.current.add(ways)
    return true
  }

  const build = async (pair: Pair, via: LngLat[], mode: ShapeSegment['mode']) => {
    if (mode === 'manual') {
      return manualSegment(pair.from, pair.to, via)
    }
    const waypoints = [pair.from, ...via, pair.to]
    const attempt = () =>
      network.current ? routeSegment(network.current, pair.from, pair.to, via) : null
    let result = attempt()
    // Međutočka izvan dohvaćenog koridora, pa onda širi koridor: spojna ulica
    // može zaobići ravnu crtu između stanica dalje nego što koridor seže.
    for (const minRadius of [undefined, widerCorridor]) {
      if (result && !result.failed) {
        break
      }
      if (await load([waypoints], minRadius)) {
        result = attempt()
      }
    }
    return result ?? { ...manualSegment(pair.from, pair.to, via), mode: 'network', failed: true }
  }

  /** Računa dionice redom; vraća nove dionice po ključu. */
  const run = async (targets: Target[]) => {
    if (!pairs || targets.length === 0) {
      return new Map<string, ShapeSegment>()
    }
    setComputing(true)
    setError(null)
    const results = new Map<string, ShapeSegment>()
    try {
      // Prvi izračun: mreža za cijelu varijantu jednim upitom.
      if (!network.current && targets.some((t) => t.mode === 'network')) {
        await load(pairs.map((p, i) => [p.from, ...(segments[i]?.via ?? []), p.to]))
      }
      for (const target of targets) {
        const pair = pairs[target.index]
        results.set(pair.key, await build(pair, target.via, target.mode))
      }
      setStore((previous) => new Map([...previous, ...results]))
      setChanged(true)
      setActive(true)
    } catch (e) {
      setError(e instanceof Error ? e.message : String(e))
    } finally {
      setComputing(false)
    }
    return results
  }

  const target = (index: number, change: Partial<Target> = {}): Target => ({
    index,
    via: segments[index]?.via ?? [],
    mode: segments[index]?.mode ?? 'network',
    ...change,
  })

  const all = () => segments.map((_, i) => i)

  return {
    /** Dionice za trenutne stanice; null je dionica koja još nije izračunata. */
    segments,
    /** Za kartu: neizračunate dionice su ravne crte. */
    drawn,
    /** Varijanta ima trasu (spremljenu ili novu). */
    active,
    /** Trasa se razlikuje od spremljene. */
    changed,
    computing,
    error,
    /** Sve stanice imaju koordinate, pa se trasa može računati. */
    available: pairs !== null && pairs.length > 0,
    pending,

    /** Neizračunate dionice; uz <paramref name="again"/> i sve po mreži iznova. */
    compute: (again = false) =>
      run(
        all()
          .filter((i) => segments[i] === null || (again && segments[i]!.mode === 'network'))
          .map((i) => target(i)),
      ),
    recompute: (index: number) => run([target(index)]),
    setMode: (index: number, mode: ShapeSegment['mode']) => run([target(index, { mode })]),
    clearVia: (index: number) => run([target(index, { via: [] })]),
    dragLine: (index: number, grabbed: LngLat, dropped: LngLat) => {
      const segment = segments[index]
      const via = segment ? insertVia(segment, grabbed, dropped) : [dropped]
      return run([target(index, { via })])
    },
    moveVia: (index: number, viaIndex: number, to: LngLat) =>
      run([
        target(index, {
          via: (segments[index]?.via ?? []).map((v, i) => (i === viaIndex ? to : v)),
        }),
      ]),
    removeVia: (index: number, viaIndex: number) =>
      run([target(index, { via: (segments[index]?.via ?? []).filter((_, i) => i !== viaIndex) })]),
    /** Bez trase: spremanje je uklanja s varijante. */
    remove: () => {
      setStore(new Map())
      setActive(false)
      setChanged(true)
      setError(null)
    },

    /**
     * Točke za spremanje: dionice koje još nisu izračunate (npr. nakon dodane
     * stanice) računaju se sada. Null kad varijanta nema trasu.
     */
    finish: async (): Promise<{ points: LngLat[]; changed: boolean } | null> => {
      if (!active || !pairs) {
        return null
      }
      const missing = all().filter((i) => segments[i] === null)
      const computed = await run(missing.map((i) => target(i)))
      const complete = pairs.map((pair, i) => computed.get(pair.key) ?? segments[i])
      if (complete.some((s) => !s)) {
        return null
      }
      return {
        points: joinSegments(complete as ShapeSegment[]),
        changed: changed || missing.length > 0,
      }
    },
  }
}

export type VariantShape = ReturnType<typeof useVariantShape>
