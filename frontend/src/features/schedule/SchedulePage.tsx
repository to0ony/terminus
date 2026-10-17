import {
  ActionIcon,
  Alert,
  Anchor,
  Badge,
  Button,
  Chip,
  Grid,
  Group,
  List,
  Modal,
  Paper,
  SegmentedControl,
  Select,
  Stack,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { notifications } from '@mantine/notifications'
import {
  IconArrowBackUp,
  IconArrowForwardUp,
  IconArrowLeft,
  IconDeviceFloppy,
  IconZoomIn,
  IconZoomOut,
} from '@tabler/icons-react'
import { useEffect, useMemo, useState } from 'react'
import { Link, useBlocker, useParams, useSearchParams } from 'react-router'
import { ApiError, errorMessage } from '@/api/errors'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { useCalendars } from '@/features/calendars/queries'
import { directionOptions } from '@/features/routes/directions'
import { useRoute, useRoutePatterns } from '@/features/routes/queries'
import { RouteBadge } from '@/features/routes/RouteBadge'
import { useSeasons } from '@/features/seasons/queries'
import { dayTypeColor } from '@/features/seasons/seasonDays'
import {
  changes,
  clampView,
  firstDeparture,
  formatTime,
  fromResponse,
  hour,
  rowIndexes,
  rowPositions,
  shiftStop,
  shiftTrip,
  templateTrip,
  tripFrom,
  validateTrip,
  type View,
} from './model'
import { type Schedule, useSaveSchedule, useSchedule } from './queries'
import { CopyDialog, TaktDialog } from './RepeatDialog'
import { ScheduleChart } from './ScheduleChart'
import { TripPanel } from './TripPanel'
import { useScheduleEditor } from './useScheduleEditor'

const noCalendar = '00000000-0000-0000-0000-000000000000'

interface DayTypeOption {
  id: string
  label: string
  color: string
  count: number
}

/**
 * Grafikon voznog reda linije (?dir=0|1&cal=<kalendar>&cmp=<kalendar>):
 * vožnje jednog smjera i tipa dana kao crte kroz stanice i vrijeme, s
 * uređivanjem povlačenjem, kopiranjem i generatorom takta.
 */
export function SchedulePage() {
  const { routeId = '' } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()
  const route = useRoute(routeId)
  const patterns = useRoutePatterns(routeId)
  const calendars = useCalendars({ PageSize: 100 })
  const seasons = useSeasons()

  const directions = directionOptions(patterns.data ?? [])
  const direction = directions.length
    ? Number(
        directions.find((d) => d.value === searchParams.get('dir'))?.value ?? directions[0].value,
      )
    : (patterns.data?.find((p) => p.directionId !== null)?.directionId ?? null)

  // Broj vožnji po kalendaru dolazi uz svaki odgovor; dok tip dana nije
  // odabran, traži se kalendar koji ne postoji, pa odgovor nema vožnji.
  const calendarParam = searchParams.get('cal')
  const schedule = useSchedule(routeId, direction, calendarParam ?? noCalendar, patterns.isSuccess)
  const counts = useMemo(() => schedule.data?.tripCounts ?? {}, [schedule.data])

  const dayTypes = useMemo<DayTypeOption[]>(() => {
    const options: DayTypeOption[] = []
    for (const season of seasons.data ?? []) {
      season.dayTypes.forEach((t, index) =>
        options.push({
          id: t.id,
          label: `${t.name} · ${season.name}`,
          color: dayTypeColor(index),
          count: counts[t.id] ?? 0,
        }),
      )
    }
    // Kalendari izvan sezona, samo oni s vožnjama ove linije.
    for (const calendar of calendars.data?.items ?? []) {
      if (!calendar.seasonId && (counts[calendar.id] ?? 0) > 0) {
        options.push({
          id: calendar.id,
          label: calendar.name,
          color: dayTypeColor(options.length),
          count: counts[calendar.id],
        })
      }
    }
    return options
  }, [seasons.data, calendars.data, counts])

  // Zadano tip dana s najviše vožnji; uvoz kalendare samo s datumima zove
  // "Posebni datumi…", pa kod jednakog broja prednost ima pravi tip dana.
  const busiest = [...dayTypes].sort(
    (a, b) =>
      b.count - a.count ||
      Number(a.label.startsWith('Posebni')) - Number(b.label.startsWith('Posebni')),
  )[0]
  const calendarId = calendarParam ?? busiest?.id ?? null
  const current = dayTypes.find((d) => d.id === calendarId)

  // Bez vožnji u odabranom tipu dana predložak dolazi iz tipa s najviše vožnji.
  const compareParam = searchParams.get('cmp')
  const compareId =
    compareParam === 'none'
      ? null
      : (compareParam ??
        (current && current.count === 0 && busiest && busiest.id !== calendarId
          ? busiest.id
          : null))

  const shown = useSchedule(
    routeId,
    direction,
    calendarId,
    patterns.isSuccess && calendarId !== null,
  )
  const compared = useSchedule(routeId, direction, compareId, compareId !== null)

  const setParam = (key: string, value: string | null) =>
    setSearchParams(
      (params) => {
        if (value === null) {
          params.delete(key)
        } else {
          params.set(key, value)
        }
        return params
      },
      { replace: true },
    )

  if (route.isPending || patterns.isPending) {
    return <PageLoader />
  }
  if (route.isError) {
    return <QueryError error={route.error} onRetry={() => void route.refetch()} />
  }

  return (
    <Stack gap="md">
      <Anchor component={Link} to={`/routes/${routeId}`} size="sm">
        <Group gap={4}>
          <IconArrowLeft size={14} />
          Linija
        </Group>
      </Anchor>

      <Group gap="sm" wrap="nowrap">
        <RouteBadge route={route.data} size="lg" />
        <Title order={2}>Grafikon voznog reda</Title>
      </Group>

      <Paper withBorder p="sm">
        <Stack gap="xs">
          {directions.length > 0 && (
            <SegmentedControl
              value={String(direction)}
              onChange={(value) => setParam('dir', value)}
              data={directions}
              w="fit-content"
            />
          )}
          <Group gap="xs" align="center">
            <Text size="sm" c="dimmed">
              Tip dana:
            </Text>
            <Chip.Group
              value={calendarId ?? ''}
              onChange={(value) => setParam('cal', String(value))}
            >
              <Group gap={6}>
                {dayTypes.map((d) => (
                  <Chip key={d.id} value={d.id} color={d.color} size="sm" variant="light">
                    {d.label} ({d.count})
                  </Chip>
                ))}
              </Group>
            </Chip.Group>
          </Group>
          <Group gap="xs">
            <Text size="sm" c="dimmed">
              Usporedi s:
            </Text>
            <Select
              size="xs"
              w={260}
              data={[
                { value: 'none', label: 'ništa' },
                ...dayTypes
                  .filter((d) => d.id !== calendarId)
                  .map((d) => ({ value: d.id, label: d.label })),
              ]}
              value={compareId ?? 'none'}
              onChange={(value) => setParam('cmp', value)}
              allowDeselect={false}
            />
          </Group>
        </Stack>
      </Paper>

      {dayTypes.length === 0 && seasons.isSuccess && calendars.isSuccess ? (
        <Alert color="yellow" variant="light">
          Nema tipova dana. Napravi sezonu na stranici{' '}
          <Anchor component={Link} to="/calendars" size="sm">
            Kalendari
          </Anchor>
          .
        </Alert>
      ) : shown.isError ? (
        <QueryError error={shown.error} onRetry={() => void shown.refetch()} />
      ) : !shown.data || !current ? (
        <PageLoader />
      ) : (
        <ScheduleEditor
          // Novi podaci (drugi tip dana, smjer ili nakon spremanja) počinju ispočetka.
          key={`${direction}-${calendarId}-${shown.dataUpdatedAt}`}
          routeId={routeId}
          direction={direction}
          dayType={current}
          dayTypes={dayTypes}
          schedule={shown.data}
          ghosts={compared.data?.trips ?? []}
          onReload={() => void shown.refetch()}
        />
      )}
    </Stack>
  )
}

interface ScheduleEditorProps {
  routeId: string
  direction: number | null
  dayType: DayTypeOption
  dayTypes: DayTypeOption[]
  schedule: Schedule
  ghosts: Schedule['trips']
  onReload: () => void
}

function ScheduleEditor({
  routeId,
  direction,
  dayType,
  dayTypes,
  schedule,
  ghosts,
  onReload,
}: ScheduleEditorProps) {
  const original = useMemo(() => schedule.trips.map(fromResponse), [schedule])
  const editor = useScheduleEditor(original)
  const save = useSaveSchedule(routeId)
  const ghostTrips = useMemo(() => ghosts.map(fromResponse), [ghosts])
  const [copyOpened, copy] = useDisclosure(false)
  const [taktOpened, takt] = useDisclosure(false)

  const stops = schedule.stops
  const names = useMemo(() => new Map(stops.map((s) => [s.id, s.name])), [stops])
  const stopName = (id: string) => names.get(id) ?? '?'

  // Vožnje drugih tipova dana (premještene u panelu) ne crtaju se ovdje.
  const visible = editor.trips.filter((t) => t.serviceCalendarId === dayType.id)
  const positions = useMemo(
    () =>
      rowPositions(
        stops.map((s) => s.id),
        [...original, ...ghostTrips],
      ),
    [stops, original, ghostTrips],
  )

  const errors = useMemo(() => {
    const result = new Map<string, string>()
    for (const trip of editor.trips) {
      const error = validateTrip(trip, (id) => names.get(id) ?? '?')
      if (error) {
        result.set(trip.key, error)
      }
    }
    return result
  }, [editor.trips, names])
  const invalid = useMemo(() => new Set(errors.keys()), [errors])

  const pending = useMemo(() => changes(original, editor.trips), [original, editor.trips])
  const dirty = pending.upserts.length + pending.deletes.length > 0

  // Dan voznog reda: od prvog do zadnjeg vremena, zaokruženo na sat.
  const span = useMemo<View>(() => {
    const all = [...original, ...ghostTrips]
    if (all.length === 0) {
      return [4 * hour, 25 * hour]
    }
    const start = Math.min(...all.map((t) => t.stopTimes[0].arrivalTime))
    const end = Math.max(...all.map((t) => t.stopTimes.at(-1)!.departureTime))
    // Sat prije i poslije, da stanu i nove vožnje na rubovima dana.
    return [
      Math.max(0, Math.floor(start / hour) * hour - hour),
      Math.ceil(end / hour) * hour + hour,
    ]
  }, [original, ghostTrips])
  const [viewState, setView] = useState<View | null>(null)
  const view = clampView(viewState ?? span, span)

  const templateSource = visible.length > 0 ? visible : ghostTrips
  const mainTemplate = templateTrip(templateSource)
  const preview = mainTemplate
    ? rowIndexes(
        stops.map((s) => s.id),
        mainTemplate.stopTimes.map((st) => st.stopId),
      ).map((row, i) => ({
        row,
        offset: mainTemplate.stopTimes[i].departureTime - firstDeparture(mainTemplate),
      }))
    : null

  const create = (departures: number[]) => {
    // Bez predloška (tip dana bez vožnji i bez usporedbe) nova vožnja nema vremena vožnje.
    const created = departures.flatMap((t) => {
      const template = templateTrip(templateSource, t)
      return template ? [tripFrom(template, t, dayType.id)] : []
    })
    if (created.length === 0) {
      return
    }
    editor.apply(
      (trips) => [...trips, ...created],
      created.map((t) => t.key),
    )
  }

  const shiftSelected = (delta: number) =>
    editor.apply((trips) =>
      trips.map((t) => (editor.selected.includes(t.key) ? shiftTrip(t, delta) : t)),
    )

  const deleteSelected = () =>
    editor.apply((trips) => trips.filter((t) => !editor.selected.includes(t.key)), [])

  const copySelected = (interval: number, times: number) => {
    const sources = editor.trips.filter((t) => editor.selected.includes(t.key))
    const created = sources.flatMap((source) =>
      Array.from({ length: times }, (_, i) =>
        tripFrom(source, firstDeparture(source) + interval * (i + 1), source.serviceCalendarId),
      ),
    )
    editor.apply(
      (trips) => [...trips, ...created],
      created.map((t) => t.key),
    )
  }

  // Tipkovnica: pomak, brisanje, poništavanje, spremanje.
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      const target = event.target as HTMLElement
      if (target.closest('input, textarea, select, [contenteditable="true"]')) {
        return
      }
      const ctrl = event.ctrlKey || event.metaKey
      if (ctrl && event.key.toLowerCase() === 'z' && !event.shiftKey) {
        editor.undo()
      } else if (
        ctrl &&
        (event.key.toLowerCase() === 'y' || (event.key.toLowerCase() === 'z' && event.shiftKey))
      ) {
        editor.redo()
      } else if (
        editor.selected.length > 0 &&
        (event.key === 'ArrowLeft' || event.key === 'ArrowRight')
      ) {
        const step = (event.shiftKey ? 5 : 1) * 60
        shiftSelected(event.key === 'ArrowLeft' ? -step : step)
      } else if (
        editor.selected.length > 0 &&
        (event.key === 'Delete' || event.key === 'Backspace')
      ) {
        deleteSelected()
      } else if (event.key === 'Escape') {
        editor.select([])
      } else {
        return
      }
      event.preventDefault()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  })

  // Nespremljene izmjene: upozorenje pri odlasku sa stranice ili promjeni tipa dana.
  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) => dirty && currentLocation !== nextLocation,
  )
  useEffect(() => {
    if (!dirty) {
      return
    }
    const onBeforeUnload = (event: BeforeUnloadEvent) => event.preventDefault()
    window.addEventListener('beforeunload', onBeforeUnload)
    return () => window.removeEventListener('beforeunload', onBeforeUnload)
  }, [dirty])

  const submit = () =>
    save.mutate(
      { directionId: direction, upserts: pending.upserts, deletes: pending.deletes },
      {
        onSuccess: () =>
          notifications.show({
            color: 'green',
            message: `Spremljeno: ${pending.upserts.length} novih ili izmijenjenih, ${pending.deletes.length} obrisanih vožnji.`,
          }),
      },
    )

  const saveError = save.error instanceof ApiError ? save.error : null
  const zoom = (factor: number) => {
    const center = (view[0] + view[1]) / 2
    const half = ((view[1] - view[0]) * factor) / 2
    setView(clampView([center - half, center + half], span))
  }

  return (
    <>
      <Group justify="space-between">
        <Group gap="xs">
          <Badge color={dayType.color} variant="light" size="lg" tt="none">
            {dayType.label}: {visible.length} vožnji
          </Badge>
          <Text size="sm" c="dimmed">
            {formatTime(view[0] % (24 * hour))} – {formatTime(view[1] % (24 * hour))}
          </Text>
          <Tooltip label="Približi">
            <ActionIcon variant="default" onClick={() => zoom(0.5)} aria-label="Približi">
              <IconZoomIn size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label="Udalji">
            <ActionIcon variant="default" onClick={() => zoom(2)} aria-label="Udalji">
              <IconZoomOut size={16} />
            </ActionIcon>
          </Tooltip>
          <Button size="xs" variant="default" onClick={() => setView(span)}>
            Cijeli dan
          </Button>
        </Group>
        <Group gap="xs">
          <Tooltip label="Poništi (Ctrl+Z)">
            <ActionIcon
              variant="default"
              onClick={editor.undo}
              disabled={!editor.canUndo}
              aria-label="Poništi"
            >
              <IconArrowBackUp size={16} />
            </ActionIcon>
          </Tooltip>
          <Tooltip label="Ponovi (Ctrl+Y)">
            <ActionIcon
              variant="default"
              onClick={editor.redo}
              disabled={!editor.canRedo}
              aria-label="Ponovi"
            >
              <IconArrowForwardUp size={16} />
            </ActionIcon>
          </Tooltip>
          <Button
            leftSection={<IconDeviceFloppy size={16} />}
            onClick={submit}
            loading={save.isPending}
            disabled={!dirty || invalid.size > 0}
          >
            Spremi{dirty ? ` (${pending.upserts.length + pending.deletes.length})` : ''}
          </Button>
        </Group>
      </Group>

      {invalid.size > 0 && (
        <Alert color="red" variant="light">
          {invalid.size} vožnji ima neispravna vremena (crveno). Ispravi ih prije spremanja.
        </Alert>
      )}
      {save.isError && (
        <Alert color="red" variant="light" title="Spremanje nije uspjelo">
          <Text size="sm">{errorMessage(save.error)}</Text>
          {saveError && Object.values(saveError.fieldErrors).length > 0 && (
            <List size="sm" mt="xs">
              {[...new Set(Object.values(saveError.fieldErrors).flat())]
                .slice(0, 8)
                .map((message) => (
                  <List.Item key={message}>{message}</List.Item>
                ))}
            </List>
          )}
          {saveError?.status === 409 && (
            <Button size="xs" mt="xs" variant="light" onClick={onReload}>
              Učitaj ponovno (izmjene se odbacuju)
            </Button>
          )}
        </Alert>
      )}

      {stops.length === 0 ? (
        <Alert color="yellow" variant="light">
          Linija u ovom smjeru još nema nijednu vožnju, pa nema ni redoslijeda stanica. Prvu vožnju
          moći ćeš dodati kad bude gotov editor linija.
        </Alert>
      ) : (
        <Grid gap="md">
          <Grid.Col span={{ base: 12, lg: 9 }}>
            <Paper withBorder p="xs">
              <ScheduleChart
                stops={stops}
                positions={positions}
                trips={visible}
                ghosts={ghostTrips}
                color={dayType.color}
                selected={editor.selected}
                invalid={invalid}
                view={view}
                span={span}
                onViewChange={setView}
                onSelect={editor.select}
                onShiftTrips={(keys, delta) =>
                  editor.apply((trips) =>
                    trips.map((t) => (keys.includes(t.key) ? shiftTrip(t, delta) : t)),
                  )
                }
                onShiftStop={(key, index, delta, cascade) =>
                  editor.apply((trips) =>
                    trips.map((t) => (t.key === key ? shiftStop(t, index, delta, cascade) : t)),
                  )
                }
                onCreate={mainTemplate ? (departure) => create([departure]) : null}
                preview={preview}
              />
            </Paper>
          </Grid.Col>
          <Grid.Col span={{ base: 12, lg: 3 }}>
            <TripPanel
              trips={visible}
              selected={editor.selected}
              errors={errors}
              stopName={stopName}
              calendars={dayTypes.map((d) => ({ value: d.id, label: d.label }))}
              onChange={(key, update) =>
                editor.apply((trips) => trips.map((t) => (t.key === key ? update(t) : t)))
              }
              onShift={shiftSelected}
              onCopy={copy.open}
              onDelete={deleteSelected}
              onTakt={takt.open}
              canCreate={mainTemplate !== null}
            />
          </Grid.Col>
        </Grid>
      )}

      <CopyDialog
        opened={copyOpened}
        count={editor.selected.length}
        onClose={copy.close}
        onCopy={copySelected}
      />
      <TaktDialog
        opened={taktOpened}
        onClose={takt.close}
        onCreate={create}
        existing={visible.map(firstDeparture)}
      />

      <Modal
        opened={blocker.state === 'blocked'}
        onClose={() => blocker.reset?.()}
        title="Nespremljene izmjene"
        size="sm"
      >
        <Stack gap="md">
          <Text size="sm">
            Grafikon ima {pending.upserts.length + pending.deletes.length} nespremljenih izmjena.
            Ako nastaviš, izgubit će se.
          </Text>
          <Group justify="flex-end">
            <Button variant="default" onClick={() => blocker.reset?.()}>
              Ostani
            </Button>
            <Button color="red" onClick={() => blocker.proceed?.()}>
              Odbaci izmjene
            </Button>
          </Group>
        </Stack>
      </Modal>
    </>
  )
}
