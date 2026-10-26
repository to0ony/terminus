import {
  ActionIcon,
  Box,
  Button,
  CloseButton,
  Group,
  Loader,
  Paper,
  Stack,
  Text,
  Title,
  UnstyledButton,
} from '@mantine/core'
import {
  IconArrowLeft,
  IconBus,
  IconClick,
  IconCurrentLocation,
  IconExternalLink,
} from '@tabler/icons-react'
import { useMediaQuery } from '@mantine/hooks'
import { createElement, type ReactNode, useMemo, useState } from 'react'
import { RouteBadge } from '@/features/routes/RouteBadge'
import { routeTypeIcon } from '@/features/routes/routeTypes'
import type { MapStop } from '@/features/stops/queries'
import { type LiveNetworkDay, useLiveApi } from './api'
import { browserTimeZone, formatTime } from './clock'
import { ClockPanel } from './ClockPanel'
import { TripTimeline } from './TripTimeline'
import { DepartureStrip } from './DepartureStrip'
import { type Departure, departuresFrom, minutesUntil, sameDirection } from './departures'
import { LiveMap } from './LiveMap'
import classes from './LiveMapPage.module.css'
import type { LiveRouteDay } from './queries'
import { vehicleAt } from './simulation'
import { buildVehicles, type Vehicle } from './tracks'
import { useSimulationClock } from './useSimulationClock'

/**
 * Karta uživo, kao Google karte: klik na stanicu prikazuje polaske do kraja
 * dana (linija, smjer, vrijeme), a klik na polazak trasu tog smjera, vozila
 * koja upravo voze i ostala vremena smjera. Vozila su prema voznom redu
 * (simulacija, ne GPS). Uživo je današnji dan i trenutno vrijeme; u simulaciji
 * se biraju dan, vrijeme i brzina.
 */
export function LiveMapPage() {
  return <LiveMapView standalone={false} />
}

/** Karta uživo preko cijelog prozora, bez izbornika i zaglavlja dashboarda (zasebna kartica). */
export function LiveMapStandalonePage() {
  return <LiveMapView standalone />
}

interface LiveMapViewProps {
  /** Preko cijelog prozora, bez naslova stranice. */
  standalone: boolean
  /** Uz uputu na karti, npr. logo i naziv prijevoznika na javnoj karti. */
  brand?: ReactNode
}

