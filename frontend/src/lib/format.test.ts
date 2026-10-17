import { describe, expect, it } from 'vitest'
import { formatBytes, formatGtfsTime, formatNumber } from './format'

describe('format', () => {
  it('groups thousands the Croatian way', () => {
    expect(formatNumber(1131537)).toBe('1.131.537')
  })

  it('formats file sizes', () => {
    expect(formatBytes(348)).toBe('348 B')
    expect(formatBytes(2048)).toBe('2 KB')
    expect(formatBytes(12_760_995)).toBe('12,2 MB')
  })

  it('shows GTFS times past midnight as clock times', () => {
    expect(formatGtfsTime('07:05:00')).toBe('07:05')
    expect(formatGtfsTime('25:10:00')).toBe('01:10')
  })
})
