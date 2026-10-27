import { Anchor, Badge, Text } from '@mantine/core'
import { Link } from 'react-router'
import type { DataColumn } from '@/components/DataTable/tableModel'
import { formatTime } from '@/features/live/clock'
import { formatNumber } from '@/lib/format'
import type { RouteTableRow } from './queries'
import { RouteBadge } from './RouteBadge'
import { RouteTypeIcon } from './RouteTypeIcon'

const dateFormat = new Intl.DateTimeFormat('hr-HR', { dateStyle: 'short' })

const dimmed = (value: string) => (
  <Text size="sm" c="dimmed">
    {value}
  </Text>
)

const count = (value: number) => (value > 0 ? formatNumber(value) : dimmed('—'))

/**
 * Stupci tablice linija; prijevoznik je zadano vidljiv samo kad ih ima više.
 * Vremena su od početka dana voznog reda, a iza ponoći opet od 00.
 */
export function routeColumns(showAgency: boolean): DataColumn<RouteTableRow>[] {
  return [
    {
      id: 'badge',
      header: 'Linija',
      sortKey: 'ShortName',
      alwaysVisible: true,
      width: 90,
      mobile: 'leading',
      cell: ({ route }) => <RouteBadge route={route} />,
    },
    {
      id: 'name',
      header: 'Naziv',
      sortKey: 'LongName',
      alwaysVisible: true,
      mobile: 'title',
      cell: ({ route }) => (
        <Anchor component={Link} to={`/routes/${route.id}`} c="inherit" size="sm" fw={500}>
          {route.longName ?? route.description ?? route.code}
        </Anchor>
      ),
    },
    {
      id: 'code',
      header: 'GTFS ID',
      sortKey: 'Code',
      hiddenByDefault: true,
      cell: ({ route }) => dimmed(route.code),
    },
    {
      id: 'type',
      header: 'Vrsta',
      sortKey: 'RouteType',
      align: 'center',
      width: 80,
      cell: ({ route }) => <RouteTypeIcon routeType={route.routeType} />,
    },
    {
      id: 'agency',
      header: 'Prijevoznik',
      sortKey: 'Agency',
      hiddenByDefault: !showAgency,
      cell: ({ agencyName }) => agencyName || dimmed('—'),
    },
    {
      id: 'variants',
      header: 'Varijante',
      sortKey: 'VariantCount',
      align: 'right',
      width: 110,
      cell: ({ variantCount }) => count(variantCount),
    },
    {
      id: 'trips',
      header: 'Vožnje',
      sortKey: 'TripCount',
      align: 'right',
      width: 100,
      mobile: 'meta',
      cell: ({ tripCount }) => count(tripCount),
    },
    {
      id: 'stops',
      header: 'Stanice',
      align: 'right',
      width: 100,
      hiddenByDefault: true,
      cell: ({ stopCount }) => count(stopCount),
    },
    {
      id: 'hours',
      header: 'Vozi',
      width: 130,
      mobile: 'meta',
      cell: ({ firstDeparture, lastArrival }) =>
        firstDeparture != null && lastArrival != null
          ? `${formatTime(firstDeparture, false)} – ${formatTime(lastArrival, false)}`
          : dimmed('—'),
    },
    {
      id: 'status',
      header: 'Status',
      sortKey: 'IsActive',
      width: 110,
      cell: ({ route }) => (
        <Badge variant="light" color={route.isActive ? 'green' : 'gray'} tt="none">
          {route.isActive ? 'Aktivna' : 'Neaktivna'}
        </Badge>
      ),
    },
    {
      id: 'updated',
      header: 'Izmijenjeno',
      sortKey: 'UpdatedAt',
      hiddenByDefault: true,
      cell: ({ route }) => dimmed(dateFormat.format(new Date(route.updatedAt))),
    },
  ]
}
