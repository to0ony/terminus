import type { LiveRouteDay, LiveTrip } from './queries'
import { buildTrack, type LngLat, type Track } from './simulation'

/** Vožnja dana spremna za simulaciju. */
export interface Vehicle {
  /** Jedinstven i kad ista vožnja vozi i prethodni dan iza ponoći. */
  key: string
  trip: LiveTrip
  track: Track
}

/** Vožnje dana kao simulirana vozila: po trasi vožnje, inače ravno od stanice do stanice. */
export function buildVehicles(day: LiveRouteDay): Vehicle[] {
  const stops = new Map(
    day.stops
      .filter((s) => s.latitude !== null && s.longitude !== null)
      .map((s) => [s.id, [s.longitude, s.latitude] as LngLat]),
  )
  const shapes = new Map(day.shapes.map((s) => [s.id, s.coordinates as LngLat[]]))

  return day.trips.map((trip) => {
    const stopTimes = trip.stopTimes.filter((st) => stops.has(st.stopId))
    return {
      key: `${trip.id}${trip.previousDay ? '-1' : ''}`,
      trip,
      track: buildTrack(
        stopTimes,
        stopTimes.map((st) => stops.get(st.stopId)!),
        trip.shapeId ? (shapes.get(trip.shapeId) ?? null) : null,
      ),
    }
  })
}

/** Crte linije za kartu: svaka trasa jednom, a vožnje bez trase kao crta stanica. */
export function routeLines(day: LiveRouteDay, vehicles: Vehicle[]): LngLat[][] {
  const shapes = day.shapes.map((s) => s.coordinates as LngLat[])
  const shaped = new Set(day.shapes.map((s) => s.id))
  const straight = new Map<string, LngLat[]>()
  for (const vehicle of vehicles) {
    if (!vehicle.trip.shapeId || !shaped.has(vehicle.trip.shapeId)) {
      const key = vehicle.track.stopTimes.map((st) => st.stopId).join(',')
      straight.set(key, vehicle.track.path.points)
    }
  }
  return [...shapes, ...straight.values()]
}
