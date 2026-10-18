import { describe, expect, it } from 'vitest'
import { type LngLat, Network, networkKind, networkQuery, type OverpassWay } from './osmNetwork'

// Ulica istok–zapad (1–2–3, ~80 m između čvorova) i jednosmjerna ulica sjeverno od 2.
const positions: Record<number, LngLat> = {
  1: [16.0, 45.8],
  2: [16.001, 45.8],
  3: [16.002, 45.8],
  4: [16.001, 45.801],
}

function way(id: number, nodes: number[], tags: Record<string, string>): OverpassWay {
  return {
    type: 'way',
    id,
    nodes,
    geometry: nodes.map((n) => ({ lon: positions[n][0], lat: positions[n][1] })),
    tags,
  }
}

function network(...ways: OverpassWay[]) {
  const result = new Network('road')
  result.add(ways)
  return result
}

describe('Network', () => {
  it('finds the path along the streets', () => {
    const roads = network(way(10, [1, 2, 3], { highway: 'residential' }))

    expect(roads.route(1, 3)).toEqual([positions[1], positions[2], positions[3]])
  })

  it('keeps to the direction of one-way streets', () => {
    const roads = network(
      way(10, [1, 2, 3], { highway: 'residential' }),
      way(11, [2, 4], { highway: 'residential', oneway: 'yes' }),
    )

    expect(roads.route(1, 4)).toEqual([positions[1], positions[2], positions[4]])
    expect(roads.route(4, 1)).toBeNull()
  })

  it('lets buses through one-way streets marked for them', () => {
    const roads = network(
      way(10, [1, 2, 3], { highway: 'residential' }),
      way(11, [2, 4], { highway: 'residential', oneway: 'yes', 'oneway:bus': 'no' }),
    )

    expect(roads.route(4, 1)).not.toBeNull()
  })

  it('treats roundabouts as one-way', () => {
    const roads = network(way(10, [1, 2], { highway: 'primary', junction: 'roundabout' }))

    expect(roads.route(2, 1)).toBeNull()
  })

  it('leaves out private roads unless buses may use them', () => {
    expect(network(way(10, [1, 2], { highway: 'service', access: 'private' })).isEmpty).toBe(true)
    expect(
      network(way(10, [1, 2], { highway: 'service', access: 'private', bus: 'yes' })).isEmpty,
    ).toBe(false)
  })

  it('snaps a stop next to the street and routes from there', () => {
    const roads = network(way(10, [1, 2, 3], { highway: 'residential' }))
    // ~20 m sjeverno od sredine dionice 1–2.
    const stop = roads.snap([16.0005, 45.80018])!

    const path = roads.route(stop, 3)!
    expect(path[0][0]).toBeCloseTo(16.0005, 6)
    expect(path[0][1]).toBeCloseTo(45.8, 6)
    expect(path.slice(1)).toEqual([positions[2], positions[3]])
  })

  it('prefers the street to a slightly closer service road', () => {
    const roads = network(
      way(10, [1, 2, 3], { highway: 'residential' }),
      // Stanica ~44 m sjeverno od ulice, a parkiralište ~30 m sjeverno od stanice.
      {
        type: 'way',
        id: 11,
        nodes: [5, 6],
        geometry: [
          { lon: 16.0, lat: 45.80067 },
          { lon: 16.002, lat: 45.80067 },
        ],
        tags: { highway: 'service' },
      },
    )
    const stop = roads.snap([16.0005, 45.8004])!

    expect(roads.positions.get(stop)![1]).toBeCloseTo(45.8, 6)
  })

  it('does not snap a point far from every street', () => {
    const roads = network(way(10, [1, 2, 3], { highway: 'residential' }))

    expect(roads.snap([16.0005, 45.81])).toBeNull()
  })

  it('goes straight between two points on the same piece of street', () => {
    const roads = network(way(10, [1, 2], { highway: 'residential' }))
    const a = roads.snap([16.0002, 45.8])!
    const b = roads.snap([16.0008, 45.8])!

    expect(roads.route(a, b)).toHaveLength(2)
  })
})

describe('networkKind', () => {
  it('maps route types to roads, tram tracks and railways', () => {
    expect(networkKind(3)).toBe('road')
    expect(networkKind(700)).toBe('road')
    expect(networkKind(11)).toBe('road')
    expect(networkKind(0)).toBe('tram')
    expect(networkKind(900)).toBe('tram')
    expect(networkKind(2)).toBe('rail')
    expect(networkKind(109)).toBe('rail')
  })
})

describe('networkQuery', () => {
  it('asks for the network around each corridor', () => {
    const query = networkQuery('tram', [
      {
        points: [
          [16, 45.8],
          [16.01, 45.81],
        ],
        radius: 300,
      },
    ])

    expect(query).toContain(
      'way["railway"~"^(tram|light_rail)$"](around:300,45.800000,16.000000,45.810000,16.010000);',
    )
    expect(query).toContain('out body geom')
  })
})
