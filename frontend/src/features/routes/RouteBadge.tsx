import { Badge, type MantineSize } from '@mantine/core'
import type { Route } from './queries'

type RouteLike = Pick<Route, 'code' | 'shortName' | 'color' | 'textColor'>

/** Oznaka linije u njezinoj boji (GTFS route_color / route_text_color, bez #). */
export function RouteBadge({ route, size = 'md' }: { route: RouteLike; size?: MantineSize }) {
  const label = route.shortName ?? route.code

  if (!route.color) {
    return (
      <Badge variant="default" radius="sm" miw={44} tt="none" size={size}>
        {label}
      </Badge>
    )
  }

  return (
    <Badge
      radius="sm"
      miw={44}
      size={size}
      tt="none"
      color={`#${route.color}`}
      c={`#${route.textColor ?? '000000'}`}
    >
      {label}
    </Badge>
  )
}
