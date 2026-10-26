import { describe, expect, it } from 'vitest'
import { formatTime, nowInZone } from './clock'

describe('clock', () => {
  it('reads the date and time in the agency time zone', () => {
    // 23:30 UTC je već sljedeći dan u Zagrebu (zimsko vrijeme, UTC+1).
    const now = new Date('2026-12-23T23:30:15.500Z')
    expect(nowInZone('Europe/Zagreb', now)).toEqual({ date: '2026-12-24', seconds: 30 * 60 + 15.5 })
  })

  it('formats seconds from midnight', () => {
    expect(formatTime(6 * 3600 + 5 * 60 + 9.9)).toBe('06:05:09')
    expect(formatTime(25 * 3600, false)).toBe('25:00')
  })
})
