import { describe, expect, it } from 'vitest'
import { groupByHour } from './departures'
import type { StopDeparture } from './queries'

function departure(departureTime: string) {
  return { departureTime } as StopDeparture
}

describe('groupByHour', () => {
  it('groups by the hour in the agency time zone, keeping order past midnight', () => {
    const groups = groupByHour([
      departure('2026-10-17T05:10:00+02:00'),
      departure('2026-10-17T05:40:00+02:00'),
      departure('2026-10-17T23:50:00+02:00'),
      departure('2026-10-18T00:15:00+02:00'),
    ])

    expect(groups.map(([hour, items]) => [hour, items.length])).toEqual([
      ['05', 2],
      ['23', 1],
      ['00', 1],
    ])
  })
})
