/** Vrijeme vožnje i stajanje jedne stanice varijante (sekunde). */
interface Timed {
  travelTime: number
  dwellTime: number
}

/** Od polaska s prve do dolaska na zadnju stanicu (stajanja na krajevima se ne broje). */
export function duration(variant: { stops: Timed[] }): number {
  return variant.stops.reduce(
    (sum, stop, i) =>
      sum + stop.travelTime + (i > 0 && i < variant.stops.length - 1 ? stop.dwellTime : 0),
    0,
  )
}

/** Zračna udaljenost u metrima (haversine). */
export function distance(a: [number, number], b: [number, number]): number {
  const [lat1, lon1] = a.map((v) => (v * Math.PI) / 180)
  const [lat2, lon2] = b.map((v) => (v * Math.PI) / 180)
  const h =
    Math.sin((lat2 - lat1) / 2) ** 2 +
    Math.cos(lat1) * Math.cos(lat2) * Math.sin((lon2 - lon1) / 2) ** 2
  return 2 * 6_371_000 * Math.asin(Math.sqrt(h))
}

/**
 * Prosječna brzina s ubrzavanjem i stajanjem na semaforima (km/h) po GTFS
 * vrsti prijevoza; za prijedlog vremena vožnje nove dionice.
 */
function speed(routeType: number): number {
  const basic = routeType >= 100 ? Math.floor(routeType / 100) * 100 : routeType
  switch (basic) {
    case 0:
    case 900:
      return 16
    case 1:
    case 400:
      return 30
    case 2:
    case 100:
      return 45
    case 4:
    case 1000:
    case 1200:
      return 20
    default:
      return 20
  }
}

/**
 * Prijedlog vremena vožnje dionice iz zračne udaljenosti: ceste nisu ravne, pa
 * se udaljenost uveća za trećinu; zaokruženo na pola minute, najmanje minuta.
 */
export function suggestTravelTime(meters: number, routeType: number): number {
  const seconds = ((meters * 1.3) / 1000 / speed(routeType)) * 3600
  return Math.max(60, Math.round(seconds / 30) * 30)
}