/** Karta s panelima; podatke uzima iz dashboarda ili javnog API-ja (LiveApiContext). */
export function LiveMapView({ standalone, brand }: LiveMapViewProps) {
  const liveApi = useLiveApi()
  const extent = liveApi.useStopsExtent()
  const [stop, setStop] = useState<MapStop | null>(null)
  const [selected, setSelected] = useState<Departure | null>(null)
  const [selectedVehicle, setSelectedVehicle] = useState<string | null>(null)
  const [showAll, setShowAll] = useState(false)
  const [timeZone, setTimeZone] = useState(browserTimeZone)
  const clock = useSimulationClock(timeZone)
  const live = liveApi.useLiveRouteDay(selected?.routeId ?? null, clock.date)
  const day = live.data ?? null
  const network = liveApi.useLiveNetwork(showAll, clock.date)

  // Vrijeme se računa u zoni prijevoznika čim se ona zna.
  const knownZone = day?.timeZone ?? network.data?.timeZone
  if (knownZone && knownZone !== timeZone) {
    setTimeZone(knownZone)
  }

  const routeVehicles = useMemo(() => (day ? buildVehicles(day, () => day.route.color) : []), [day])

  // Sva vozila: vožnje svih linija, svaka u boji svoje linije.
  const networkVehicles = useMemo(() => {
    const data = network.data
    if (!data) {
      return []
    }
    const colors = new Map(data.routes.map((r) => [r.id, r.color]))
    return buildVehicles(data, (trip) => colors.get(trip.routeId) ?? null)
  }, [network.data])

  // Smjer odabranog polaska: vožnje linije s istim smjerom i natpisom.
  const view = useMemo(() => {
    const trip = day?.trips.find((t) => t.id === selected?.tripId)
    if (!day || !trip) {
      return null
    }
    const vehicles = routeVehicles.filter(
      (v) => v.trip.directionId === trip.directionId && v.trip.headsign === trip.headsign,
    )
    const shapeIds = new Set(vehicles.map((v) => v.trip.shapeId))
    const stopIds = new Set(vehicles.flatMap((v) => v.trip.stopTimes.map((st) => st.stopId)))
    const directionDay: LiveRouteDay = {
      ...day,
      trips: vehicles.map((v) => v.trip),
      shapes: day.shapes.filter((s) => shapeIds.has(s.id)),
      stops: day.stops.filter((s) => stopIds.has(s.id)),
    }
    return {
      day: directionDay,
      vehicles,
      key: `${day.route.id}|${trip.directionId}|${trip.headsign}`,
    }
  }, [day, routeVehicles, selected?.tripId])

  // Na karti: sva vozila ili samo vozila odabranog smjera.
  const shownVehicles = showAll ? networkVehicles : (view?.vehicles ?? [])

  // Vozilo izvan odabranog smjera (sva vozila): panel prikazuje njegovu vožnju.
  const otherVehicle =
    showAll && selectedVehicle && !view?.vehicles.some((v) => v.key === selectedVehicle)
      ? networkVehicles.find((v) => v.key === selectedVehicle)
      : undefined

  // Mobitel: panel je donja ploča koja se skuplja, a sat je gore.
  const mobile = useMediaQuery('(max-width: 48em)') ?? false
  const [sheetOpen, setSheetOpen] = useState(true)
  const [following, setFollowing] = useState(false)

  // Odabrano vozilo: karta ga prati, a na mobitelu se ploča spusti da se vidi.
  const selectVehicle = (key: string) => {
    setSelectedVehicle(key)
    setFollowing(true)
    if (mobile) {
      setSheetOpen(false)
    }
  }

  const selectStop = (next: MapStop | null) => {
    setStop(next)
    setSheetOpen(true)
    setFollowing(false)
    setSelected(null)
    setSelectedVehicle(null)
  }

  const selectDeparture = (departure: Departure | null) => {
    setSelected(departure)
    setSheetOpen(true)
    setFollowing(false)
    setSelectedVehicle(null)
  }

  const closeVehicle = () => {
    setSelectedVehicle(null)
    setFollowing(false)
  }

  const collapsed = mobile && !sheetOpen
  const bodyMaxHeight = mobile ? '45dvh' : undefined

  const map = (
    <Box
      pos="relative"
      h={standalone ? '100dvh' : 'calc(100dvh - 150px)'}
      mih={standalone ? undefined : 420}
    >
      {extent.isSuccess && (
        <LiveMap
          h="100%"
          extent={extent.data ?? null}
          selectedStopId={stop?.id ?? null}
          onSelectStop={selectStop}
          day={view?.day ?? null}
          focusKey={view?.key ?? null}
          vehicles={shownVehicles}
          timeRef={clock.timeRef}
          selectedVehicle={selectedVehicle}
          onSelectVehicle={selectVehicle}
          following={following}
          onStopFollowing={() => setFollowing(false)}
          followPadding={
            mobile
              ? { top: clock.mode === 'simulation' ? 150 : 90, bottom: 80, left: 20, right: 20 }
              : { top: 20, bottom: 140, left: 400, right: 20 }
          }
          fitPadding={
            mobile
              ? { top: clock.mode === 'simulation' ? 150 : 90, bottom: 320, left: 30, right: 30 }
              : { top: 60, bottom: 160, left: 400, right: 60 }
          }
          pickerBottom={mobile ? 84 : 10}
        />
      )}

      <Paper
        pos="absolute"
        shadow="md"
        withBorder
        {...(mobile
          ? {
              left: 0,
              right: 0,
              bottom: 0,
              style: { borderRadius: '16px 16px 0 0' },
            }
          : { top: 12, left: 12, w: 380, maw: 'calc(100% - 24px)', radius: 'md' })}
      >
        {mobile && (
          <UnstyledButton
            onClick={() => setSheetOpen(!sheetOpen)}
            w="100%"
            pt={8}
            pb={2}
            aria-label={sheetOpen ? 'Skupi' : 'Proširi'}
            style={{ display: 'flex', justifyContent: 'center' }}
          >
            <Box w={40} h={4} bg="gray.4" style={{ borderRadius: 2 }} />
          </UnstyledButton>
        )}
        {otherVehicle && network.data ? (
          <VehiclePanel
            vehicle={otherVehicle}
            network={network.data}
            time={clock.time}
            onClose={closeVehicle}
            collapsed={collapsed}
            bodyMaxHeight={bodyMaxHeight}
          />
        ) : stop ? (
          <StopPanel
            stop={stop}
            date={clock.date}
            time={clock.time}
            timeZone={timeZone}
            selected={selected}
            onSelect={selectDeparture}
            onClose={() => selectStop(null)}
            view={view}
            loading={selected !== null && live.isPending}
            selectedVehicle={selectedVehicle}
            onSelectVehicle={selectVehicle}
            collapsed={collapsed}
            bodyMaxHeight={bodyMaxHeight}
          />
        ) : (
          <Stack gap="sm" p="md">
            {brand}
            <Group gap="sm" wrap="nowrap">
              <IconClick size={20} />
              <div>
                <Text size="sm">
                  Klikni stanicu na karti za polaske s nje
                  {showAll ? ' ili vozilo za njegovu vožnju' : ''}.
                </Text>
                <Text size="xs" c="dimmed">
                  Vozila su prema voznom redu, ne prema GPS-u.
                </Text>
              </div>
            </Group>
          </Stack>
        )}
      </Paper>

      <Paper
        pos="absolute"
        shadow="md"
        radius="md"
        withBorder
        p={mobile ? 'xs' : 'sm'}
        {...(mobile
          ? { top: 8, left: 8, right: 52 }
          : {
              bottom: 32,
              left: '50%',
              w: 560,
              maw: 'calc(100% - 24px)',
              style: { transform: 'translateX(-50%)' },
            })}
      >
        <ClockPanel
          clock={clock}
          simulation={!liveApi.isPublic}
          showAll={showAll}
          onShowAllChange={(next) => {
            setShowAll(next)
            if (!next && otherVehicle) {
              closeVehicle()
            }
          }}
          loadingAll={showAll && network.isPending}
        />
      </Paper>

      {selectedVehicle && shownVehicles.some((v) => v.key === selectedVehicle) && (
        <Button
          pos="absolute"
          radius="xl"
          size="xs"
          variant={following ? 'filled' : 'white'}
          leftSection={<IconCurrentLocation size={14} />}
          onClick={() => setFollowing(!following)}
          style={{
            left: '50%',
            transform: 'translateX(-50%)',
            boxShadow: 'var(--mantine-shadow-md)',
            ...(mobile ? { bottom: 76 } : { top: 12 }),
          }}
        >
          {following ? 'Prati se vozilo · zaustavi' : 'Prati vozilo'}
        </Button>
      )}
    </Box>
  )

  if (standalone) {
    return map
  }

  return (
    <Stack gap="sm">
      <Group justify="space-between">
        <Title order={2}>Karta uživo</Title>
        <Group gap="md">
          <Text size="sm" c="dimmed" visibleFrom="md">
            Vozila su prema voznom redu, ne prema GPS-u.
          </Text>
          <Button
            component="a"
            href="/live/full"
            target="_blank"
            variant="default"
            size="xs"
            leftSection={<IconExternalLink size={14} />}
          >
            Otvori u novoj kartici
          </Button>
        </Group>
      </Group>
      {map}
    </Stack>
  )
}

