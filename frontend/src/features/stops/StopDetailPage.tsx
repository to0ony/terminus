import {
  Anchor,
  Badge,
  Button,
  Chip,
  Grid,
  Group,
  Menu,
  Paper,
  SimpleGrid,
  Stack,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { DatePickerInput } from '@mantine/dates'
import { useDisclosure } from '@mantine/hooks'
import { modals } from '@mantine/modals'
import { notifications } from '@mantine/notifications'
import {
  IconArrowLeft,
  IconCalendar,
  IconChevronDown,
  IconDeviceTv,
  IconFileTypePdf,
  IconPencil,
  IconRoute,
  IconTrash,
} from '@tabler/icons-react'
import { Link, useNavigate, useParams, useSearchParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { RouteBadge } from '@/features/routes/RouteBadge'
import { RouteDiagramPdfDialog } from '@/features/routes/RouteDiagramPdfDialog'
import { formatNumber } from '@/lib/format'
import { groupByHour } from './departures'
import { locationTypeLabel, wheelchairLabels } from './locationTypes'
import {
  type Stop,
  useDeleteStop,
  useStop,
  useStopDepartures,
  useStopRoutes,
  useStopRoutesPdf,
} from './queries'
import { StationPlatforms } from './StationPlatforms'
import { StopDialog } from './StopDialog'
import { StopLocation } from './StopLocation'
import { TimetablePdfDialog } from './TimetablePdfDialog'

/** Stanica: podaci, linije koje na njoj staju i polasci za dan (?date=, ?route=). */
export function StopDetailPage() {
  const { stopId = '' } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const date = searchParams.get('date')
  const routeFilter = searchParams.getAll('route')

  const stop = useStop(stopId)
  const routes = useStopRoutes(stopId)
  const departures = useStopDepartures(stopId, date)
  const [pdfOpened, pdf] = useDisclosure(false)
  const [routesPdfOpened, routesPdf] = useDisclosure(false)
  const diagramPdf = useStopRoutesPdf(stopId)
  const [editOpened, edit] = useDisclosure(false)
  const navigate = useNavigate()
  const remove = useDeleteStop()

  const confirmDelete = (target: Stop) =>
    modals.openConfirmModal({
      title: 'Obriši stanicu',
      children: (
        <Text size="sm">
          Stanica „{target.name}” bit će obrisana.
          {target.locationType === 1 && ' Njezini peroni ostaju, ali bez kolodvora.'} Stanica koju
          koriste vožnje ili varijante ne može se obrisati; nju označi neaktivnom.
        </Text>
      ),
      labels: { confirm: 'Obriši', cancel: 'Odustani' },
      confirmProps: { color: 'red' },
      onConfirm: () =>
        remove.mutate(target.id, {
          onSuccess: () => void navigate('/stops'),
          onError: (error) =>
            notifications.show({
              color: 'red',
              title: 'Stanica nije obrisana',
              message: errorMessage(error),
            }),
        }),
    })

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
  // Polasci postoje samo za stajališta i kolodvore (location_type 0 i 1).
  const hasBoard = stop.data.locationType <= 1
  const hasRoutes = (routes.data?.length ?? 0) > 0

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
        <Group gap="sm">
          <Button variant="default" leftSection={<IconPencil size={16} />} onClick={edit.open}>
            Uredi
          </Button>
          <Button
            variant="light"
            color="red"
            leftSection={<IconTrash size={16} />}
            onClick={() => confirmDelete(stop.data)}
          >
            Obriši
          </Button>
          {(hasBoard || hasRoutes) && (
            <Menu position="bottom-end" withinPortal>
              <Menu.Target>
                <Button
                  variant="default"
                  leftSection={<IconFileTypePdf size={16} />}
                  rightSection={<IconChevronDown size={14} />}
                >
                  PDF
                </Button>
              </Menu.Target>
              <Menu.Dropdown>
                <Menu.Item
                  leftSection={<IconFileTypePdf size={14} />}
                  disabled={!hasBoard}
                  onClick={pdf.open}
                >
                  Vozni red stanice
                </Menu.Item>
                <Menu.Item
                  leftSection={<IconRoute size={14} />}
                  disabled={!hasRoutes}
                  onClick={routesPdf.open}
                >
                  Sheme linija sa stanice
                </Menu.Item>
              </Menu.Dropdown>
            </Menu>
          )}
          {hasBoard && (
            <>
              <Button
                component="a"
                href={`/board/${encodeURIComponent(stop.data.code)}`}
                target="_blank"
                variant="default"
                leftSection={<IconDeviceTv size={16} />}
              >
                Zaslon polazaka
              </Button>
            </>
          )}
        </Group>
      </Group>
      <StopDialog stop={stop.data} opened={editOpened} onClose={edit.close} />
      {hasBoard && <TimetablePdfDialog stop={stop.data} opened={pdfOpened} onClose={pdf.close} />}
      <RouteDiagramPdfDialog
        title="PDF sheme linija"
        description={`Sheme svih linija koje staju na stanici ${stop.data.name}, svaka na svojoj stranici. Stanica je na svakoj shemi istaknuta.`}
        pdf={diagramPdf}
        opened={routesPdfOpened}
        onClose={routesPdf.close}
      />

      <Grid gap="md">
        <Grid.Col span={{ base: 12, md: 5 }}>
          <Stack gap="md">
            <StopFacts stop={stop.data} />
            {stop.data.locationType === 1 && <StationPlatforms station={stop.data} />}
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
          <StopLocation stop={stop.data} />
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
