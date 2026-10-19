import { describe, expect, it } from 'vitest'
import { Network } from './osmNetwork'
import { type TileLine, tilesFor, toLngLat, waysFromTiles } from './tileNetwork'

// Koordinate pločice oko Zagreba na zoomu 14 (jedinica ~0,4 m).
const origin = [35_900_000, 23_700_000] as const
const at = (dx: number, dy: number): [number, number] => [origin[0] + dx, origin[1] + dy]

const line = (props: TileLine['props'], ...coords: [number, number][]): TileLine => ({
  coords,
  props,
})

function network(lines: TileLine[], kind: 'road' | 'tram' = 'road') {
  const result = new Network(kind)
  result.add(waysFromTiles(lines, kind))
  return result
}

describe('waysFromTiles', () => {
  it('joins streets that share a point', () => {
    const roads = network([
      line({ class: 'secondary' }, at(0, 0), at(500, 0)),
      line({ class: 'minor' }, at(500, 0), at(500, 500)),
    ])
    const from = roads.snap(toLngLat(at(0, 0)))!
    const to = roads.snap(toLngLat(at(500, 500)))!

    expect(roads.route(from, to)).toHaveLength(3)
  })

  it('joins a side street that ends next to the main street without a shared point', () => {
    // Glavna ulica je pojednostavljena: točke raskrižja (250, 0) nema.
    const roads = network([
      line({ class: 'secondary' }, at(0, 0), at(500, 0)),
      line({ class: 'minor' }, at(250, 2), at(250, 500)),
    ])
    const from = roads.snap(toLngLat(at(0, 0)))!
    const to = roads.snap(toLngLat(at(250, 500)))!

    expect(roads.route(from, to)).not.toBeNull()
  })

  it('keeps one-way streets one-way', () => {
    const roads = network([line({ class: 'minor', oneway: 1 }, at(0, 0), at(500, 0))])
    const a = roads.snap(toLngLat(at(0, 0)))!
    const b = roads.snap(toLngLat(at(500, 0)))!

    expect(roads.route(a, b)).not.toBeNull()
    expect(roads.route(b, a)).toBeNull()
  })

  it('leaves out paths, tracks and rails from the road network', () => {
    const ways = waysFromTiles(
      [
        line({ class: 'path', subclass: 'footway' }, at(0, 0), at(100, 0)),
        line({ class: 'track' }, at(0, 0), at(100, 0)),
        line({ class: 'rail', subclass: 'rail' }, at(0, 0), at(100, 0)),
        line({ class: 'tertiary' }, at(0, 0), at(100, 0)),
      ],
      'road',
    )

    expect(ways.map((w) => w.tags?.highway)).toEqual(['tertiary'])
  })

  it('takes only tram tracks for trams', () => {
    const ways = waysFromTiles(
      [
        line({ class: 'transit', subclass: 'tram' }, at(0, 0), at(100, 0)),
        line({ class: 'rail', subclass: 'rail' }, at(0, 0), at(100, 0)),
        line({ class: 'primary' }, at(0, 0), at(100, 0)),
      ],
      'tram',
    )

    expect(ways.map((w) => w.tags?.railway)).toEqual(['tram'])
  })
})

describe('tilesFor', () => {
  it('lists the zoom 14 tiles of an area', () => {
    // Malo područje u Zagrebu je u jednoj ili dvije pločice.
    const tiles = tilesFor(15.97, 45.81, 15.975, 45.812)

    expect(tiles.length).toBeGreaterThanOrEqual(1)
    expect(tiles.length).toBeLessThanOrEqual(2)
    expect(tiles[0]).toMatch(/^\d+\/\d+$/)
  })
})
