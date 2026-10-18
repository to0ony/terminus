import { describe, expect, it } from 'vitest'
import { type PairCandidate, type ReversibleStop, reverseStops } from './reverse'

const stop = (
  stopId: string,
  name: string,
  latitude: number,
  travelTime: number,
  dwellTime = 0,
): ReversibleStop => ({
  stopId,
  name,
  platformCode: null,
  latitude,
  longitude: 16,
  travelTime,
  dwellTime,
})

const candidate = (id: string, name: string, latitude: number, more: Partial<PairCandidate> = {}) =>
  ({
    id,
    name,
    platformCode: null,
    latitude,
    longitude: 16.0005,
    locationType: 0,
    isActive: true,
    ...more,
  }) satisfies PairCandidate

// Oko 1 km između stanica; par je ~40 m s druge strane ceste.
const outbound = [
  stop('a1', 'Glavni kolodvor', 45.8, 0, 30),
  stop('b1', 'Trg', 45.81, 180, 20),
  stop('c1', 'Bolnica', 45.82, 240, 0),
]

describe('reverseStops', () => {
  it('reverses the order and keeps each section time', () => {
    const reversed = reverseStops(outbound, [])

    expect(reversed.map((s) => s.stopId)).toEqual(['c1', 'b1', 'a1'])
    expect(reversed.map((s) => s.travelTime)).toEqual([0, 240, 180])
    expect(reversed.map((s) => s.dwellTime)).toEqual([0, 20, 30])
  })

  it('pairs each stop with the nearest stop of the same name', () => {
    const reversed = reverseStops(outbound, [
      candidate('b2', 'trg', 45.8101),
      candidate('b3', 'Trg', 45.813),
      candidate('a2', 'Glavni Kolodvor', 45.8001),
    ])

    expect(reversed.map((s) => s.stopId)).toEqual(['c1', 'b2', 'a2'])
    expect(reversed.map((s) => s.unpaired)).toEqual([true, false, false])
  })

  it('ignores stations, inactive stops, stops of the outbound and those too far away', () => {
    const reversed = reverseStops(outbound, [
      candidate('b2', 'Trg', 45.8101, { locationType: 1 }),
      candidate('b3', 'Trg', 45.8101, { isActive: false }),
      candidate('a1', 'Glavni kolodvor', 45.8),
      candidate('c2', 'Bolnica', 45.83),
    ])

    expect(reversed.map((s) => s.stopId)).toEqual(['c1', 'b1', 'a1'])
    expect(reversed.every((s) => s.unpaired)).toBe(true)
  })
})
