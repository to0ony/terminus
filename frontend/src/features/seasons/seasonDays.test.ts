import { describe, expect, it } from 'vitest'
import type { DayType, Season } from './queries'
import { dayMap, dayModeLabel, dayState, monthsOf, weekdayLabel } from './seasonDays'

const none = {
  monday: false,
  tuesday: false,
  wednesday: false,
  thursday: false,
  friday: false,
  saturday: false,
  sunday: false,
}

function dayType(id: string, name: string, days: Partial<typeof none>): DayType {
  return { id, code: id, name, tripCount: 0, ...none, ...days }
}

const workday = dayType('w', 'Radni dan', {
  monday: true,
  tuesday: true,
  wednesday: true,
  thursday: true,
  friday: true,
})
const saturday = dayType('s', 'Subota', { saturday: true })
const sunday = dayType('n', 'Nedjelja', { sunday: true })

const season: Season = {
  id: 'season',
  name: 'Zimski 2026/27',
  startDate: '2026-09-01',
  endDate: '2027-06-14',
  holidayCountry: 'HR',
  holidayMode: 'RunsAs',
  holidayCalendarId: 'n',
  dayTypes: [workday, saturday, sunday],
  days: [
    { date: '2026-12-25', mode: 'RunsAs', calendarId: 'n', source: 'Holiday', note: 'Božić' },
    { date: '2026-12-24', mode: 'RunsAs', calendarId: 's', source: 'Manual', note: 'Badnjak' },
    { date: '2027-01-02', mode: 'NoService', calendarId: null, source: 'Manual', note: null },
  ],
  createdAt: '',
  updatedAt: '',
}

const days = dayMap(season)

describe('dayState', () => {
  it('uses the weekday inside the season', () => {
    expect(dayState(season, days, '2026-12-23')).toMatchObject({
      kind: 'regular',
      dayType: workday,
    })
    expect(dayState(season, days, '2026-12-26').dayType).toBe(saturday)
  })

  it('applies holidays and manual exceptions', () => {
    expect(dayState(season, days, '2026-12-25')).toMatchObject({ kind: 'holiday', dayType: sunday })
    expect(dayState(season, days, '2026-12-24')).toMatchObject({
      kind: 'manual',
      dayType: saturday,
    })
    expect(dayState(season, days, '2027-01-02')).toMatchObject({ kind: 'manual', dayType: null })
  })

  it('marks days outside the season', () => {
    expect(dayState(season, days, '2026-08-31').kind).toBe('outside')
    expect(dayState(season, days, '2027-06-15').kind).toBe('outside')
  })
})

describe('weekdayLabel', () => {
  it('joins consecutive days into ranges', () => {
    expect(weekdayLabel(workday)).toBe('Pon–Pet')
    expect(weekdayLabel(saturday)).toBe('Sub')
    expect(weekdayLabel({ ...none, monday: true, tuesday: true, sunday: true })).toBe(
      'Pon–Uto, Ned',
    )
    expect(weekdayLabel(none)).toBe('samo posebni datumi')
  })
})

describe('monthsOf', () => {
  it('lists the months of the season across the new year', () => {
    expect(monthsOf('2026-11-15', '2027-02-01')).toEqual([
      '2026-11-01',
      '2026-12-01',
      '2027-01-01',
      '2027-02-01',
    ])
  })
})

describe('dayModeLabel', () => {
  it('names the day type or the lack of service', () => {
    expect(dayModeLabel(season, season.days[0])).toBe('kao Nedjelja')
    expect(dayModeLabel(season, season.days[2])).toBe('ne vozi')
  })
})
