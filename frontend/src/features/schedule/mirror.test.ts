import { describe, expect, it } from 'vitest'
import type { EditorTrip, ScheduleVariant } from './model'
import { mirrorRows, mirrorTrips } from './mirror'

const hour = 3600
const minute = 60

/** Vožnja Terminal → Boće: polazak u start, 30 min vožnje. */
function outbound(
  key: string,
  start: number,
  dayTypes = ['rd'],
  departureId: string | null = null,
): EditorTrip {
  return {
    key,
    id: key,
    code: key,
    updatedAt: '2026-10-19T10:00:00Z',
    serviceCalendarId: dayTypes[0],
    headsign: 'Boće',
    shortName: null,
    blockId: `bus-${key}`,
    shapeId: null,
    variantId: 'there',
    departureId,
    dayTypes,
    wheelchairAccessible: 1,
    bikesAllowed: 0,
    stopTimes: [stopTime('terminal', start), stopTime('boce', start + 30 * minute)],
  }
}

function stopTime(stopId: string, time: number) {
  return {
    stopId,
    arrivalTime: time,
    departureTime: time,
    stopHeadsign: null,
    pickupType: 0 as const,
    dropOffType: 0 as const,
    continuousPickup: null,
    continuousDropOff: null,
    shapeDistTraveled: null,
    timepoint: true,
  }
}

const back: ScheduleVariant = {
  id: 'back',
  name: 'Boće → Terminal',
  directionId: 1,
  headsign: 'Terminal',
  shapeId: 'shape-back',
  sortOrder: 0,
  tripCount: 0,
  stops: [
    { stopId: 'boce', travelTime: 0, dwellTime: 0 },
    { stopId: 'centar', travelTime: 20 * minute, dwellTime: 0 },
    { stopId: 'terminal', travelTime: 15 * minute, dwellTime: 0 },
  ],
}

const turn = (offset: number, overrides: Record<string, number> = {}) => ({
  mode: 'turn' as const,
  offset,
  overrides,
})

describe('mirrorRows', () => {
  it('turns each departure at the terminus with the shared layover', () => {
    const rows = mirrorRows(
      [outbound('b', 7 * hour), outbound('a', 6 * hour)],
      turn(8 * minute),
      [],
      35 * minute,
    )
    expect(rows.map((r) => r.departure)).toEqual([6 * hour + 38 * minute, 7 * hour + 38 * minute])
    expect(rows.every((r) => !r.locked)).toBe(true)
  })

  it('keeps a locked layover when the shared one changes', () => {
    const rows = mirrorRows(
      [outbound('a', 6 * hour), outbound('b', 7 * hour)],
      turn(10 * minute, { b: 15 * minute }),
      [],
      35 * minute,
    )
    expect(rows.map((r) => [r.offset / minute, r.locked])).toEqual([
      [10, false],
      [15, true],
    ])
  })

  it('offsets from the outbound departure in offset mode', () => {
    const [row] = mirrorRows(
      [outbound('a', 6 * hour)],
      { mode: 'offset', offset: 45 * minute, overrides: {} },
      [],
      35 * minute,
    )
    expect(row.departure).toBe(6 * hour + 45 * minute)
  })

  it('makes one return per departure across day types', () => {
    const rows = mirrorRows(
      [
        outbound('a-rd', 6 * hour, ['rd', 'rs'], 'dep'),
        outbound('a-rs', 6 * hour, ['rs', 'rd'], 'dep'),
        outbound('b', 9 * hour, ['vk']),
      ],
      turn(5 * minute),
      [],
      35 * minute,
    )
    expect(rows.map((r) => [r.key, r.dayTypes])).toEqual([
      ['dep', ['rd', 'rs']],
      ['b', ['vk']],
    ])
  })

  it('marks returns that already exist and ones past the end of the day', () => {
    const existing = { ...outbound('x', 6 * hour + 36 * minute), variantId: 'back' }
    const rows = mirrorRows(
      [outbound('a', 6 * hour), outbound('late', 47 * hour)],
      turn(5 * minute),
      [existing],
      35 * minute,
    )
    expect(rows.map((r) => [r.existing, r.tooLate])).toEqual([
      [true, false],
      [false, true],
    ])
  })
})

describe('mirrorTrips', () => {
  it('builds return trips by the variant with the departure day types and vehicle', () => {
    const rows = mirrorRows(
      [outbound('a', 6 * hour, ['rd', 'rs'], 'dep'), outbound('late', 47 * hour)],
      turn(10 * minute),
      [],
      35 * minute,
    )
    const [trip, ...rest] = mirrorTrips(rows, back)
    expect(rest).toEqual([])
    expect(trip.stopTimes.map((st) => st.departureTime)).toEqual([
      6 * hour + 40 * minute,
      7 * hour,
      7 * hour + 15 * minute,
    ])
    expect(trip).toMatchObject({
      id: null,
      variantId: 'back',
      shapeId: 'shape-back',
      headsign: 'Terminal',
      serviceCalendarId: 'rd',
      dayTypes: ['rd', 'rs'],
      blockId: 'bus-a',
      wheelchairAccessible: 1,
    })
  })
})
