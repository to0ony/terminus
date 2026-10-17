import type { StopDeparture } from './queries'

/**
 * Polasci po satu, kao na voznom redu na stanici. Vrijeme je već u zoni
 * prijevoznika (npr. 2026-10-17T23:40:00+02:00), pa se sat čita iz teksta, a
 * ne iz zone preglednika. Vožnje iza ponoći dolaze iza 23 h istog dana.
 */
export function groupByHour(departures: StopDeparture[]): [string, StopDeparture[]][] {
  const groups = new Map<string, StopDeparture[]>()
  for (const departure of departures) {
    const hour = departure.departureTime.slice(11, 13)
    const group = groups.get(hour)
    if (group) {
      group.push(departure)
    } else {
      groups.set(hour, [departure])
    }
  }
  return [...groups.entries()]
}
