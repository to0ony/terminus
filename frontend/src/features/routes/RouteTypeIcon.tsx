import { ThemeIcon, Tooltip } from '@mantine/core'
import { createElement } from 'react'
import { routeTypeIcon, routeTypeLabel } from './routeTypes'

/**
 * Vrsta prijevoza kao ikona s nazivom u oblačiću. Svijetla siva varijanta
 * teme, pa se sama prilagođava svijetlom i tamnom načinu.
 */
export function RouteTypeIcon({ routeType }: { routeType: number }) {
  const label = routeTypeLabel(routeType)
  return (
    <Tooltip label={label}>
      <ThemeIcon variant="light" color="gray" size="md" radius="sm" aria-label={label}>
        {createElement(routeTypeIcon(routeType), { size: 18, stroke: 1.7 })}
      </ThemeIcon>
    </Tooltip>
  )
}
