import type { BoardDeparture } from './queries'

/** Do koliko minuta se prikazuje "za X min", a iza toga sat polaska. */
export const minutesShownUntil = 10

/** Koliko se polazak još prikazuje nakon svog vremena ("stiže"). */
const graceMs = 30_000

/** Pomak zone u minutama iz ISO vremena: "…+02:00" → 120, "…Z" → 0. */
export function offsetMinutes(isoTime: string): number {
  const match = /([+-])(\d{2}):(\d{2})$/.exec(isoTime)
  if (!match) {
    return 0
  }
  const minutes = Number(match[2]) * 60 + Number(match[3])
  return match[1] === '-' ? -minutes : minutes
}

/** Sat (HH:MM) u zoni s pomakom offset, neovisno o zoni preglednika. */
export function clockText(nowMs: number, offset: number): string {
  const local = new Date(nowMs + offset * 60_000)
  const hours = String(local.getUTCHours()).padStart(2, '0')
  const minutes = String(local.getUTCMinutes()).padStart(2, '0')
  return `${hours}:${minutes}`
}

/**
 * Polasci koji još nisu otišli. Odgovor može biti iz cachea (do 20 s), pa se
 * odlazak provjerava i na zaslonu.
 */
export function upcoming(departures: BoardDeparture[], nowMs: number): BoardDeparture[] {
  return departures.filter((d) => Date.parse(d.departureTime) >= nowMs - graceMs)
}

/** Cijele minute do polaska, najmanje 0. */
export function minutesUntil(departureTime: string, nowMs: number): number {
  return Math.max(0, Math.floor((Date.parse(departureTime) - nowMs) / 60_000))
}

/**
 * "stiže", "za 4 min" ili sat polaska (HH:MM). Sat se čita iz teksta jer je
 * vrijeme već u zoni prijevoznika.
 */
export function whenText(departureTime: string, nowMs: number): string {
  const minutes = minutesUntil(departureTime, nowMs)
  if (minutes < 1) {
    return 'stiže'
  }
  if (minutes <= minutesShownUntil) {
    return `za ${minutes} min`
  }
  return departureTime.slice(11, 16)
}

/** ?past= iz adrese: koliko otišlih polazaka prikazati; zadano 2, najviše 5 (granica backenda). */
export function parsePast(value: string | null): number {
  if (value === null) {
    return 2
  }
  const past = Number(value)
  return Number.isInteger(past) && past >= 0 ? Math.min(past, 5) : 2
}

/** Datum (YYYY-MM-DD) u zoni s pomakom offset. */
export function localDate(nowMs: number, offset: number): string {
  return new Date(nowMs + offset * 60_000).toISOString().slice(0, 10)
}

export type BoardItem =
  | {
      kind: 'departure'
      departure: BoardDeparture
      /** Polazak je otišao; prikazuje se zatamnjeno. */
      past: boolean
      /** Zadnji polazak te linije i smjera u danu voznog reda. */
      last: boolean
    }
  | {
      kind: 'end'
      /** Dan voznog reda sljedećih polazaka. */
      next: string
      /** Danas više nema polazaka (kraj dana je prije prvog retka). */
      none: boolean
    }

const sameLine = (a: BoardDeparture, b: BoardDeparture) =>
  a.routeName === b.routeName && a.headsign === b.headsign

/**
 * Redci zaslona: do <paramref name="past"/> otišlih polazaka, sljedeći
 * polasci i oznaka kraja dana voznog reda između dva dana. Polazak je zadnji
 * kad iza njega nema iste linije i smjera tog dana, a popis sigurno pokriva
 * ostatak dana: ima polazak idućeg dana ili je kraći od traženog
 * (<paramref name="complete"/>, samo za današnji dan).
 */
export function boardItems(
  departures: BoardDeparture[],
  recent: BoardDeparture[],
  nowMs: number,
  past: number,
  today: string,
  complete: boolean,
): BoardItem[] {
  const gone = (d: BoardDeparture) => Date.parse(d.departureTime) < nowMs - graceMs
  // Polasci koji su otišli nakon zadnjeg dohvata pridružuju se otišlima.
  const left = past > 0 ? [...recent, ...departures.filter(gone)].slice(-past) : []
  const next = upcoming(departures, nowMs)
  const rows = [
    ...left.map((departure) => ({ departure, past: true })),
    ...next.map((departure) => ({ departure, past: false })),
  ]

  const items: BoardItem[] = []
  // Prvi redak je već idući dan voznog reda: danas više nema polazaka.
  if (rows.length > 0 && rows[0].departure.serviceDate > today) {
    items.push({ kind: 'end', next: rows[0].departure.serviceDate, none: true })
  }
  rows.forEach(({ departure, past: isPast }, i) => {
    const previous = rows[i - 1]?.departure
    if (previous && departure.serviceDate > previous.serviceDate) {
      items.push({ kind: 'end', next: departure.serviceDate, none: false })
    }
    let last = false
    if (!isPast) {
      const later = next.slice(next.indexOf(departure) + 1)
      // Kraći popis pokriva ostatak današnjeg dana, ali ne i sutrašnjeg (24 h od sada).
      const covered =
        later.some((d) => d.serviceDate > departure.serviceDate) ||
        (complete && departure.serviceDate <= today)
      last =
        covered &&
        !later.some((d) => d.serviceDate === departure.serviceDate && sameLine(d, departure))
    }
    items.push({ kind: 'departure', departure, past: isPast, last })
  })
  return items
}

/** "sutra", "danas" ili dan u tjednu s datumom ("pon 20. 10."). */
export function dayLabel(date: string, today: string): string {
  const days = Math.round((Date.parse(date) - Date.parse(today)) / 86_400_000)
  if (days === 0) {
    return 'danas'
  }
  if (days === 1) {
    return 'sutra'
  }
  const [year, month, day] = date.split('-').map(Number)
  const weekday = ['ned', 'pon', 'uto', 'sri', 'čet', 'pet', 'sub'][
    new Date(Date.UTC(year, month - 1, day)).getUTCDay()
  ]
  return `${weekday} ${day}. ${month}.`
}

/** ?limit= iz adrese; zadano 12, najviše 30 (granica backenda). */
export function parseLimit(value: string | null): number {
  const limit = Number(value)
  return Number.isInteger(limit) && limit >= 1 ? Math.min(limit, 30) : 12
}
