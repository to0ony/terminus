import { describe, expect, it } from 'vitest'
import {
  changes,
  type EditorTrip,
  formatTime,
  headways,
  parseTime,
  rowIndexes,
  rowPositions,
  shiftStop,
  shiftTrip,
  taktDepartures,
  templateTrip,
  tripFrom,
  validateTrip,
} from './model'

function trip(key: string, stops: string[], start: number, step = 120): EditorTrip {
  return {
    key,
    id: key.startsWith('new') ? null : key,
    code: key,
    updatedAt: key.startsWith('new') ? null : '2026-10-17T10:00:00Z',
    serviceCalendarId: 'rd',
    headsign: 'Sopot',
    shortName: null,
    blockId: 'b1',
    shapeId: null,
    variantId: 'v1',
    wheelchairAccessible: 0,
    bikesAllowed: 0,
    stopTimes: stops.map((stopId, i) => ({
      stopId,
      arrivalTime: start + i * step,
      departureTime: start + i * step,
      stopHeadsign: null,
      pickupType: 0,
      dropOffType: 0,
      continuousPickup: null,
      continuousDropOff: null,
      shapeDistTraveled: null,
      timepoint: true,
    })),
  }
}

const name = (id: string) => id

describe('formatTime and parseTime', () => {
  it('keep hours after midnight', () => {
    expect(formatTime(24 * 3600 + 600)).toBe('24:10')
    expect(parseTime('7:05')).toBe(7 * 3600 + 300)
    expect(parseTime('24:10')).toBe(24 * 3600 + 600)
    expect(parseTime('7:65')).toBeNull()
    expect(parseTime('49:00')).toBeNull()
  })
})

describe('rowIndexes', () => {
  it('maps a short trip and a stop visited twice', () => {
    expect(rowIndexes(['a', 'b', 'c', 'b'], ['b', 'c'])).toEqual([1, 2])
    expect(rowIndexes(['a', 'b', 'c', 'b'], ['a', 'b', 'c', 'b'])).toEqual([0, 1, 2, 3])
  })
})

describe('rowPositions', () => {
  it('spaces rows by the typical running time', () => {
    const t = trip('t', ['a', 'b', 'c'], 0)
    t.stopTimes[2].arrivalTime = t.stopTimes[2].departureTime = 120 + 360
    const positions = rowPositions(['a', 'b', 'c'], [t])
    expect(positions[0]).toBe(0)
    expect(positions[1]).toBeCloseTo(0.25)
    expect(positions[2]).toBeCloseTo(1)
  })
})

describe('shifting', () => {
  it('moves a whole trip or one stop with the rest', () => {
    const t = trip('t', ['a', 'b', 'c'], 600)
    expect(shiftTrip(t, 60).stopTimes.map((st) => st.departureTime)).toEqual([660, 780, 900])
    expect(shiftStop(t, 1, 60, false).stopTimes.map((st) => st.departureTime)).toEqual([
      600, 780, 840,
    ])
    expect(shiftStop(t, 1, 60, true).stopTimes.map((st) => st.departureTime)).toEqual([
      600, 780, 900,
    ])
  })
})

describe('validateTrip', () => {
  it('rejects times that go backwards', () => {
    const t = shiftStop(trip('t', ['a', 'b', 'c'], 600), 1, -300, false)
    expect(validateTrip(t, name)).toMatch(/^b: dolazak \(00:07\) je prije polaska/)
    expect(validateTrip(trip('t', ['a', 'b'], 600), name)).toBeNull()
  })
})

describe('new trips', () => {
  it('copy the running times of the main pattern', () => {
    const trips = [
      trip('short', ['a', 'b'], 300),
      trip('t1', ['a', 'b', 'c'], 600),
      trip('t2', ['a', 'b', 'c'], 3600, 180),
    ]
    const template = templateTrip(trips, 3000)!
    expect(template.key).toBe('t2')

    const created = tripFrom(template, 7200, 'sub')
    expect(created.id).toBeNull()
    expect(created.blockId).toBeNull()
    expect(created.serviceCalendarId).toBe('sub')
    expect(created.variantId).toBe('v1')
    expect(created.stopTimes.map((st) => st.departureTime)).toEqual([7200, 7380, 7560])
  })

  it('generate a takt', () => {
    expect(taktDepartures(3600, 5400, 600)).toEqual([3600, 4200, 4800, 5400])
  })
})

describe('headways', () => {
  it('measures gaps between first departures', () => {
    const gaps = headways([trip('a', ['x'], 600), trip('b', ['x'], 1200), trip('c', ['x'], 1500)])
    expect(gaps.map((g) => g.gap)).toEqual([600, 300])
  })
})

describe('changes', () => {
  it('lists new, changed and deleted trips', () => {
    const original = [trip('t1', ['a', 'b'], 600), trip('t2', ['a', 'b'], 1200)]
    const created = { ...trip('new-1', ['a', 'b'], 1800) }
    const current = [shiftTrip(original[0], 60), created]

    const result = changes(original, current)

    expect(result.upserts.map((u) => u.id)).toEqual(['t1', null])
    expect(result.upserts[0].variantId).toBe('v1')
    expect(result.deletes).toEqual([{ id: 't2', updatedAt: '2026-10-17T10:00:00Z' }])
  })

  it('is empty when nothing changed', () => {
    const original = [trip('t1', ['a', 'b'], 600)]
    expect(changes(original, [...original])).toEqual({ upserts: [], deletes: [] })
  })
})
