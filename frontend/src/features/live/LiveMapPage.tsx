import {
  ActionIcon,
  Badge,
  Box,
  CloseButton,
  Group,
  Loader,
  Paper,
  ScrollArea,
  SegmentedControl,
  Slider,
  Stack,
  Text,
  Title,
  Tooltip,
  UnstyledButton,
} from '@mantine/core'
import { DatePickerInput } from '@mantine/dates'
import {
  IconArrowLeft,
  IconBus,
  IconCalendar,
  IconClick,
  IconPlayerPauseFilled,
  IconPlayerPlayFilled,
} from '@tabler/icons-react'
import { useMemo, useState } from 'react'
import { RouteBadge } from '@/features/routes/RouteBadge'
import { type MapStop, useStopRoutes, useStopsExtent } from '@/features/stops/queries'
import { browserTimeZone, day as daySeconds, formatTime } from './clock'
import { LiveMap } from './LiveMap'
import classes from './LiveMapPage.module.css'
import { type LiveRouteDay, useLiveRouteDay } from './queries'
import { vehicleAt } from './simulation'
import { buildVehicles, type Vehicle } from './tracks'
import { type SimulationClock, useSimulationClock } from './useSimulationClock'

/** Brzine simulacije: koliko puta brže od stvarnog vremena. */
const speeds = [1, 5, 10, 30, 60, 120, 300, 600]

/**
 * Karta uživo: klik na stanicu pa na liniju prikazuje trasu linije i vozila
 * koja upravo voze, prema voznom redu (simulacija, ne GPS). Uživo je današnji
 * dan i trenutno vrijeme; u simulaciji se biraju dan, vrijeme i brzina.
 */
export function LiveMapPage() {
  const extent = useStopsExtent()
  const [stop, setStop] = useState<MapStop | null>(null)
  const [routeId, setRouteId] = useState<string | null>(null)
  const [selectedVehicle, setSelectedVehicle] = useState<string | null>(null)
  const [timeZone, setTimeZone] = useState(browserTimeZone)
  const clock = useSimulationClock(timeZone)
  const live = useLiveRouteDay(routeId, clock.date)
  const day = live.data ?? null

  // Vrijeme se računa u zoni prijevoznika čim se ona zna.
  if (day && day.timeZone !== timeZone) {
    setTimeZone(day.timeZone)
  }

  const vehicles = useMemo(() => (day ? buildVehicles(day) : []), [day])

  const selectRoute = (id: string | null) => {
    setRouteId(id)
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
            onSelectStop={(next) => setStop(next)}
            day={day}
            vehicles={vehicles}
            timeRef={clock.timeRef}
            selectedVehicle={selectedVehicle}
            onSelectVehicle={setSelectedVehicle}
          />
        )}

        <Paper
          pos="absolute"
          top={12}
          left={12}
          w={340}
          maw="calc(100% - 24px)"
          shadow="md"
          radius="md"
          withBorder
        >
          <ScrollArea.Autosize mah="calc(100dvh - 380px)">
            <Box p="md">
              {routeId && (day || live.isPending) ? (
                <RoutePanel
                  day={day}
                  vehicles={vehicles}
                  time={clock.time}
                  stop={stop}
                  selectedVehicle={selectedVehicle}
                  onSelectVehicle={setSelectedVehicle}
                  onBack={() => selectRoute(null)}
                />
              ) : stop ? (
                <StopPanel stop={stop} onSelectRoute={selectRoute} onClose={() => setStop(null)} />
              ) : (
                <Group gap="sm" wrap="nowrap">
                  <IconClick size={20} />
                  <Text size="sm">Klikni stanicu na karti, pa liniju koja na njoj staje.</Text>
                </Group>
              )}
            </Box>
          </ScrollArea.Autosize>
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

