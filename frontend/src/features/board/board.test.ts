import { describe, expect, it } from 'vitest'
import {
  boardItems,
  clockText,
  dayLabel,
  minutesUntil,
  offsetMinutes,
  parseLimit,
  parsePast,
  upcoming,
  whenText,
} from './board'
import type { BoardDeparture } from './queries'

const now = Date.parse('2026-10-17T20:00:00+02:00')

function departure(
  departureTime: string,
  serviceDate = departureTime.slice(0, 10),
  routeName = '6',
): BoardDeparture {
  return {
    routeName,
    routeColor: null,
    routeTextColor: null,
    headsign: 'Sopot',
    platform: null,
    departureTime,
    minutesUntil: 0,
    serviceDate,
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

describe('parsePast', () => {
  it('defaults to two and caps at five', () => {
    expect(parsePast(null)).toBe(2)
    expect(parsePast('0')).toBe(0)
    expect(parsePast('9')).toBe(5)
    expect(parsePast('x')).toBe(2)
  })
})

describe('dayLabel', () => {
  it('names today, tomorrow and later days', () => {
    expect(dayLabel('2026-10-17', '2026-10-17')).toBe('danas')
    expect(dayLabel('2026-10-18', '2026-10-17')).toBe('sutra')
    expect(dayLabel('2026-10-19', '2026-10-17')).toBe('pon 19. 10.')
  })
})

describe('boardItems', () => {
  const kinds = (items: ReturnType<typeof boardItems>) =>
    items.map((item) =>
      item.kind === 'end'
        ? `end:${item.next}${item.none ? ':none' : ''}`
        : `${item.departure.departureTime.slice(11, 16)}${item.past ? ':past' : ''}${item.last ? ':last' : ''}`,
    )

  it('shows the last departed ones first, dimmed', () => {
    const recent = [
      departure('2026-10-17T19:40:00+02:00'),
      departure('2026-10-17T19:50:00+02:00'),
      departure('2026-10-17T19:55:00+02:00'),
    ]
    // 20:00:00 je otišao nakon dohvata (više od 30 s prije), pa je i on otišao.
    const now20 = Date.parse('2026-10-17T20:01:00+02:00')
    const items = boardItems(
      [departure('2026-10-17T20:00:00+02:00'), departure('2026-10-17T20:10:00+02:00')],
      recent,
      now20,
      2,
      '2026-10-17',
      false,
    )
    expect(kinds(items)).toEqual(['19:55:past', '20:00:past', '20:10'])
  })

  it('marks the end of the service day and the last departure of each line', () => {
    const items = boardItems(
      [
        departure('2026-10-17T22:00:00+02:00', '2026-10-17', '6'),
        departure('2026-10-17T22:30:00+02:00', '2026-10-17', '4'),
        departure('2026-10-17T23:00:00+02:00', '2026-10-17', '6'),
        departure('2026-10-18T05:00:00+02:00', '2026-10-18', '6'),
      ],
      [],
      Date.parse('2026-10-17T21:50:00+02:00'),
      0,
      '2026-10-17',
      false,
    )
    expect(kinds(items)).toEqual(['22:00', '22:30:last', '23:00:last', 'end:2026-10-18', '05:00'])
  })

  it('does not guess the last departure while the list ends within the day', () => {
    const items = boardItems(
      [departure('2026-10-17T22:00:00+02:00'), departure('2026-10-17T22:30:00+02:00')],
      [],
      Date.parse('2026-10-17T21:50:00+02:00'),
      0,
      '2026-10-17',
      false,
    )
    expect(kinds(items)).toEqual(['22:00', '22:30'])
  })

  it('keeps trips after midnight in their service day', () => {
    const items = boardItems(
      [
        departure('2026-10-18T00:20:00+02:00', '2026-10-17'),
        departure('2026-10-18T05:00:00+02:00', '2026-10-18'),
      ],
      [departure('2026-10-17T23:50:00+02:00', '2026-10-17')],
      Date.parse('2026-10-18T00:10:00+02:00'),
      2,
      '2026-10-18',
      false,
    )
    expect(kinds(items)).toEqual(['23:50:past', '00:20:last', 'end:2026-10-18', '05:00'])
  })

  it('says there is nothing more today when the list starts tomorrow', () => {
    const items = boardItems(
      [departure('2026-10-18T05:00:00+02:00', '2026-10-18')],
      [],
      Date.parse('2026-10-17T23:30:00+02:00'),
      2,
      '2026-10-17',
      true,
    )
    expect(kinds(items)).toEqual(['end:2026-10-18:none', '05:00'])
  })
})
