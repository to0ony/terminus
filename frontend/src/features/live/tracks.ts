import type { components } from '@/api/schema'
import type { LiveTrip } from './queries'
import { buildTrack, type LngLat, type Track } from './simulation'

/** Vožnje dana jedne linije ili svih linija. */
interface TripsDay {
  trips: LiveTrip[]
  shapes: components['schemas']['LiveShapeResponse'][]
  stops: components['schemas']['TimetableStopResponse'][]
}

/** Vožnja dana spremna za simulaciju. */
export interface Vehicle {
  /** Jedinstven i kad ista vožnja vozi i prethodni dan iza ponoći. */
  key: string
  trip: LiveTrip
  track: Track
  /** Boja linije bez #, za ikonu vozila. */
  color: string
}

/** Plava kao na Google kartama, za linije bez boje. */
export const defaultRouteColor = '1a73e8'

function stopPositions(day: TripsDay) {
  return new Map(
    day.stops
      .filter((s) => s.latitude !== null && s.longitude !== null)
      .map((s) => [s.id, [s.longitude, s.latitude] as LngLat]),
  )
}

/**
 * Vožnje dana kao simulirana vozila: po trasi vožnje, inače ravno od stanice do
 * stanice. Boja vozila je boja njegove linije.
 */
export function buildVehicles(
  day: TripsDay,
  colorOf: (trip: LiveTrip) => string | null,
): Vehicle[] {
  const stops = stopPositions(day)
  const shapes = new Map(day.shapes.map((s) => [s.id, s.coordinates as LngLat[]]))

  return day.trips.map((trip) => {
    const stopTimes = trip.stopTimes.filter((st) => stops.has(st.stopId))
    return {
      key: `${trip.id}${trip.previousDay ? '-1' : ''}`,
      trip,
      color: colorOf(trip) ?? defaultRouteColor,
      track: buildTrack(
        stopTimes,
        stopTimes.map((st) => stops.get(st.stopId)!),
        trip.shapeId ? (shapes.get(trip.shapeId) ?? null) : null,
      ),
    }
  })
}

/** Crte za kartu: svaka trasa jednom, a vožnje bez trase kao crta stanica. */
export function routeLines(day: TripsDay): LngLat[][] {
  const stops = stopPositions(day)
  const shaped = new Set(day.shapes.map((s) => s.id))
  const straight = new Map<string, LngLat[]>()
  for (const trip of day.trips) {
    if (!trip.shapeId || !shaped.has(trip.shapeId)) {
      const points = trip.stopTimes
        .map((st) => stops.get(st.stopId))
        .filter((point): point is LngLat => point !== undefined)
      straight.set(trip.stopTimes.map((st) => st.stopId).join(','), points)
    }
  }
  return [...day.shapes.map((s) => s.coordinates as LngLat[]), ...straight.values()]
}