function StopPanel({
  stop,
  onSelectRoute,
  onClose,
}: {
  stop: MapStop
  onSelectRoute: (routeId: string) => void
  onClose: () => void
}) {
  const routes = useStopRoutes(stop.id)

  return (
    <Stack gap="sm">
      <Group justify="space-between" wrap="nowrap" align="flex-start">
        <Group gap="sm" wrap="nowrap">
          <Box
            bg="#1a73e8"
            c="white"
            w={32}
            h={32}
            style={{ borderRadius: '50%', display: 'grid', placeItems: 'center', flexShrink: 0 }}
          >
            <IconBus size={18} />
          </Box>
          <div>
            <Text fw={600} lh={1.3}>
              {stop.name}
            </Text>
            <Text size="xs" c="dimmed">
              Autobusna stanica{stop.platformCode ? ` · peron ${stop.platformCode}` : ''}
            </Text>
          </div>
        </Group>
        <CloseButton aria-label="Zatvori" onClick={onClose} />
      </Group>

      {routes.isPending ? (
        <Loader size="sm" />
      ) : routes.data?.length ? (
        <Stack gap={4}>
          <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
            Linije
          </Text>
          {routes.data.map((route) => (
            <UnstyledButton
              key={route.id}
              onClick={() => onSelectRoute(route.id)}
              p={6}
              style={{ borderRadius: 6 }}
              className={classes.row}
            >
              <Group gap="sm" wrap="nowrap">
                <RouteBadge route={route} />
                <Text size="sm" lineClamp={1}>
                  {route.longName ?? ''}
                </Text>
              </Group>
            </UnstyledButton>
          ))}
        </Stack>
      ) : (
        <Text size="sm" c="dimmed">
          Na stanici ne staje nijedna linija.
        </Text>
      )}
    </Stack>
  )
}

