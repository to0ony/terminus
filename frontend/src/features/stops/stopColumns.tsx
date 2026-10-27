import { Anchor, Badge, Group, Text } from '@mantine/core'
import { Link } from 'react-router'
import type { DataColumn } from '@/components/DataTable/tableModel'
import { RouteTags } from '@/features/routes/RouteTags'
import { formatNumber } from '@/lib/format'
import { locationTypeLabel } from './locationTypes'
import type { StopTableRow } from './queries'
import { StopAccess } from './StopAccess'

const dateFormat = new Intl.DateTimeFormat('hr-HR', { dateStyle: 'short' })

const dimmed = (value: string) => (
  <Text size="sm" c="dimmed">
    {value}
  </Text>
)

/** Stupci tablice stanica; zadano su vidljivi naziv, oznaka, peron i linije. */
export const stopColumns: DataColumn<StopTableRow>[] = [
  {
    id: 'name',
    header: 'Naziv',
    sortKey: 'Name',
    alwaysVisible: true,
    mobile: 'title',
    cell: ({ stop }) => (
      <Group gap={6} wrap="nowrap">
        <Anchor component={Link} to={`/stops/${stop.id}`} c="inherit" size="sm" fw={500}>
          {stop.name}
        </Anchor>
        {stop.locationType !== 0 && (
          <Badge size="xs" variant="light" tt="none">
            {locationTypeLabel(stop.locationType)}
          </Badge>
        )}
        {!stop.isActive && (
          <Badge size="xs" variant="light" color="gray" tt="none">
            Neaktivna
          </Badge>
        )}
      </Group>
    ),
  },
  {
    id: 'publicCode',
    header: 'Oznaka',
    sortKey: 'PublicCode',
    width: 110,
    mobile: 'meta',
    cell: ({ stop }) => dimmed(stop.publicCode ?? '—'),
  },
  {
    id: 'code',
    header: 'GTFS ID',
    sortKey: 'Code',
    hiddenByDefault: true,
    cell: ({ stop }) => dimmed(stop.code),
  },
  {
    id: 'platform',
    header: 'Peron',
    sortKey: 'PlatformCode',
    width: 80,
    mobile: 'meta',
    cell: ({ stop }) => stop.platformCode ?? '',
  },
  {
    id: 'type',
    header: 'Vrsta',
    sortKey: 'LocationType',
    hiddenByDefault: true,
    cell: ({ stop }) => locationTypeLabel(stop.locationType),
  },
  {
    id: 'station',
    header: 'Kolodvor',
    hiddenByDefault: true,
    cell: ({ stop, parentStationName }) =>
      stop.parentStationId && parentStationName ? (
        <Anchor component={Link} to={`/stops/${stop.parentStationId}`} size="sm">
          {parentStationName}
        </Anchor>
      ) : (
        ''
      ),
  },
  {
    id: 'routes',
    header: 'Linije',
    sortKey: 'RouteCount',
    mobile: 'meta',
    cell: ({ routes }) => <RouteTags routes={routes} />,
  },
  {
    id: 'platforms',
    header: 'Peroni',
    align: 'right',
    hiddenByDefault: true,
    cell: ({ platformCount }) => (platformCount > 0 ? formatNumber(platformCount) : ''),
  },
  {
    id: 'access',
    header: 'Pristupačnost',
    sortKey: 'WheelchairBoarding',
    hiddenByDefault: true,
    cell: ({ stop }) => <StopAccess value={stop.wheelchairBoarding} />,
  },
  {
    id: 'coordinates',
    header: 'Koordinate',
    hiddenByDefault: true,
    cell: ({ stop }) =>
      stop.latitude != null && stop.longitude != null ? (
        dimmed(`${stop.latitude.toFixed(5)}, ${stop.longitude.toFixed(5)}`)
      ) : (
        <Badge size="xs" variant="light" color="yellow" tt="none">
          Nema
        </Badge>
      ),
  },
  {
    id: 'status',
    header: 'Status',
    sortKey: 'IsActive',
    hiddenByDefault: true,
    cell: ({ stop }) => (
      <Badge variant="light" color={stop.isActive ? 'green' : 'gray'} tt="none">
        {stop.isActive ? 'Aktivna' : 'Neaktivna'}
      </Badge>
    ),
  },
  {
    id: 'updated',
    header: 'Izmijenjeno',
    sortKey: 'UpdatedAt',
    hiddenByDefault: true,
    cell: ({ stop }) => dimmed(dateFormat.format(new Date(stop.updatedAt))),
  },
]
