import { describe, expect, it } from 'vitest'
import type { ServiceCalendar } from './queries'
import { exceptionMap, serviceDay } from './serviceDays'

const weekdaysOnly = {
  monday: true,
  tuesday: true,
  wednesday: true,
  thursday: true,
  friday: true,
  saturday: false,
  sunday: false,
  startDate: '2026-10-01',
  endDate: '2026-10-31',
} as ServiceCalendar

describe('serviceDay', () => {
  it('runs on the calendar weekdays inside its period', () => {
    const none = exceptionMap([])
    expect(serviceDay(weekdaysOnly, none, '2026-10-16')).toBe('regular') // petak
    expect(serviceDay(weekdaysOnly, none, '2026-10-17')).toBeNull() // subota
    expect(serviceDay(weekdaysOnly, none, '2026-11-02')).toBeNull() // izvan razdoblja
  })

  it('lets exceptions add and remove days', () => {
    const exceptions = exceptionMap([
      { date: '2026-10-17', exceptionType: 1 },
      { date: '2026-10-26', exceptionType: 2 },
      { date: '2026-10-25', exceptionType: 2 },
    ])
    expect(serviceDay(weekdaysOnly, exceptions, '2026-10-17')).toBe('added')
    expect(serviceDay(weekdaysOnly, exceptions, '2026-10-26')).toBe('removed')
    // Ukidanje dana koji ionako ne vozi nema što prikazati.
    expect(serviceDay(weekdaysOnly, exceptions, '2026-10-25')).toBeNull()
  })
})
