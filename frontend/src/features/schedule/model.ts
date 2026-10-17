import type { components } from '@/api/schema'

type TripResponse = components['schemas']['ScheduleTripResponse']
type StopTimeResponse = components['schemas']['ScheduleStopTimeResponse']
type TripRequest = components['schemas']['ScheduleTripRequest']
type DeleteRequest = components['schemas']['ScheduleDeleteRequest']

export type EditorStopTime = StopTimeResponse

/** Vožnja u grafikonu; nova vožnja nema Id ni UpdatedAt. */
export interface EditorTrip extends Omit<TripResponse, 'id' | 'code' | 'updatedAt'> {
  /** Stalni ključ u editoru: Id postojeće vožnje ili privremeni ključ nove. */
  key: string
  id: string | null
  code: string | null
  updatedAt: string | null
}

export const minute = 60
export const hour = 3600
/** GTFS dopušta vremena do 48:00. */
export const maxSeconds = 48 * hour

/** Prikazani raspon vremena [od, do] u sekundama. */
export type View = [number, number]

/** Prozor vremena unutar dana voznog reda, od 20 minuta do cijelog dana. */
export function clampView([v0, v1]: View, [s0, s1]: View): View {
  const length = Math.min(Math.max(v1 - v0, 20 * minute), s1 - s0)
  let start = Math.max(s0, Math.min(v0, s1 - length))
  start = Math.max(0, Math.min(start, maxSeconds - length))
  return [start, start + length]
}

export function fromResponse(trip: TripResponse): EditorTrip {
  return { ...trip, key: trip.id }
}

let nextKey = 0

/** Privremeni ključ nove vožnje. */
export function newKey(): string {
  nextKey++
  return `new-${nextKey}`
}

