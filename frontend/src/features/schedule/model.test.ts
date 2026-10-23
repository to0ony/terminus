import { describe, expect, it } from 'vitest'
import {
  changes,
  detachTrip,
  drawnTrip,
  type EditorTrip,
  formatTime,
  headways,
  moveStop,
  moveToDayType,
  parseTime,
  resizeView,
  retimeTrip,
  rowIndexes,
  rowPositions,
  shiftStop,
  shiftTrip,
  stretchTrip,
  taktDepartures,
  templateTrip,
  tripFrom,
  tripFromVariant,
  validateTrip,
  withDayTypes,
} from './model'

describe('resizeView', () => {
  const span: [number, number] = [5 * 3600, 23 * 3600]

  it('moves one edge and keeps the other', () => {
    expect(resizeView([7 * 3600, 9 * 3600], 'start', 6 * 3600, span)).toEqual([6 * 3600, 9 * 3600])
    expect(resizeView([7 * 3600, 9 * 3600], 'end', 12 * 3600, span)).toEqual([7 * 3600, 12 * 3600])
  })

  it('keeps at least 20 minutes and stays inside the day', () => {
    expect(resizeView([7 * 3600, 9 * 3600], 'start', 10 * 3600, span)).toEqual([
      9 * 3600 - 1200,
      9 * 3600,
    ])
    expect(resizeView([7 * 3600, 9 * 3600], 'end', 0, span)).toEqual([7 * 3600, 7 * 3600 + 1200])
    expect(resizeView([7 * 3600, 9 * 3600], 'start', 0, span)).toEqual([5 * 3600, 9 * 3600])
    expect(resizeView([7 * 3600, 9 * 3600], 'end', 30 * 3600, span)).toEqual([7 * 3600, 23 * 3600])
  })
})

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
    departureId: null,
    dayTypes: ['rd'],
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

