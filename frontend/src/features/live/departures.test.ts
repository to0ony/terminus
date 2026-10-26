import { describe, expect, it } from 'vitest'
import { departuresFrom, sameDirection } from './departures'

const departure = (tripId: string, headsign: string, departureTime: string) => ({
  tripId,
  routeId: 'R1',
  routeName: '2',
  routeColor: 'D32F2F',
  routeTextColor: 'FFFFFF',
  headsign,
  stopId: 'S1',
  platform: null,
  departureTime,
})

describe('departures', () => {
  it('orders the rest of the day, past midnight above 24 h', () => {
    const list = departuresFrom(
      [
        departure('T3', 'Retfala', '2026-12-24T00:10:00+01:00'),
        departure('T1', 'Retfala', '2026-12-23T06:00:00+01:00'),
        departure('T2', 'Kolodvor', '2026-12-23T08:30:00+01:00'),
      ],
      '2026-12-23',
      'Europe/Zagreb',
      7 * 3600,
    )

    expect(list.map((d) => [d.tripId, d.seconds])).toEqual([
      ['T2', 8.5 * 3600],
      ['T3', 24 * 3600 + 600],
    ])
  })

  it('groups by route and headsign', () => {
    const a = departure('T1', 'Retfala', '2026-12-23T06:00:00+01:00')
    expect(sameDirection(a, departure('T2', 'Retfala', '2026-12-23T07:00:00+01:00'))).toBe(true)
    expect(sameDirection(a, departure('T3', 'Kolodvor', '2026-12-23T07:00:00+01:00'))).toBe(false)
  })
})
