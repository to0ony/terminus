import { describe, expect, it } from 'vitest'
import { parseOverpass, withoutExisting } from './osm'

const node = (id: number, lat: number, lon: number, tags?: Record<string, string>) => ({
  type: 'node',
  id,
  lat,
  lon,
  tags,
})

describe('parseOverpass', () => {
  it('keeps named stops with platform, ref and wheelchair access', () => {
    const { stops, unnamed } = parseOverpass({
      elements: [
        node(1, 46.3, 16.3, {
          name: 'Kapucinski trg',
          local_ref: 'A',
          ref: '12',
          wheelchair: 'yes',
        }),
        node(2, 46.31, 16.31),
      ],
    })
    expect(unnamed).toBe(1)
    expect(stops).toEqual([
      {
        id: 1,
        name: 'Kapucinski trg',
        platformCode: 'A',
        publicCode: '12',
        latitude: 46.3,
        longitude: 16.3,
        wheelchair: 1,
      },
    ])
  })

  it('merges the same name within 15 m but keeps both sides of the street', () => {
    const { stops } = parseOverpass({
      elements: [
        node(1, 46.3, 16.3, { name: 'Varteks' }),
        // ~5 m dalje: ista točka (stajalište i točka zaustavljanja).
        node(2, 46.30004, 16.3, { name: 'Varteks' }),
        // ~45 m dalje: druga strana ceste.
        node(3, 46.3004, 16.3, { name: 'Varteks' }),
      ],
    })
    expect(stops.map((s) => s.id)).toEqual([1, 3])
  })
})

describe('withoutExisting', () => {
  it('drops stops within 20 m of an existing one', () => {
    const { stops } = parseOverpass({
      elements: [node(1, 46.3, 16.3, { name: 'A' }), node(2, 46.31, 16.3, { name: 'B' })],
    })
    const left = withoutExisting(stops, [
      { latitude: 46.30005, longitude: 16.3 },
      { latitude: null, longitude: null },
    ])
    expect(left.map((s) => s.id)).toEqual([2])
  })
})
