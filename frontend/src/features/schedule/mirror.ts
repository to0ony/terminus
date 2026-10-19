import {
  type EditorTrip,
  firstDeparture,
  maxSeconds,
  minute,
  type ScheduleVariant,
  tripFromVariant,
} from './model'

/**
 * Kako nastaje vrijeme povratka: okret na terminusu (dolazak + okret) ili
 * pomak od polaska u suprotnom smjeru.
 */
export type MirrorMode = 'turn' | 'offset'

export interface MirrorSettings {
  mode: MirrorMode
  /** Zajednički okret ili pomak u sekundama. */
  offset: number
  /** Zaključani polasci: vlastiti okret ili pomak po ključu polaska. */
  overrides: Record<string, number>
}

/** Polazak suprotnog smjera i povratak koji iz njega nastaje. */
export interface MirrorRow {
  /** Polazak (svi njegovi tipovi dana zajedno) ili vožnja bez polaska. */
  key: string
  source: EditorTrip
  dayTypes: string[]
  /** Polazak s prve stanice i dolazak na zadnju, u suprotnom smjeru. */
  start: number
  end: number
  /** Okret ili pomak koji vrijedi za ovaj polazak. */
  offset: number
  locked: boolean
  /** Polazak povratka s prve stanice. */
  departure: number
  /** Povratak bi završio iza 48:00, pa ne može nastati. */
  tooLate: boolean
  /** U povratnom smjeru već postoji vožnja u isto vrijeme (±2 min) u nekom od tipova dana. */
  existing: boolean
}

/** Koliko blizu postojeće vožnje se povratak smatra već upisanim. */
const sameTime = 2 * minute

/**
 * Povratni polasci iz vožnji suprotnog smjera: svaki polazak (sa svim tipovima
 * dana) daje jedan povratak, s okretom ili pomakom koji je zajednički ili
 * zaključan za taj polazak.
 */
export function mirrorRows(
  sourceTrips: EditorTrip[],
  settings: MirrorSettings,
  existing: EditorTrip[],
  /** Trajanje povratne varijante, od polaska s prve do dolaska na zadnju stanicu. */
  duration: number,
): MirrorRow[] {
  const groups = new Map<string, EditorTrip>()
  for (const trip of sourceTrips) {
    if (trip.stopTimes.length === 0) {
      continue
    }
    const key = trip.departureId ?? trip.key
    if (!groups.has(key)) {
      groups.set(key, trip)
    }
  }

  return [...groups.entries()]
    .map(([key, source]): MirrorRow => {
      const start = firstDeparture(source)
      const end = source.stopTimes.at(-1)!.arrivalTime
      const locked = key in settings.overrides
      const offset = locked ? settings.overrides[key] : settings.offset
      const departure = (settings.mode === 'turn' ? end : start) + offset
      const dayTypes = source.dayTypes.length > 0 ? source.dayTypes : [source.serviceCalendarId]
      return {
        key,
        source,
        dayTypes,
        start,
        end,
        offset,
        locked,
        departure,
        tooLate: departure < 0 || departure + duration > maxSeconds,
        existing: existing.some(
          (t) =>
            dayTypes.includes(t.serviceCalendarId) &&
            Math.abs(firstDeparture(t) - departure) < sameTime,
        ),
      }
    })
    .sort((a, b) => a.start - b.start || a.key.localeCompare(b.key))
}

/**
 * Nove vožnje povratnog smjera po varijanti: jedna po polasku, u svim njegovim
 * tipovima dana, s vozilom (BlockId) i pristupačnošću polazne vožnje.
 * Povratak koji ne stane u dan (iza 48:00) ne nastaje.
 */
export function mirrorTrips(rows: MirrorRow[], variant: ScheduleVariant): EditorTrip[] {
  return rows
    .filter((row) => !row.tooLate)
    .map((row) => ({
      ...tripFromVariant(variant, row.departure, row.dayTypes[0]),
      dayTypes: row.dayTypes,
      blockId: row.source.blockId,
      wheelchairAccessible: row.source.wheelchairAccessible,
      bikesAllowed: row.source.bikesAllowed,
    }))
}
