import { describe, expect, it } from 'vitest'
import { buildTrack, type LngLat, makePath, pointAt, project, vehicleAt } from './simulation'

const times = (...pairs: [number, number][]) =>
  pairs.map(([arrivalTime, departureTime], i) => ({ stopId: `S${i}`, arrivalTime, departureTime }))

describe('simulation', () => {
  it('measures and walks along a path', () => {
    const path = makePath([
      [18.0, 45.0],
      [18.01, 45.0],
    ])
    // 0,01° dužine na 45° širine je oko 786 m.
    expect(path.cumulative[1]).toBeGreaterThan(780)
    expect(path.cumulative[1]).toBeLessThan(790)
    expect(pointAt(path, path.cumulative[1] / 2)[0]).toBeCloseTo(18.005, 6)
  })

  it('places stops on an out-and-back shape in order', () => {
    // Tamo i natrag po istoj ulici: B je na putu tamo i na putu natrag.
    const shape: LngLat[] = [
      [18.0, 45.0],
      [18.02, 45.0],
      [18.0, 45.0],
    ]
    const path = makePath(shape)
    const b: LngLat = [18.01, 45.0]
    const first = project(path, b)
    const second = project(path, b, first + 1)
    expect(first).toBeCloseTo(path.cumulative[1] / 2, 0)
    expect(second).toBeCloseTo(path.cumulative[1] * 1.5, 0)
  })

  it('moves a vehicle between stops and holds it at a stop', () => {
    const stops: LngLat[] = [
      [18.0, 45.0],
      [18.01, 45.0],
      [18.02, 45.0],
    ]
    const track = buildTrack(times([0, 60], [180, 240], [360, 360]), stops, null)

    expect(vehicleAt(track, -1)).toBeNull()
    expect(vehicleAt(track, 30)).toMatchObject({ stopIndex: 0, atStop: true })
    const halfway = vehicleAt(track, 120)!
    expect(halfway).toMatchObject({ stopIndex: 0, atStop: false })
    expect(halfway.position[0]).toBeCloseTo(18.005, 6)
    expect(vehicleAt(track, 200)).toMatchObject({ stopIndex: 1, atStop: true })
    expect(vehicleAt(track, 361)).toBeNull()
  })

  it('follows the shape between stops', () => {
    // Trasa ide preko sjevera, ne ravno.
    const shape: LngLat[] = [
      [18.0, 45.0],
      [18.0, 45.01],
      [18.01, 45.01],
      [18.01, 45.0],
    ]
    const track = buildTrack(
      times([0, 0], [300, 300]),
      [
        [18.0, 45.0],
        [18.01, 45.0],
      ],
      shape,
    )

    const halfway = vehicleAt(track, 150)!
    expect(halfway.position[1]).toBeCloseTo(45.01, 3)
  })
})
