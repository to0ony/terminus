import type { DayType, Season, SeasonDay } from './queries'

/** Ključevi dana u tjednu po Date.getUTCDay() (nedjelja je 0). */
const weekdayKeys = [
  'sunday',
  'monday',
  'tuesday',
  'wednesday',
  'thursday',
  'friday',
  'saturday',
] as const

type WeekdayKey = (typeof weekdayKeys)[number]

/** Dani u tjednu od ponedjeljka. */
export const weekdays: { key: WeekdayKey; short: string; name: string }[] = [
  { key: 'monday', short: 'Pon', name: 'ponedjeljak' },
  { key: 'tuesday', short: 'Uto', name: 'utorak' },
  { key: 'wednesday', short: 'Sri', name: 'srijeda' },
  { key: 'thursday', short: 'Čet', name: 'četvrtak' },
  { key: 'friday', short: 'Pet', name: 'petak' },
  { key: 'saturday', short: 'Sub', name: 'subota' },
  { key: 'sunday', short: 'Ned', name: 'nedjelja' },
]

/** Boje tipova dana redom; ista boja svuda znači isti tip dana. */
const palette = [
  'blue',
  'teal',
  'orange',
  'grape',
  'pink',
  'cyan',
  'lime',
  'indigo',
  'yellow',
  'red',
]

export function dayTypeColor(index: number): string {
  return palette[index % palette.length]
}

export function weekdayOf(date: string): WeekdayKey {
  return weekdayKeys[new Date(`${date}T12:00:00Z`).getUTCDay()]
}

/** Uzastopni dani kao raspon: "Pon–Pet", "Sub", "Pon–Čet, Ned". */
export function weekdayLabel(dayType: Pick<DayType, WeekdayKey>): string {
  const parts: string[] = []
  for (let i = 0; i < weekdays.length; i++) {
    if (!dayType[weekdays[i].key]) {
      continue
    }
    let end = i
    while (end + 1 < weekdays.length && dayType[weekdays[end + 1].key]) {
      end++
    }
    parts.push(end > i ? `${weekdays[i].short}–${weekdays[end].short}` : weekdays[i].short)
    i = end
  }
  return parts.length > 0 ? parts.join(', ') : 'samo posebni datumi'
}

export type DayKind = 'outside' | 'regular' | 'holiday' | 'period' | 'manual'

export interface DayState {
  kind: DayKind
  /** Tip dana koji vozi; null ako taj dan ništa ne vozi. */
  dayType: DayType | null
  /** Dan sezone koji odstupa (praznik ili ručna iznimka), ako postoji. */
  day?: SeasonDay
}

/**
 * Što vozi zadani dan (YYYY-MM-DD): tip dana prema danu u tjednu, osim ako
 * praznik ili ručna iznimka kažu drukčije.
 */
export function dayState(season: Season, days: Map<string, SeasonDay>, date: string): DayState {
  if (date < season.startDate || date > season.endDate) {
    return { kind: 'outside', dayType: null }
  }

  const regular = season.dayTypes.find((t) => t[weekdayOf(date)]) ?? null
  const day = days.get(date)
  if (!day) {
    return { kind: 'regular', dayType: regular }
  }

  const dayType =
    day.mode === 'RunsAs'
      ? (season.dayTypes.find((t) => t.id === day.calendarId) ?? null)
      : day.mode === 'NoService'
        ? null
        : regular

  const kind = day.source === 'Holiday' ? 'holiday' : day.source === 'Period' ? 'period' : 'manual'
  return { kind, dayType, day }
}

export function dayMap(season: Season): Map<string, SeasonDay> {
  return new Map(season.days.map((d) => [d.date, d]))
}

/** Prvi dan svakog mjeseca sezone (YYYY-MM-01). */
export function monthsOf(start: string, end: string): string[] {
  const months: string[] = []
  let [year, month] = start.split('-').map(Number)
  const [endYear, endMonth] = end.split('-').map(Number)
  while (year < endYear || (year === endYear && month <= endMonth)) {
    months.push(`${year}-${String(month).padStart(2, '0')}-01`)
    month++
    if (month > 12) {
      month = 1
      year++
    }
  }
  return months
}

/** Opis odstupanja: "kao Nedjelja", "ne vozi" ili "normalno". */
export function dayModeLabel(season: Season, day: SeasonDay): string {
  if (day.mode === 'NoService') {
    return 'ne vozi'
  }
  if (day.mode === 'RunsAs') {
    const dayType = season.dayTypes.find((t) => t.id === day.calendarId)
    return `kao ${dayType?.name ?? '?'}`
  }
  return 'normalno'
}

/** Zadani tipovi dana za novu sezonu. */
export const dayTypePresets = {
  week: {
    label: 'Radni dan · Subota · Nedjelja',
    dayTypes: [
      days('Radni dan', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday'),
      days('Subota', 'saturday'),
      days('Nedjelja', 'sunday'),
    ],
  },
  weekend: {
    label: 'Radni dan · Vikend',
    dayTypes: [
      days('Radni dan', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday'),
      days('Vikend', 'saturday', 'sunday'),
    ],
  },
  daily: {
    label: 'Svaki dan isto',
    dayTypes: [
      days(
        'Svaki dan',
        'monday',
        'tuesday',
        'wednesday',
        'thursday',
        'friday',
        'saturday',
        'sunday',
      ),
    ],
  },
} as const

export type DayTypePreset = keyof typeof dayTypePresets

function days(name: string, ...keys: WeekdayKey[]) {
  return {
    name,
    ...Object.fromEntries(weekdayKeys.map((key) => [key, keys.includes(key)])),
  } as { name: string } & Record<WeekdayKey, boolean>
}