/** Vožnja vozila odabranog na karti (sva vozila): linija, smjer i sljedeće stanice. */
function VehiclePanel({
  vehicle,
  network,
  time,
  onClose,
  collapsed,
  bodyMaxHeight,
}: {
  vehicle: Vehicle
  network: LiveNetworkDay
  time: number
  onClose: () => void
  collapsed: boolean
  bodyMaxHeight?: string
}) {
  const route = network.routes.find((r) => r.id === vehicle.trip.routeId)
  const stopNames = useMemo(() => new Map(network.stops.map((s) => [s.id, s.name])), [network])
  const state = vehicleAt(vehicle.track, time)
  const stopTimes = vehicle.track.stopTimes
  const current = stopTimes[state?.stopIndex ?? 0]

  return (
    <Stack gap={0}>
      <Group justify="space-between" wrap="nowrap" align="flex-start" p="md" pb="sm">
        <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
          {route && <RouteBadge route={route} />}
          <div style={{ minWidth: 0 }}>
            <Text fw={600} lh={1.3} lineClamp={1}>
              {vehicle.trip.headsign}
            </Text>
            <Text size="xs" c="dimmed">
              Polazak {formatTime(stopTimes[0].departureTime, false)}
              {vehicle.trip.shortName ? ` · ${vehicle.trip.shortName}` : ''}
            </Text>
          </div>
        </Group>
        <CloseButton aria-label="Zatvori" onClick={onClose} />
      </Group>
      <Box
        className={classes.panelBody}
        display={collapsed ? 'none' : undefined}
        mah={bodyMaxHeight}
      >
        <Box px="md" pb="md">
          {state ? (
            <TripTimeline
              vehicle={vehicle}
              stopNames={stopNames}
              color={`#${vehicle.color}`}
              fromStopId={current.stopId}
              fromSeconds={current.departureTime}
              time={time}
            />
          ) : (
            <Text size="sm" c="dimmed">
              Vozilo je završilo vožnju.
            </Text>
          )}
        </Box>
      </Box>
    </Stack>
  )
}

