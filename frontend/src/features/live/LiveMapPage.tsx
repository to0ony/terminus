import {
  ActionIcon,
  Badge,
  Box,
  CloseButton,
  Group,
  Loader,
  Paper,
  ScrollArea,
  Stack,
  Text,
  Title,
  UnstyledButton,
} from '@mantine/core'
import { IconArrowLeft, IconBus, IconClick } from '@tabler/icons-react'
import { useMemo, useState } from 'react'
import { RouteBadge } from '@/features/routes/RouteBadge'
import { type MapStop, useStopDepartures, useStopsExtent } from '@/features/stops/queries'
import { browserTimeZone, formatTime } from './clock'
import { ClockPanel } from './ClockPanel'
import { type Departure, departuresFrom, minutesUntil, sameDirection } from './departures'
import { LiveMap } from './LiveMap'
import classes from './LiveMapPage.module.css'
import { type LiveRouteDay, useLiveRouteDay } from './queries'
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
  const extent = useStopsExtent()
  const [stop, setStop] = useState<MapStop | null>(null)
  const [selected, setSelected] = useState<Departure | null>(null)
  const [selectedVehicle, setSelectedVehicle] = useState<string | null>(null)
  const [timeZone, setTimeZone] = useState(browserTimeZone)
  const clock = useSimulationClock(timeZone)
  const live = useLiveRouteDay(selected?.routeId ?? null, clock.date)
  const day = live.data ?? null

  // Vrijeme se računa u zoni prijevoznika čim se ona zna.
  if (day && day.timeZone !== timeZone) {
    setTimeZone(day.timeZone)
  }

  const allVehicles = useMemo(() => (day ? buildVehicles(day) : []), [day])

  // Smjer odabranog polaska: vožnje linije s istim smjerom i natpisom.
  const view = useMemo(() => {
    const trip = day?.trips.find((t) => t.id === selected?.tripId)
    if (!day || !trip) {
      return null
    }
    const vehicles = allVehicles.filter(
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
  }, [day, allVehicles, selected?.tripId])

  const selectStop = (next: MapStop | null) => {
    setStop(next)
    setSelected(null)
    setSelectedVehicle(null)
  }

  const selectDeparture = (departure: Departure | null) => {
    setSelected(departure)
    setSelectedVehicle(null)
  }

  return (
    <Stack gap="sm">
      <Group justify="space-between">
        <Title order={2}>Karta uživo</Title>
        <Text size="sm" c="dimmed">
          Vozila su prema voznom redu, ne prema GPS-u.
        </Text>
      </Group>

      <Box pos="relative" h="calc(100dvh - 150px)" mih={520}>
        {extent.isSuccess && (
          <LiveMap
            h="100%"
            extent={extent.data}
            selectedStopId={stop?.id ?? null}
            onSelectStop={selectStop}
            day={view?.day ?? null}
            focusKey={view?.key ?? null}
            vehicles={view?.vehicles ?? []}
            timeRef={clock.timeRef}
            selectedVehicle={selectedVehicle}
            onSelectVehicle={setSelectedVehicle}
          />
        )}

        <Paper
          pos="absolute"
          top={12}
          left={12}
          w={360}
          maw="calc(100% - 24px)"
          shadow="md"
          radius="md"
          withBorder
        >
          {stop ? (
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
              onSelectVehicle={setSelectedVehicle}
            />
          ) : (
            <Group gap="sm" wrap="nowrap" p="md">
              <IconClick size={20} />
              <Text size="sm">Klikni stanicu na karti za polaske s nje.</Text>
            </Group>
          )}
        </Paper>

        <Paper
          pos="absolute"
          bottom={32}
          left="50%"
          w={560}
          maw="calc(100% - 24px)"
          shadow="md"
          radius="md"
          withBorder
          p="sm"
          style={{ transform: 'translateX(-50%)' }}
        >
          <ClockPanel clock={clock} />
        </Paper>
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
}: StopPanelProps) {
  const departures = useStopDepartures(stop.id, date)
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
          ) : (
            <BusDot color="#1a73e8" size={32} />
          )}
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

      <ScrollArea.Autosize mah="calc(100dvh - 400px)">
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
      </ScrollArea.Autosize>
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
        <Group gap={6}>
          {departures.map((departure) => {
            const active =
              departure.tripId === selected.tripId && departure.seconds === selected.seconds
            const past = departure.seconds < time
            return (
              <Badge
                key={`${departure.tripId}-${departure.seconds}`}
                component="button"
                onClick={() => onSelect(departure)}
                variant={active ? 'filled' : 'light'}
                color={past ? 'gray' : 'blue'}
                tt="none"
                size="lg"
                radius="sm"
                style={{ cursor: 'pointer' }}
              >
                {formatTime(departure.seconds, false)}
              </Badge>
            )
          })}
        </Group>
        {selected.seconds >= time && (
          <Text size="sm">
            Odabrani polazak {formatTime(selected.seconds, false)}
            {selected.seconds - time < 60 * 60
              ? ` · za ${minutesUntil(selected.seconds, time)} min`
              : ''}
          </Text>
        )}
      </Stack>

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
