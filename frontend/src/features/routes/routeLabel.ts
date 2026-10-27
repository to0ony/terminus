import type { Route } from './queries'

/** Kratki naziv linije za putokaz i naslove: oznaka i, ako postoji, puni naziv. */
export function routeLabel(route: Pick<Route, 'code' | 'shortName' | 'longName'>) {
  const short = route.shortName ?? route.code
  return route.longName ? `${short} ${route.longName}` : short
}
