import type { components } from '@/api/schema'
import { day, nowInZone } from './clock'

type StopDayDeparture = components['schemas']['StopDayDepartureResponse']

/** Polazak sa stanice, s vremenom u sekundama od ponoći svog dana. */
export interface Departure extends StopDayDeparture {
  seconds: number
  /** 0 za dan karte, 1 za sljedeći dan. */
  dayOffset: number
}

/**
 * Polasci dana s vremenom u sekundama od ponoći dana karte (iza ponoći više od
 * 24 h), redom; samo oni od zadanog vremena nadalje.
 */
export function departuresFrom(
  departures: StopDayDeparture[],
  date: string,
  timeZone: string,
  from: number,
  dayOffset = 0,
): Departure[] {
  return departures
    .map((departure) => {
      const local = nowInZone(timeZone, new Date(departure.departureTime))
      const seconds = Math.round(local.seconds) + (local.date > date ? day : 0)
      return { ...departure, seconds, dayOffset }
    })
    .filter((departure) => departure.seconds >= from)
    .sort((a, b) => a.seconds - b.seconds)
}

/** Polasci istog smjera: ista linija i isti natpis. */
export function sameDirection(a: StopDayDeparture, b: StopDayDeparture): boolean {
  return a.routeId === b.routeId && a.headsign === b.headsign
}

/** Za koliko minuta, ili vrijeme kad je dalje od sat vremena. */
export function minutesUntil(seconds: number, now: number): number {
  return Math.max(0, Math.floor((seconds - now) / 60))
}