/** "07:05" iz sekundi; sati iznad 24 ostaju (24:10 je 00:10 sljedećeg dana). */
export function formatTime(seconds: number): string {
  const h = Math.floor(seconds / hour)
  const m = Math.floor((seconds % hour) / minute)
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`
}

/** "7:05", "07:05" ili "24:10" u sekunde; null ako nije ispravno. */
export function parseTime(value: string): number | null {
  const match = /^(\d{1,2}):(\d{2})$/.exec(value.trim())
  if (!match) {
    return null
  }
  const [h, m] = [Number(match[1]), Number(match[2])]
  if (m > 59 || h * hour + m * minute > maxSeconds) {
    return null
  }
  return h * hour + m * minute
}

/** Za svako vrijeme vožnje redak grafikona (stanice smjera), redom. */
export function rowIndexes(rows: string[], stopIds: string[]): number[] {
  const result: number[] = []
  let from = 0
  for (const stopId of stopIds) {
    let index = rows.indexOf(stopId, from)
    if (index < 0) {
      // Ne bi se smjelo dogoditi; vožnja tada ide na prvo pojavljivanje stanice.
      index = Math.max(0, rows.indexOf(stopId))
    }
    result.push(index)
    from = index + 1
  }
  return result
}

/**
 * Položaj redaka od 0 do 1, razmaknutih po tipičnom (srednjem) vremenu vožnje
 * između susjednih stanica. Tako je pravilna vožnja ravna crta, a sporiji ili
 * brži dio vidi se kao lom. Dionice bez podataka i vrlo kratke dobivaju
 * najmanji razmak, da se nazivi stanica ne preklapaju.
 */
export function rowPositions(rows: string[], trips: EditorTrip[]): number[] {
  if (rows.length < 2) {
    return rows.map(() => 0)
  }

  const samples: number[][] = rows.slice(1).map(() => [])
  for (const trip of trips) {
    const indexes = rowIndexes(
      rows,
      trip.stopTimes.map((st) => st.stopId),
    )
    for (let i = 1; i < indexes.length; i++) {
      if (indexes[i] === indexes[i - 1] + 1) {
        samples[indexes[i - 1]].push(
          trip.stopTimes[i].arrivalTime - trip.stopTimes[i - 1].departureTime,
        )
      }
    }
  }

  const gaps = samples.map((s) => Math.max(median(s) ?? 2 * minute, 0.75 * minute))
  const total = gaps.reduce((a, b) => a + b, 0)
  const positions = [0]
  for (const gap of gaps) {
    positions.push(positions.at(-1)! + gap / total)
  }
  return positions
}

function median(values: number[]): number | null {
  if (values.length === 0) {
    return null
  }
  const sorted = [...values].sort((a, b) => a - b)
  return sorted[Math.floor(sorted.length / 2)]
}

/** Pomak cijele vožnje (svih vremena). */
export function shiftTrip(trip: EditorTrip, delta: number): EditorTrip {
  return {
    ...trip,
    stopTimes: trip.stopTimes.map((st) => ({
      ...st,
      arrivalTime: st.arrivalTime + delta,
      departureTime: st.departureTime + delta,
    })),
  }
}

/**
 * Pomak vremena na jednoj stanici; s <paramref name="cascade"/> pomiču se i
 * sve stanice iza nje (mijenja se vrijeme vožnje dionice).
 */
export function shiftStop(
  trip: EditorTrip,
  index: number,
  delta: number,
  cascade: boolean,
): EditorTrip {
  return {
    ...trip,
    stopTimes: trip.stopTimes.map((st, i) =>
      i === index || (cascade && i > index)
        ? { ...st, arrivalTime: st.arrivalTime + delta, departureTime: st.departureTime + delta }
        : st,
    ),
  }
}

/** Postavlja dolazak i polazak na stanici (npr. iz tablice u panelu). */
export function setStopTime(
  trip: EditorTrip,
  index: number,
  arrivalTime: number,
  departureTime: number,
): EditorTrip {
  return {
    ...trip,
    stopTimes: trip.stopTimes.map((st, i) =>
      i === index ? { ...st, arrivalTime, departureTime } : st,
    ),
  }
}

/**
 * GTFS pravila za vremena vožnje; null ako je sve ispravno, inače opis prve
 * greške.
 */
export function validateTrip(trip: EditorTrip, stopName: (id: string) => string): string | null {
  if (trip.stopTimes.length < 2) {
    return 'Vožnja mora imati barem dvije stanice.'
  }
  for (let i = 0; i < trip.stopTimes.length; i++) {
    const st = trip.stopTimes[i]
    if (st.arrivalTime < 0 || st.departureTime > maxSeconds) {
      return `${stopName(st.stopId)}: vrijeme mora biti između 00:00 i 48:00.`
    }
    if (st.departureTime < st.arrivalTime) {
      return `${stopName(st.stopId)}: polazak je prije dolaska.`
    }
    const previous = trip.stopTimes[i - 1]
    if (previous && st.arrivalTime < previous.departureTime) {
      return `${stopName(st.stopId)}: dolazak (${formatTime(st.arrivalTime)}) je prije polaska s prethodne stanice (${formatTime(previous.departureTime)}).`
    }
  }
  return null
}

export function firstDeparture(trip: EditorTrip): number {
  return trip.stopTimes[0]?.departureTime ?? 0
}

/**
 * Predložak za nove vožnje: vožnja najčešćeg redoslijeda stanica (glavni
 * obrazac), a među njima ona s polaskom najbližim zadanom vremenu, jer se
 * vremena vožnje kroz dan mijenjaju.
 */
export function templateTrip(trips: EditorTrip[], near?: number): EditorTrip | null {
  if (trips.length === 0) {
    return null
  }
  const counts = new Map<string, number>()
  for (const trip of trips) {
    const pattern = sequenceKey(trip)
    counts.set(pattern, (counts.get(pattern) ?? 0) + 1)
  }
  const main = [...counts.entries()].sort((a, b) => b[1] - a[1])[0][0]
  const candidates = trips.filter((t) => sequenceKey(t) === main)
  if (near === undefined) {
    return candidates[0]
  }
  return candidates.reduce((best, t) =>
    Math.abs(firstDeparture(t) - near) < Math.abs(firstDeparture(best) - near) ? t : best,
  )
}

function sequenceKey(trip: EditorTrip): string {
  return trip.stopTimes.map((st) => st.stopId).join(',')
}

/** Nova vožnja s vremenima vožnje predloška i prvim polaskom u <paramref name="departure"/>. */
export function tripFrom(template: EditorTrip, departure: number, calendarId: string): EditorTrip {
  return {
    ...shiftTrip(template, departure - firstDeparture(template)),
    key: newKey(),
    id: null,
    code: null,
    updatedAt: null,
    serviceCalendarId: calendarId,
    // Vozilo nove vožnje nije poznato.
    blockId: null,
  }
}

/** Polasci od <paramref name="from"/> do <paramref name="to"/> (uključivo) svakih <paramref name="every"/> sekundi. */
export function taktDepartures(from: number, to: number, every: number): number[] {
  const result: number[] = []
  if (every <= 0) {
    return result
  }
  for (let t = from; t <= to; t += every) {
    result.push(t)
  }
  return result
}

/** Razmaci između uzastopnih polazaka (prvo vrijeme vožnje), za traku intervala. */
export function headways(trips: EditorTrip[]): { at: number; gap: number }[] {
  const departures = trips.map(firstDeparture).sort((a, b) => a - b)
  return departures
    .slice(1)
    .map((t, i) => ({ at: (t + departures[i]) / 2, gap: t - departures[i] }))
}

/**
 * Što treba spremiti: nove i izmijenjene vožnje te obrisane, prema vožnjama
 * kakve su učitane.
 */
export function changes(
  original: EditorTrip[],
  current: EditorTrip[],
): { upserts: TripRequest[]; deletes: DeleteRequest[] } {
  const before = new Map(original.map((t) => [t.key, t]))
  const after = new Map(current.map((t) => [t.key, t]))

  const upserts = current
    .filter((t) => {
      const old = before.get(t.key)
      return !old || JSON.stringify(old) !== JSON.stringify(t)
    })
    .map(toRequest)

  const deletes = original
    .filter((t) => !after.has(t.key) && t.id && t.updatedAt)
    .map((t) => ({ id: t.id, updatedAt: t.updatedAt }))

  return { upserts, deletes }
}

function toRequest(trip: EditorTrip): TripRequest {
  return {
    id: trip.id,
    updatedAt: trip.updatedAt,
    serviceCalendarId: trip.serviceCalendarId,
    headsign: trip.headsign,
    shortName: trip.shortName,
    blockId: trip.blockId,
    shapeId: trip.shapeId,
    wheelchairAccessible: trip.wheelchairAccessible,
    bikesAllowed: trip.bikesAllowed,
    stopTimes: trip.stopTimes.map((st) => ({ ...st })),
  }
}
