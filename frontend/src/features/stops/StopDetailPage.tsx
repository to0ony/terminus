import {
  Anchor,
  Badge,
  Button,
  Chip,
  Grid,
  Group,
  Paper,
  SimpleGrid,
  Stack,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { DatePickerInput } from '@mantine/dates'
import { useDisclosure } from '@mantine/hooks'
import { IconArrowLeft, IconCalendar, IconDeviceTv, IconFileTypePdf } from '@tabler/icons-react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { RouteBadge } from '@/features/routes/RouteBadge'
import { formatNumber } from '@/lib/format'
import { groupByHour } from './departures'
import { locationTypeLabel, wheelchairLabels } from './locationTypes'
import { type Stop, useStop, useStopDepartures, useStopRoutes } from './queries'
import { StopsMap } from './StopsMap'
import { TimetablePdfDialog } from './TimetablePdfDialog'

/** Stanica: podaci, linije koje na njoj staju i polasci za dan (?date=, ?route=). */
export function StopDetailPage() {
  const { stopId = '' } = useParams()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const date = searchParams.get('date')
  const routeFilter = searchParams.getAll('route')

  const stop = useStop(stopId)
  const routes = useStopRoutes(stopId)
  const departures = useStopDepartures(stopId, date)
  const [pdfOpened, pdf] = useDisclosure(false)

  const setDate = (value: string | null) =>
    setSearchParams(
      (current) => {
        if (value) {
          current.set('date', value)
        } else {
          current.delete('date')
        }
        return current
      },
      { replace: true },
    )

  const setRouteFilter = (ids: string[]) =>
    setSearchParams(
      (current) => {
        current.delete('route')
        ids.forEach((id) => current.append('route', id))
        return current
      },
      { replace: true },
    )

  if (stop.isPending) {
    return <PageLoader />
  }
  if (stop.isError) {
    return <QueryError error={stop.error} onRetry={() => void stop.refetch()} />
  }

  const shown =
    departures.data?.departures.filter(
      (d) => routeFilter.length === 0 || routeFilter.includes(d.routeId),
    ) ?? []
  const hasCoordinates = stop.data.latitude !== null && stop.data.longitude !== null
  // Polasci postoje samo za stajališta i kolodvore (location_type 0 i 1).
  const hasBoard = stop.data.locationType <= 1

  return (
    <Stack gap="md">
      <Anchor component={Link} to="/stops" size="sm">
        <Group gap={4}>
          <IconArrowLeft size={14} />
          Stanice
        </Group>
      </Anchor>

      <Group justify="space-between" align="flex-start">
        <Stack gap={4}>
          <Group gap="sm">
            <Title order={2}>{stop.data.name}</Title>
            {stop.data.platformCode && (
              <Badge variant="light" size="lg" tt="none">
                Peron {stop.data.platformCode}
              </Badge>
            )}
            {!stop.data.isActive && (
              <Badge variant="light" color="gray">
                Neaktivna
              </Badge>
            )}
          </Group>
          <Text size="sm" c="dimmed">
            {locationTypeLabel(stop.data.locationType)} · {stop.data.publicCode ?? stop.data.code}
          </Text>
        </Stack>
        {hasBoard && (
          <Group gap="sm">
            <Button
              variant="default"
              leftSection={<IconFileTypePdf size={16} />}
              onClick={pdf.open}
            >
              PDF vozni red
            </Button>
            <Button
              component="a"
              href={`/board/${encodeURIComponent(stop.data.code)}`}
              target="_blank"
              variant="default"
              leftSection={<IconDeviceTv size={16} />}
            >
              Zaslon polazaka
            </Button>
          </Group>
        )}
      </Group>
      {hasBoard && <TimetablePdfDialog stop={stop.data} opened={pdfOpened} onClose={pdf.close} />}

      <Grid gap="md">
        <Grid.Col span={{ base: 12, md: 5 }}>
          <Stack gap="md">
            <StopFacts stop={stop.data} />
            <Paper withBorder p="md">
              <Text fw={600} size="sm" mb="xs">
                Linije
              </Text>
              {routes.isPending ? (
                <Text size="sm" c="dimmed">
                  Učitavanje…
                </Text>
              ) : routes.isError ? (
                <Text size="sm" c="red">
                  Linije se nisu učitale.
                </Text>
              ) : routes.data.length === 0 ? (
                <Text size="sm" c="dimmed">
                  Na stanici ne staje nijedna vožnja.
                </Text>
              ) : (
                <Group gap={6}>
                  {routes.data.map((route) => (
                    <Tooltip key={route.id} label={route.longName ?? route.code} openDelay={300}>
                      <Link to={`/routes/${route.id}`} style={{ textDecoration: 'none' }}>
                        <RouteBadge route={route} />
                      </Link>
                    </Tooltip>
                  ))}
                </Group>
              )}
            </Paper>
          </Stack>
        </Grid.Col>
        <Grid.Col span={{ base: 12, md: 7 }}>
          {hasCoordinates && (
            <Paper withBorder style={{ overflow: 'hidden' }}>
              <StopsMap
                key={stop.data.id}
                h={300}
                extent={{
                  minLatitude: stop.data.latitude! - 0.003,
                  maxLatitude: stop.data.latitude! + 0.003,
                  minLongitude: stop.data.longitude! - 0.005,
                  maxLongitude: stop.data.longitude! + 0.005,
                }}
                selectedId={stop.data.id}
                onSelect={(selected) => void navigate(`/stops/${selected.id}`)}
              />
            </Paper>
          )}
        </Grid.Col>
      </Grid>

      <Stack gap="sm">
        <Group justify="space-between" align="flex-end">
          <Title order={3}>Polasci</Title>
          <DatePickerInput
            aria-label="Dan"
            leftSection={<IconCalendar size={16} />}
            value={date ?? departures.data?.date ?? null}
            onChange={setDate}
            valueFormat="dddd, D. M. YYYY."
            w={240}
          />
        </Group>

        {routes.data && routes.data.length > 1 && (
          <Chip.Group multiple value={routeFilter} onChange={setRouteFilter}>
            <Group gap={6}>
              {routes.data.map((route) => (
                <Chip key={route.id} value={route.id} size="xs">
                  {route.shortName ?? route.code}
                </Chip>
              ))}
            </Group>
          </Chip.Group>
        )}

        {departures.isError ? (
          <QueryError error={departures.error} onRetry={() => void departures.refetch()} />
        ) : !departures.data ? (
          <PageLoader />
        ) : shown.length === 0 ? (
          <Paper withBorder p="xl">
            <Text c="dimmed" ta="center">
              Taj dan nema polazaka{routeFilter.length > 0 && ' odabranih linija'}.
            </Text>
          </Paper>
        ) : (
          <Stack gap={0} style={{ opacity: departures.isPlaceholderData ? 0.6 : 1 }}>
            <Text size="sm" c="dimmed" mb="xs">
              {formatNumber(shown.length)} polazaka
            </Text>
            <Paper withBorder>
              {groupByHour(shown).map(([hour, items], index) => (
                <Group
                  key={hour}
                  gap={0}
                  wrap="nowrap"
                  align="stretch"
                  style={{
                    borderTop: index > 0 ? '1px solid var(--mantine-color-default-border)' : 'none',
                  }}
                >
                  <Text
                    fw={700}
                    w={56}
                    ta="center"
                    py={8}
                    style={{
                      flexShrink: 0,
                      background: 'var(--mantine-color-default-hover)',
                      fontVariantNumeric: 'tabular-nums',
                    }}
                  >
                    {hour}
                  </Text>
                  <Group gap={6} p={6} style={{ flex: 1 }}>
                    {items.map((departure) => (
                      <Tooltip
                        key={`${departure.tripId}-${departure.departureTime}`}
                        label={`→ ${departure.headsign}${departure.platform ? ` · peron ${departure.platform}` : ''}`}
                        openDelay={200}
                      >
                        <Group
                          gap={6}
                          wrap="nowrap"
                          px={6}
                          py={3}
                          style={{
                            border: '1px solid var(--mantine-color-default-border)',
                            borderRadius: 'var(--mantine-radius-sm)',
                          }}
                        >
                          <Text size="sm" fw={600} style={{ fontVariantNumeric: 'tabular-nums' }}>
                            {departure.departureTime.slice(14, 16)}
                          </Text>
                          <RouteBadge
                            size="sm"
                            route={{
                              code: departure.routeName ?? '?',
                              shortName: departure.routeName,
                              color: departure.routeColor,
                              textColor: departure.routeTextColor,
                            }}
                          />
                        </Group>
                      </Tooltip>
                    ))}
                  </Group>
                </Group>
              ))}
            </Paper>
          </Stack>
        )}
      </Stack>
    </Stack>
  )
}

function StopFacts({ stop }: { stop: Stop }) {
  const facts = [
    { label: 'GTFS oznaka', value: stop.code },
    { label: 'Javna oznaka', value: stop.publicCode },
    { label: 'Zona', value: stop.zoneId },
    { label: 'Pristupačnost', value: wheelchairLabels[stop.wheelchairBoarding] },
    {
      label: 'Koordinate',
      value:
        stop.latitude !== null && stop.longitude !== null
          ? `${stop.latitude.toFixed(5)}, ${stop.longitude.toFixed(5)}`
          : null,
    },
    { label: 'Opis', value: stop.description },
  ].filter((fact) => fact.value)

  return (
    <Paper withBorder p="md">
      <SimpleGrid cols={2} spacing="sm" verticalSpacing="xs">
        {facts.map((fact) => (
          <div key={fact.label}>
            <Text size="xs" c="dimmed">
              {fact.label}
            </Text>
            <Text size="sm">{fact.value}</Text>
          </div>
        ))}
      </SimpleGrid>
      {stop.parentStationId && (
        <Anchor
          component={Link}
          to={`/stops/${stop.parentStationId}`}
          size="sm"
          mt="sm"
          display="block"
        >
          Kolodvor kojem pripada
        </Anchor>
      )}
    </Paper>
  )
}
