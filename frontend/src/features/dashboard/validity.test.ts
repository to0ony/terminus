import { describe, expect, it } from 'vitest'
import type { ServiceOverview } from './queries'
import { validity } from './validity'

function overview(date: string, lastServiceDate: string | null) {
  return { date, lastServiceDate, activeCalendars: [] } as ServiceOverview
}

describe('validity', () => {
  it('counts the days left from the overview day', () => {
    expect(validity(overview('2026-10-17', '2026-12-31'))).toEqual({
      state: 'valid',
      lastDate: '2026-12-31',
      daysLeft: 75,
    })
  })

  it('warns when the timetable ends within 30 days', () => {
    expect(validity(overview('2026-12-20', '2026-12-31')).state).toBe('expiring')
    expect(validity(overview('2026-12-31', '2026-12-31'))).toMatchObject({ daysLeft: 0 })
  })

  it('is expired without a last service day', () => {
    expect(validity(overview('2027-01-05', null))).toEqual({ state: 'expired' })
  })
})