interface StopPanelProps {
  stop: MapStop
  date: string
  time: number
  timeZone: string
  selected: Departure | null
  onSelect: (departure: Departure | null) => void
  onClose: () => void
  view: { day: LiveRouteDay; vehicles: Vehicle[] } | null
  loading: boolean
  selectedVehicle: string | null
  onSelectVehicle: (key: string) => void
  /** Samo naslov (skupljena ploča na mobitelu). */
  collapsed: boolean
  bodyMaxHeight?: string
}

/** Stanica: polasci do kraja dana, a nakon klika na polazak njegov smjer. */
function StopPanel({
  stop,
  date,
  time,
  timeZone,
  selected,
  onSelect,
  onClose,
  view,
  loading,
  selectedVehicle,
  onSelectVehicle,
  collapsed,
  bodyMaxHeight,
}: StopPanelProps) {
  const liveApi = useLiveApi()
  const departures = liveApi.useStopDepartures(stop.id, date)
  const routes = liveApi.useStopRoutes(stop.id)
  const routeTypes = useMemo(
    () => new Map(routes.data?.map((r) => [r.id, r.routeType]) ?? []),
    [routes.data],
  )
  // Polasci od sada; odabrani ostaje i kad prođe, da se vidi njegov smjer.
  const upcoming = useMemo(
    () =>
      departures.data && departures.data.date === date
        ? departuresFrom(departures.data.departures, date, timeZone, 0)
        : [],
    [departures.data, date, timeZone],
  )
  const remaining = upcoming.filter((d) => d.seconds >= Math.floor(time))

  return (
    <Stack gap={0}>
      <Group justify="space-between" wrap="nowrap" align="flex-start" p="md" pb="sm">
        <Group gap="sm" wrap="nowrap">
          {selected ? (
            <ActionIcon
              variant="subtle"
              color="gray"
              onClick={() => onSelect(null)}
              aria-label="Natrag"
            >
              <IconArrowLeft size={18} />
            </ActionIcon>
          ) : null}
          <div>
            <Text fw={600} lh={1.3}>
              {stop.name}
            </Text>
            <Text size="xs" c="dimmed">
              {selected
                ? 'Polasci ovog smjera'
                : `Polasci do kraja dana${remaining.length ? ` · ${remaining.length}` : ''}`}
            </Text>
          </div>
        </Group>
        <CloseButton aria-label="Zatvori" onClick={onClose} />
      </Group>

      <Box
        className={classes.panelBody}
        display={collapsed ? 'none' : undefined}
        mah={bodyMaxHeight}
      >
        <Box px="md" pb="md">
          {departures.isPending ? (
            <Loader size="sm" />
          ) : selected ? (
            <DirectionPanel
              selected={selected}
              departures={upcoming.filter((d) => sameDirection(d, selected))}
              time={time}
              onSelect={onSelect}
              view={view}
              loading={loading}
              selectedVehicle={selectedVehicle}
              onSelectVehicle={onSelectVehicle}
            />
          ) : remaining.length === 0 ? (
            <Text size="sm" c="dimmed">
              Do kraja dana sa stanice nema više polazaka.
            </Text>
          ) : (
            <Stack gap={2}>
              {remaining.map((departure) => (
                <UnstyledButton
                  key={`${departure.tripId}-${departure.seconds}`}
                  onClick={() => onSelect(departure)}
                  className={classes.row}
                  p={6}
                  style={{ borderRadius: 6 }}
                >
                  <Group gap="sm" wrap="nowrap" justify="space-between">
                    <Group gap="sm" wrap="nowrap" style={{ minWidth: 0 }}>
                      <RouteTypeIcon routeType={routeTypes.get(departure.routeId)} />
                      <RouteBadge route={routeOf(departure)} size="sm" />
                      <div style={{ minWidth: 0 }}>
                        <Text size="sm" lineClamp={1}>
                          {departure.headsign}
                        </Text>
                        {departure.platform && (
                          <Text size="xs" c="dimmed">
                            Peron {departure.platform}
                          </Text>
                        )}
                      </div>
                    </Group>
                    <DepartureTime seconds={departure.seconds} time={time} />
                  </Group>
                </UnstyledButton>
              ))}
            </Stack>
          )}
        </Box>
      </Box>
    </Stack>
  )
}