describe('departures', () => {
  const order = ['rd', 'rasp', 'vik']

  it('keeps the own day type and the order of day types', () => {
    const daily = withDayTypes(trip('t1', ['A', 'B'], 0), ['vik', 'rasp'], order)
    expect(daily.dayTypes).toEqual(['rd', 'rasp', 'vik'])

    // Vlastiti tip dana se ne može isključiti.
    expect(withDayTypes(daily, ['vik'], order).dayTypes).toEqual(['rd', 'vik'])
  })

  it('moves the departure with the trip to another day type', () => {
    const daily = withDayTypes(trip('t1', ['A', 'B'], 0), order, order)

    const moved = moveToDayType(withDayTypes(daily, ['rd', 'vik'], order), 'rasp')

    expect(moved.serviceCalendarId).toBe('rasp')
    expect(moved.dayTypes).toEqual(['rasp', 'vik'])
  })

  it('sends the day types, or detaches the trip', () => {
    const daily = { ...withDayTypes(trip('t1', ['A', 'B'], 0), order, order), departureId: 'd1' }

    const sent = changes([trip('t1', ['A', 'B'], 0)], [daily]).upserts[0]
    expect(sent.dayTypes).toEqual(order)
    expect(sent.detach).toBe(false)

    const alone = changes([daily], [detachTrip(daily)]).upserts[0]
    expect(alone.detach).toBe(true)
    expect(alone.dayTypes).toBeNull()
  })

  it('makes copies that are not part of the template departure', () => {
    const daily = { ...withDayTypes(trip('t1', ['A', 'B'], 0), order, order), departureId: 'd1' }

    const copy = tripFrom(daily, 3600, 'rd')

    expect(copy.departureId).toBeNull()
    expect(copy.dayTypes).toEqual(['rd'])
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

describe('trips from a variant', () => {
  const variant = {
    id: 'v',
    name: 'Glavna',
    directionId: 0,
    headsign: 'Sopot',
    shapeId: null,
    sortOrder: 0,
    tripCount: 0,
    stops: [
      { stopId: 'a', travelTime: 0, dwellTime: 0 },
      { stopId: 'b', travelTime: 120, dwellTime: 30 },
      { stopId: 'c', travelTime: 270, dwellTime: 0 },
      { stopId: 'd', travelTime: 180, dwellTime: 0 },
    ],
  }

  it('follow its running and dwell times', () => {
    const trip = tripFromVariant(variant, 3600, 'rd')
    expect(trip.stopTimes.map((st) => [st.arrivalTime, st.departureTime])).toEqual([
      [3600, 3600],
      [3720, 3750],
      [4020, 4020],
      [4200, 4200],
    ])
    expect(trip.variantId).toBe('v')
    expect(trip.headsign).toBe('Sopot')
  })

  it('stretch between drawn points and follow the variant after the last', () => {
    // Varijanta treba 7 min do c (420 s), nacrtano je 14 min: sve se rasteže dvostruko.
    const trip = drawnTrip(
      variant,
      [
        { index: 0, time: 3600 },
        { index: 2, time: 3600 + 840 },
      ],
      'rd',
    )
    expect(trip.stopTimes.map((st) => st.departureTime)).toEqual([3600, 3900, 4440, 4620])
  })

  it('with two clicks spread over the whole variant', () => {
    const trip = drawnTrip(
      variant,
      [
        { index: 3, time: 3600 + 1200 },
        { index: 0, time: 3600 },
      ],
      'rd',
    )
    expect(trip.stopTimes.at(-1)!.departureTime).toBe(4800)
    expect(trip.stopTimes[1].departureTime).toBe(3600 + Math.round((150 / 600) * 1200))
  })
})

describe('retimeTrip', () => {
  const variant = {
    id: 'v',
    name: 'Glavna',
    directionId: 0 as const,
    headsign: null,
    shapeId: null,
    sortOrder: 0,
    tripCount: 1,
    stops: [
      { stopId: 'a', travelTime: 0, dwellTime: 0 },
      { stopId: 'b', travelTime: 300, dwellTime: 60 },
      { stopId: 'c', travelTime: 240, dwellTime: 0 },
    ],
  }

  it('keeps the first departure and takes every other time from the variant', () => {
    const retimed = retimeTrip(trip('t1', ['a', 'b', 'c'], 3600, 100), variant)!
    expect(retimed.stopTimes.map((st) => [st.arrivalTime, st.departureTime])).toEqual([
      [3600, 3600],
      [3900, 3960],
      [4200, 4200],
    ])
    expect(retimed.key).toBe('t1')
  })

  it('keeps both ends of an approximate trip and spreads the stops by the variant', () => {
    const retimed = retimeTrip(trip('t1', ['a', 'b', 'c'], 3600, 100), variant, true)!
    expect(retimed.stopTimes.map((st) => [st.arrivalTime, st.departureTime])).toEqual([
      [3600, 3600],
      [3720, 3720],
      [3800, 3800],
    ])
  })

  it('skips a trip with other stops than the variant', () => {
    expect(retimeTrip(trip('t1', ['a', 'c'], 3600), variant)).toBeNull()
  })
})

describe('approximate trips', () => {
  const times = (t: EditorTrip) => t.stopTimes.map((st) => st.departureTime)

  it('stretch the stops in between by their running times', () => {
    const stretched = stretchTrip(trip('t1', ['a', 'b', 'c', 'd'], 3600), 3600, 4320)
    expect(times(stretched)).toEqual([3600, 3840, 4080, 4320])
  })

  it('round the stops in between to whole minutes', () => {
    const stretched = stretchTrip(trip('t1', ['a', 'b', 'c', 'd'], 3600), 3600, 3900)
    expect(times(stretched)).toEqual([3600, 3720, 3780, 3900])
  })

  it('change the duration when the last stop is dragged', () => {
    const moved = moveStop(trip('t1', ['a', 'b', 'c', 'd'], 3600), 3, 360, false, true)
    expect(times(moved)).toEqual([3600, 3840, 4080, 4320])
  })

  it('move the whole trip when the first stop is dragged', () => {
    const moved = moveStop(trip('t1', ['a', 'b', 'c'], 3600), 0, 600, false, true)
    expect(times(moved)).toEqual([4200, 4320, 4440])
  })

  it('never end before they start', () => {
    const moved = moveStop(trip('t1', ['a', 'b', 'c'], 3600), 2, -3600, false, true)
    expect(times(moved)).toEqual([3600, 3600, 3600])
  })

  it('keep the stops in between in place', () => {
    const original = trip('t1', ['a', 'b', 'c'], 3600)
    expect(moveStop(original, 1, 60, false, true)).toBe(original)
  })

  it('move only one stop on an exact route', () => {
    const moved = moveStop(trip('t1', ['a', 'b', 'c'], 3600), 2, 60, false, false)
    expect(times(moved)).toEqual([3600, 3720, 3900])
  })
})