function RoutePanel({
  day,
  vehicles,
  time,
  stop,
  selectedVehicle,
  onSelectVehicle,
  onBack,
}: {
  day: LiveRouteDay | null
  vehicles: Vehicle[]
  time: number
  stop: MapStop | null
  selectedVehicle: string | null
  onSelectVehicle: (key: string) => void
  onBack: () => void
}) {
  const stopNames = useMemo(() => new Map(day?.stops.map((s) => [s.id, s.name]) ?? []), [day])

  if (!day) {
    return <Loader size="sm" />
  }

  const running = vehicles.flatMap((vehicle) => {
    const state = vehicleAt(vehicle.track, time)
    return state ? [{ vehicle, state }] : []
  })

  // Sljedeći polasci linije s odabrane stanice.
  const departures = stop
    ? vehicles
        .flatMap((vehicle) =>
          vehicle.track.stopTimes
            .slice(0, -1)
            .filter((st) => st.stopId === stop.id && st.departureTime >= time)
            .map((st) => ({ vehicle, departure: st.departureTime })),
        )
        .sort((a, b) => a.departure - b.departure)
        .slice(0, 5)
    : []

  return (
    <Stack gap="sm">
      <Group gap="xs" wrap="nowrap">
        <ActionIcon variant="subtle" color="gray" onClick={onBack} aria-label="Natrag">
          <IconArrowLeft size={18} />
        </ActionIcon>
        <RouteBadge route={day.route} />
        <Text fw={600} size="sm" lineClamp={2}>
          {day.route.longName ?? ''}
        </Text>
      </Group>

      {stop && departures.length > 0 && (
        <Stack gap={4}>
          <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
            Polasci · {stop.name}
          </Text>
          {departures.map(({ vehicle, departure }) => (
            <Group key={`${vehicle.key}-${departure}`} justify="space-between" wrap="nowrap">
              <Text size="sm" lineClamp={1}>
                {vehicle.trip.headsign}
              </Text>
              <Text size="sm" fw={600} c={departure - time < 15 * 60 ? 'green.7' : undefined}>
                {departure - time < 60 * 60
                  ? `${Math.max(0, Math.round((departure - time) / 60))} min`
                  : formatTime(departure, false)}
              </Text>
            </Group>
          ))}
        </Stack>
      )}

      <Stack gap={4}>
        <Text size="xs" c="dimmed" tt="uppercase" fw={600}>
          U vožnji ({running.length})
        </Text>
        {day.trips.length === 0 && (
          <Text size="sm" c="dimmed">
            Linija taj dan ne vozi.
          </Text>
        )}
        {day.trips.length > 0 && running.length === 0 && (
          <Text size="sm" c="dimmed">
            Trenutno nijedno vozilo nije na liniji.
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
                <Box
                  bg={`#${day.route.color ?? '1a73e8'}`}
                  c="white"
                  w={26}
                  h={26}
                  style={{
                    borderRadius: '50%',
                    display: 'grid',
                    placeItems: 'center',
                    flexShrink: 0,
                  }}
                >
                  <IconBus size={15} />
                </Box>
                <div style={{ minWidth: 0 }}>
                  <Text size="sm" fw={500} lineClamp={1}>
                    {vehicle.trip.shortName ? `${vehicle.trip.shortName} · ` : ''}
                    {vehicle.trip.headsign}
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

function ClockPanel({ clock }: { clock: SimulationClock }) {
  const simulation = clock.mode === 'simulation'
  const speedIndex = Math.max(0, speeds.indexOf(clock.speed))

  return (
    <Stack gap="xs">
      <Group justify="space-between" wrap="nowrap">
        <SegmentedControl
          size="xs"
          value={clock.mode}
          onChange={(value) => clock.setMode(value as SimulationClock['mode'])}
          data={[
            { value: 'live', label: 'Uživo' },
            { value: 'simulation', label: 'Simulacija' },
          ]}
        />
        <Group gap="xs" wrap="nowrap">
          {!simulation && (
            <Badge color="green" variant="dot" tt="none">
              Danas
            </Badge>
          )}
          <Text ff="monospace" fw={700} fz="xl">
            {formatTime(clock.time)}
          </Text>
        </Group>
      </Group>

      {simulation && (
        <>
          <Group gap="sm" wrap="nowrap">
            <DatePickerInput
              size="xs"
              aria-label="Dan"
              leftSection={<IconCalendar size={14} />}
              value={clock.date}
              onChange={(value) => value && clock.setDate(value)}
              valueFormat="DD. MM. YYYY."
              w={150}
            />
            <Tooltip label={clock.playing ? 'Zaustavi' : 'Pokreni'}>
              <ActionIcon
                variant="filled"
                radius="xl"
                onClick={() => {
                  if (!clock.playing && clock.time >= daySeconds - 1) {
                    clock.setTime(0)
                  }
                  clock.setPlaying(!clock.playing)
                }}
                aria-label={clock.playing ? 'Zaustavi' : 'Pokreni'}
              >
                {clock.playing ? (
                  <IconPlayerPauseFilled size={14} />
                ) : (
                  <IconPlayerPlayFilled size={14} />
                )}
              </ActionIcon>
            </Tooltip>
            <Slider
              style={{ flex: 1 }}
              min={0}
              max={daySeconds - 1}
              step={60}
              value={clock.time}
              onChange={clock.setTime}
              label={(value) => formatTime(value, false)}
              aria-label="Vrijeme"
            />
          </Group>
          <Group gap="sm" wrap="nowrap">
            <Text size="xs" c="dimmed" w={60}>
              Brzina
            </Text>
            <Slider
              style={{ flex: 1 }}
              min={0}
              max={speeds.length - 1}
              step={1}
              value={speedIndex}
              onChange={(index) => clock.setSpeed(speeds[index])}
              label={(index) => `${speeds[index]}×`}
              marks={speeds.map((speed, index) => ({ value: index, label: `${speed}×` }))}
              mb="md"
              aria-label="Brzina"
            />
          </Group>
        </>
      )}
    </Stack>
  )
}