/** Smjer odabranog polaska: ostala vremena gore, pa vozila koja upravo voze. */
function DirectionPanel({
  selected,
  departures,
  time,
  onSelect,
  view,
  loading,
  selectedVehicle,
  onSelectVehicle,
}: {
  selected: Departure
  departures: Departure[]
  time: number
  onSelect: (departure: Departure) => void
  view: { day: LiveRouteDay; vehicles: Vehicle[] } | null
  loading: boolean
  selectedVehicle: string | null
  onSelectVehicle: (key: string) => void
}) {
  const stopNames = useMemo(() => new Map(view?.day.stops.map((s) => [s.id, s.name]) ?? []), [view])
  const selectedTrip = view?.vehicles.find(
    (v) => v.trip.id === selected.tripId && !v.trip.previousDay,
  )
  const running = (view?.vehicles ?? []).flatMap((vehicle) => {
    const state = vehicleAt(vehicle.track, time)
    return state ? [{ vehicle, state }] : []
  })

  return (
    <Stack gap="md">
      <Group gap="sm" wrap="nowrap">
        <RouteBadge route={routeOf(selected)} />
        <Text fw={600} lineClamp={2}>
          {selected.headsign}
        </Text>
      </Group>

      <Stack gap={6}>
        <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
          Vremena polazaka
        </Text>
        <DepartureStrip departures={departures} selected={selected} onSelect={onSelect} />
        {selected.seconds >= time && (
          <Text size="sm">
            Odabrani polazak {formatTime(selected.seconds, false)}
            {selected.seconds - time < 60 * 60
              ? ` · za ${minutesUntil(selected.seconds, time)} min`
              : ''}
          </Text>
        )}
      </Stack>

      {selectedTrip && (
        <Stack gap={6}>
          <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
            Stanice
          </Text>
          <TripTimeline
            vehicle={selectedTrip}
            stopNames={stopNames}
            color={`#${view?.day.route.color ?? '1a73e8'}`}
            fromStopId={selected.stopId}
            fromSeconds={selected.seconds}
            time={time}
          />
        </Stack>
      )}

      <Stack gap={4}>
        <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
          Vozila na trasi ({running.length})
        </Text>
        {loading && <Loader size="sm" />}
        {!loading && running.length === 0 && (
          <Text size="sm" c="dimmed">
            Trenutno nijedno vozilo ovog smjera nije na trasi.
          </Text>
        )}
        {running.map(({ vehicle, state }) => {
          const stopTimes = vehicle.track.stopTimes
          const next = state.atStop ? stopTimes[state.stopIndex] : stopTimes[state.stopIndex + 1]
          return (
            <UnstyledButton
              key={vehicle.key}
              onClick={() => onSelectVehicle(vehicle.key)}
              p={6}
              className={classes.row}
              style={{ borderRadius: 6 }}
              data-active={vehicle.key === selectedVehicle || undefined}
            >
              <Group gap="sm" wrap="nowrap" align="flex-start">
                <BusDot color={`#${view?.day.route.color ?? '1a73e8'}`} size={26} />
                <div style={{ minWidth: 0 }}>
                  <Text size="sm" fw={500} lineClamp={1}>
                    Polazak {formatTime(stopTimes[0].departureTime, false)}
                    {vehicle.trip.id === selected.tripId ? ' · odabrani' : ''}
                  </Text>
                  <Text size="xs" c="dimmed" lineClamp={1}>
                    {state.atStop
                      ? `Na stanici ${stopNames.get(next.stopId) ?? ''}`
                      : `Prema ${stopNames.get(next.stopId) ?? ''} · ${formatTime(next.arrivalTime, false)}`}
                  </Text>
                </div>
              </Group>
            </UnstyledButton>
          )
        })}
      </Stack>
    </Stack>
  )
}

