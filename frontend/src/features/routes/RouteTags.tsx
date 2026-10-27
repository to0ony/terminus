import { Badge, Group, Text, Tooltip } from '@mantine/core'
import { RouteBadge } from './RouteBadge'
import type { components } from '@/api/schema'

type RouteTag = components['schemas']['RouteTagResponse']

/** Najviše oznaka linija u retku; ostale se zbroje u „+N”. */
const maxRouteTags = 6

/** Oznake linija u boji (npr. linije koje staju na stanici); višak kao „+N” s popisom. */
export function RouteTags({ routes }: { routes: RouteTag[] }) {
  if (routes.length === 0) {
    return (
      <Text size="sm" c="dimmed">
        —
      </Text>
    )
  }
  const rest = routes.length - maxRouteTags
  return (
    <Group gap={4} wrap="wrap">
      {routes.slice(0, maxRouteTags).map((route) => (
        <RouteBadge key={route.id} route={route} size="sm" />
      ))}
      {rest > 0 && (
        <Tooltip
          label={routes
            .slice(maxRouteTags)
            .map((r) => r.shortName ?? r.code)
            .join(', ')}
        >
          <Badge size="sm" variant="default" tt="none">
            +{rest}
          </Badge>
        </Tooltip>
      )}
    </Group>
  )
}
