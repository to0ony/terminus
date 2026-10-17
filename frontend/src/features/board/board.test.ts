import { describe, expect, it } from 'vitest'
import { clockText, minutesUntil, offsetMinutes, parseLimit, upcoming, whenText } from './board'
import type { BoardDeparture } from './queries'

const now = Date.parse('2026-10-17T20:00:00+02:00')

function departure(departureTime: string): BoardDeparture {
  return {
    routeName: '6',
    routeColor: null,
    routeTextColor: null,
    headsign: 'Sopot',
    platform: null,
    departureTime,
    minutesUntil: 0,
  }
}

describe('offsetMinutes', () => {
  it('reads the zone offset from an ISO time', () => {
    expect(offsetMinutes('2026-10-17T20:52:10.4729346+02:00')).toBe(120)
    expect(offsetMinutes('2026-01-01T10:00:00-03:30')).toBe(-210)
    expect(offsetMinutes('2026-01-01T10:00:00Z')).toBe(0)
  })
})

describe('clockText', () => {
  it('shows the time in the given zone, not the browser zone', () => {
    expect(clockText(now, 120)).toBe('20:00')
    expect(clockText(now, 60)).toBe('19:00')
  })
})

describe('whenText', () => {
  it('shows minutes up to ten, then the clock time', () => {
    expect(whenText('2026-10-17T20:00:40+02:00', now)).toBe('stiže')
    expect(whenText('2026-10-17T20:04:59+02:00', now)).toBe('za 4 min')
    expect(whenText('2026-10-17T20:10:00+02:00', now)).toBe('za 10 min')
    expect(whenText('2026-10-17T20:11:00+02:00', now)).toBe('20:11')
  })

  it('reads the clock time from the text after midnight', () => {
    expect(whenText('2026-10-18T00:15:00+02:00', now)).toBe('00:15')
  })
})

describe('minutesUntil', () => {
  it('never goes below zero', () => {
    expect(minutesUntil('2026-10-17T19:59:40+02:00', now)).toBe(0)
  })
})

describe('upcoming', () => {
  it('drops departures that left more than half a minute ago', () => {
    const list = [
      departure('2026-10-17T19:59:00+02:00'),
      departure('2026-10-17T19:59:45+02:00'),
      departure('2026-10-17T20:03:00+02:00'),
    ]
    expect(upcoming(list, now).map((d) => d.departureTime)).toEqual([
      '2026-10-17T19:59:45+02:00',
      '2026-10-17T20:03:00+02:00',
    ])
  })
})

describe('parseLimit', () => {
  it('defaults to 12 and caps at 30', () => {
    expect(parseLimit(null)).toBe(12)
    expect(parseLimit('abc')).toBe(12)
    expect(parseLimit('0')).toBe(12)
    expect(parseLimit('8')).toBe(8)
    expect(parseLimit('100')).toBe(30)
  })
})
