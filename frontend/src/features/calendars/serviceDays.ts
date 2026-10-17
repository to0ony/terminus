import type { CalendarDate, ServiceCalendar } from './queries'

const weekdayKeys = [
  'sunday',
  'monday',
  'tuesday',
  'wednesday',
  'thursday',
  'friday',
  'saturday',
] as const

/** Dani u tjednu od ponedjeljka, s kraticama za prikaz. */
export const weekdays = [
  { key: 'monday', short: 'Pon' },
  { key: 'tuesday', short: 'Uto' },
  { key: 'wednesday', short: 'Sri' },
  { key: 'thursday', short: 'Čet' },
  { key: 'friday', short: 'Pet' },
  { key: 'saturday', short: 'Sub' },
  { key: 'sunday', short: 'Ned' },
] as const

export const exceptionAdded = 1
export const exceptionRemoved = 2

/**
 * Vozi li kalendar zadani dan (YYYY-MM-DD) i zašto, po pravilima GTFS-a:
 * iznimka (calendar_dates) ima prednost, inače dan u tjednu unutar razdoblja.
 */
export type ServiceDay = 'regular' | 'added' | 'removed' | null

export function serviceDay(
  calendar: ServiceCalendar,
  exceptions: Map<string, number>,
  date: string,
): ServiceDay {
  const exception = exceptions.get(date)
  if (exception === exceptionAdded) {
    return 'added'
  }
  const inRange = date >= calendar.startDate && date <= calendar.endDate
  const weekday = weekdayKeys[new Date(`${date}T12:00:00Z`).getUTCDay()]
  const regular = inRange && calendar[weekday]
  if (exception === exceptionRemoved) {
    return regular ? 'removed' : null
  }
  return regular ? 'regular' : null
}

export function exceptionMap(dates: CalendarDate[]): Map<string, number> {
  return new Map(dates.map((d) => [d.date, d.exceptionType]))
}

/** Kalendar bez ijednog dana u tjednu vozi samo na datume iz iznimaka. */
export function hasWeekdays(calendar: ServiceCalendar): boolean {
  return weekdays.some((day) => calendar[day.key])
}
