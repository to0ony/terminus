import { describe, expect, it } from 'vitest'
import { type LngLat, Network, type OsmWay } from './osmNetwork'
import {
  corridorTiles,
  stopDistances,
  insertVia,
  joinSegments,
  manualSegment,
  nearestSegment,
  routeSegment,
  splitShape,
  straightSegment,
} from './variantShape'

// Trasa ravno prema istoku, točka svakih ~80 m.
const shape: LngLat[] = Array.from({ length: 11 }, (_, i) => [16 + i * 0.001, 45.8])

describe('splitShape', () => {
  it('splits the shape at the points nearest to the stops', () => {
    const segments = splitShape(shape, [
      [16, 45.8001],
      [16.004, 45.8001],
      [16.01, 45.8001],
    ])

    expect(segments).toHaveLength(2)
    expect(segments[0].points).toEqual(shape.slice(0, 5))
    expect(segments[1].points).toEqual(shape.slice(4))
  })

  it('joins back into the same shape', () => {
    const segments = splitShape(shape, [
      [16, 45.8],
      [16.004, 45.8],
      [16.01, 45.8],
    ])

    expect(joinSegments(segments)).toEqual(shape)
  })

  it('finds a stop passed twice on a loop at the right pass', () => {
    // Tamo i natrag po istoj ulici: stanica 2 je na povratku.
    const loop: LngLat[] = [...shape, ...shape.slice(0, -1).reverse()]
    const segments = splitShape(loop, [
      [16, 45.8],
      [16.01, 45.8],
      [16.003, 45.8],
      [16, 45.8],
    ])

    expect(segments[1].points[0]).toEqual([16.01, 45.8])
    expect(segments[1].points.at(-1)).toEqual([16.003, 45.8])
  })
})

describe('routeSegment', () => {
  const ways: OsmWay[] = [
    {
      type: 'way',
      id: 1,
      nodes: [1, 2, 3],
      geometry: [
        { lon: 16, lat: 45.8 },
        { lon: 16.001, lat: 45.8 },
        { lon: 16.002, lat: 45.8 },
      ],
      tags: { highway: 'residential' },
    },
  ]

  it('follows the street between the stops', () => {
    const roads = new Network('road')
    roads.add(ways)

    const segment = routeSegment(roads, [16, 45.8], [16.002, 45.8], [])

    expect(segment.failed).toBe(false)
    expect(segment.points).toHaveLength(3)
  })

  it('falls back to a straight line when a stop is far from every street', () => {
    const roads = new Network('road')
    roads.add(ways)

    const segment = routeSegment(roads, [16, 45.8], [16.002, 45.82], [])

    expect(segment.failed).toBe(true)
    expect(segment.points).toEqual([
      [16, 45.8],
      [16.002, 45.82],
    ])
  })
})

describe('insertVia', () => {
  it('puts the new point in order along the segment', () => {
    const segment = manualSegment([16, 45.8], [16.01, 45.8], [[16.007, 45.8]])
    const withPoints = { ...segment, points: shape }

    expect(insertVia(withPoints, [16.003, 45.8], [16.003, 45.801])).toEqual([
      [16.003, 45.801],
      [16.007, 45.8],
    ])
    expect(insertVia(withPoints, [16.009, 45.8], [16.009, 45.801])).toEqual([
      [16.007, 45.8],
      [16.009, 45.801],
    ])
  })
})

describe('nearestSegment', () => {
  it('finds the segment under the pointer', () => {
    const segments = [
      straightSegment([16, 45.8], [16.004, 45.8]),
      straightSegment([16.004, 45.8], [16.01, 45.8]),
    ]

    expect(nearestSegment(segments, [16.009, 45.8001])).toBe(1)
  })
})

describe('corridorTiles', () => {
  it('covers the stops with a margin and widens it around long sections', () => {
    const a: LngLat = [16, 45.8]
    const b: LngLat = [16.004, 45.8]
    const far: LngLat = [16.2, 45.8]

    const short = corridorTiles([[a, b]])
    const long = corridorTiles([[a, far]])

    // Zoom 14 pločica je ~1,7 km, pa kratka dionica stane u nekoliko pločica.
    expect(short.length).toBeGreaterThan(0)
    expect(short.length).toBeLessThanOrEqual(4)
    // ~15 km s koridorom od 3 km: dulje i šire.
    expect(long.length).toBeGreaterThan(short.length * 5)
  })
})

describe('stopDistances', () => {
  it('adds up the shape and estimates sections without one', () => {
    const a: LngLat = [16, 45.8]
    const b: LngLat = [16.004, 45.8]
    const c: LngLat = [16.008, 45.8]
    // Prva dionica ide okolo: dvostruko dulje od ravne crte.
    const detour = { points: [a, [16.002, 45.8018] as LngLat, b] }

    const distances = stopDistances([a, b, c], [detour, null])

    expect(distances[0]).toEqual({ km: 0, estimated: false })
    expect(distances[1].estimated).toBe(false)
    expect(distances[1].km).toBeGreaterThan(0.31 * 1.2)
    expect(distances[2].estimated).toBe(true)
    expect(distances[2].km - distances[1].km).toBeCloseTo(0.311, 2)
  })
})