/** Vrijeme polaska; unutar sat vremena i koliko je još minuta. */
function DepartureTime({ seconds, time }: { seconds: number; time: number }) {
  const soon = seconds - time < 60 * 60
  return (
    <div style={{ textAlign: 'right', flexShrink: 0 }}>
      <Text size="sm" fw={600}>
        {formatTime(seconds, false)}
      </Text>
      {soon && (
        <Text size="xs" c={seconds - time < 15 * 60 ? 'green.7' : 'dimmed'}>
          za {minutesUntil(seconds, time)} min
        </Text>
      )}
    </div>
  )
}

/** Vrsta prijevoza ispred oznake linije (autobus, vlak, brod…). */
function RouteTypeIcon({ routeType }: { routeType: number | undefined }) {
  return createElement(routeTypeIcon(routeType ?? 3), {
    size: 18,
    color: 'var(--mantine-color-dimmed)',
    style: { flexShrink: 0 },
  })
}

function BusDot({ color, size }: { color: string; size: number }) {
  return (
    <Box
      bg={color}
      c="white"
      w={size}
      h={size}
      style={{ borderRadius: '50%', display: 'grid', placeItems: 'center', flexShrink: 0 }}
    >
      <IconBus size={Math.round(size * 0.58)} />
    </Box>
  )
}

function routeOf(departure: Departure) {
  return {
    code: departure.routeName ?? '',
    shortName: departure.routeName,
    color: departure.routeColor,
    textColor: departure.routeTextColor,
  }
}
