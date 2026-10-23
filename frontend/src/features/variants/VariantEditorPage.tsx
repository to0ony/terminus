import {
  ActionIcon,
  Alert,
  Anchor,
  Badge,
  Box,
  Button,
  Grid,
  Group,
  Modal,
  NumberInput,
  Paper,
  ScrollArea,
  SegmentedControl,
  Stack,
  Table,
  Text,
  TextInput,
  Title,
  Tooltip,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import {
  IconArrowDown,
  IconArrowLeft,
  IconArrowUp,
  IconDeviceFloppy,
  IconGripVertical,
  IconLock,
  IconRuler2,
  IconSearch,
  IconX,
} from '@tabler/icons-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useBlocker, useNavigate, useParams, useSearchParams } from 'react-router'
import { ApiError, errorMessage } from '@/api/errors'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { useRoute } from '@/features/routes/queries'
import { useRetimeTrips } from '@/features/schedule/queries'
import { RouteBadge } from '@/features/routes/RouteBadge'
import { type MapStop, useStopsExtent, useStopsInArea } from '@/features/stops/queries'
import { StopDialog } from '@/features/stops/StopDialog'
import { type MapOverlay, StopsMap } from '@/features/stops/StopsMap'
import {
  createShape,
  type Variant,
  type VariantRequest,
  useCreateVariant,
  useShape,
  useUpdateVariant,
  useVariant,
} from './queries'
import { type LngLat, networkKind } from './osmNetwork'
import { moveBefore } from './reorder'
import { reverseStops } from './reverse'
import { ShapeLayers } from './ShapeLayers'
import { ShapePanel } from './ShapePanel'
import { StopNumber } from './StopNumber'
import { StopSearch } from './StopSearch'
import { useVariantShape } from './useVariantShape'
import { formatKm } from './variantMath'
import { stopDistances } from './variantShape'
import { distance, duration, suggestTravelTime } from './variantMath'

/** Stanica varijante u editoru; key razlikuje istu stanicu dvaput na kružnoj liniji. */
interface EditorStop {
  key: number
  stopId: string
  name: string
  platformCode: string | null
  latitude: number | null
  longitude: number | null
  /** Sekunde od prethodne stanice; prva stanica 0. */
  travelTime: number
  dwellTime: number
  /** Neaktivnu stanicu vožnje preskaču; bez vrijednosti je aktivna. */
  isActive?: boolean
  /** Povratni smjer: stanica nema para s druge strane, pa je ostala ista. */
  unpaired?: boolean
}

let nextKey = 1

/** Stanica bez ključa, za usporedbu s početnim stanjem. */
const comparable = (s: EditorStop) => [s.stopId, s.travelTime, s.dwellTime]

/** Polazište editora: postojeća varijanta ili povratni smjer koji još nije spremljen. */
type Template = Pick<Variant, 'name' | 'directionId' | 'headsign'> & {
  stops: Omit<EditorStop, 'key'>[]
}

function fromVariant(variant: Template): EditorStop[] {
  return variant.stops.map((s) => ({
    key: nextKey++,
    stopId: s.stopId,
    name: s.name,
    platformCode: s.platformCode,
    latitude: s.latitude,
    longitude: s.longitude,
    travelTime: s.travelTime,
    dwellTime: s.dwellTime,
    isActive: s.isActive,
    unpaired: s.unpaired,
  }))
}

/** GTFS shape_id nove trase: linija, smjer i vrijeme, jedinstven u praksi. */
function newShapeCode(routeCode: string, direction: number) {
  return `${routeCode}-${direction}-${Date.now().toString(36)}`
}

/** Okvir oko stanica s rubom u stupnjevima; null ako nijedna nema koordinate. */
function bounds(stops: Pick<EditorStop, 'latitude' | 'longitude'>[], margin: number) {
  const placed = stops.filter((s) => s.latitude !== null && s.longitude !== null)
  if (placed.length === 0) {
    return null
  }
  const lats = placed.map((s) => s.latitude!)
  const lons = placed.map((s) => s.longitude!)
  return {
    minLatitude: Math.min(...lats) - margin,
    maxLatitude: Math.max(...lats) + margin,
    minLongitude: Math.min(...lons) - margin * 1.5,
    maxLongitude: Math.max(...lons) + margin * 1.5,
  }
}

/**
 * Varijanta linije: stanice se dodaju klikom na kartu, vremena vožnje predlažu iz udaljenosti.
 * `new?from=` je kopija varijante, a `new?reverse=` njezin povratni smjer
 * (s `&trips=1` nakon spremanja vodi na preslikavanje vožnji). Nova prazna
 * varijanta s `new?dir=1` počinje u povratnom smjeru.
 */
export function VariantEditorPage() {
  const { routeId = '', variantId = 'new' } = useParams()
  const [searchParams] = useSearchParams()
  const isNew = variantId === 'new'
  const reverseId = isNew ? searchParams.get('reverse') : null
  const sourceId = isNew ? (reverseId ?? searchParams.get('from')) : variantId
  const newDirection = searchParams.get('dir') === '1' ? 1 : 0
  const route = useRoute(routeId)
  const source = useVariant(sourceId ?? '', sourceId !== null)
  const extent = useStopsExtent()
  // Parovi s druge strane ceste traže se među stanicama oko polaznog smjera (oko 500 m).
  const around = reverseId && source.data ? bounds(source.data.stops, 0.005) : null
  const nearby = useStopsInArea(
    around && {
      MinLatitude: around.minLatitude,
      MaxLatitude: around.maxLatitude,
      MinLongitude: around.minLongitude,
      MaxLongitude: around.maxLongitude,
    },
  )

  const hasArea = around !== null
  // Kopija i izmjena kreću od trase izvorne varijante; povratni smjer se računa iznova.
  const shapeId = reverseId ? null : (source.data?.shapeId ?? null)
  const shape = useShape(shapeId)

  const template = useMemo((): Template | null => {
    if (!source.data) {
      return null
    }
    if (!reverseId) {
      return source.data
    }
    // Polazni smjer bez koordinata nema kamo tražiti parove.
    if (hasArea && !nearby.data) {
      return null
    }
    return {
      // Naziv i natpis smjera slijede iz novih krajnjih stanica.
      name: '',
      headsign: null,
      directionId: source.data.directionId === 1 ? 0 : 1,
      stops: reverseStops(source.data.stops, nearby.data?.stops ?? []),
    }
  }, [source.data, reverseId, hasArea, nearby.data])

  if (route.isError) {
    return <QueryError error={route.error} onRetry={() => void route.refetch()} />
  }
  if (source.isError) {
    return <QueryError error={source.error} onRetry={() => void source.refetch()} />
  }
  if (nearby.isError) {
    return <QueryError error={nearby.error} onRetry={() => void nearby.refetch()} />
  }
  if (shape.isError) {
    return <QueryError error={shape.error} onRetry={() => void shape.refetch()} />
  }
  if (
    route.isPending ||
    (sourceId !== null && template === null) ||
    extent.isPending ||
    (shapeId !== null && shape.isPending)
  ) {
    return <PageLoader />
  }

  return (
    <VariantEditor
      key={variantId + (sourceId ?? '') + (reverseId ? '-reverse' : '') + newDirection}
      routeId={routeId}
      routeType={route.data.routeType}
      routeCode={route.data.code}
      routeBadge={<RouteBadge route={route.data} />}
      // Kopija i povratni smjer postaju nova varijanta; izmjena zadržava Id.
      variant={isNew ? null : (source.data ?? null)}
      template={template}
      reversed={reverseId !== null}
      newDirection={newDirection}
      mirrorFrom={reverseId && searchParams.get('trips') === '1' ? reverseId : null}
      shape={
        shape.data
          ? {
              id: shape.data.id,
              points: shape.data.points.map((p): LngLat => [p.longitude, p.latitude]),
            }
          : null
      }
      extent={extent.data ?? null}
    />
  )
}

interface VariantEditorProps {
  routeId: string
  routeType: number
  routeCode: string
  routeBadge: React.ReactNode
  variant: Variant | null
  template: Template | null
  /** Povratni smjer druge varijante: naziv se ne preuzima. */
  reversed: boolean
  /** Smjer nove varijante bez predloška. */
  newDirection: number
  /** Povratni smjer s vožnjama: nakon spremanja preslikavaju se vožnje ove varijante. */
  mirrorFrom: string | null
  /** Spremljena trasa varijante (ili izvorne, za kopiju). */
  shape: { id: string; points: LngLat[] } | null
  extent: {
    minLatitude: number
    minLongitude: number
    maxLatitude: number
    maxLongitude: number
  } | null
}

function VariantEditor({
  routeId,
  routeType,
  routeCode,
  routeBadge,
  variant,
  template,
  reversed,
  newDirection,
  mirrorFrom,
  shape: savedShape,
  extent,
}: VariantEditorProps) {
  const navigate = useNavigate()
  const create = useCreateVariant(routeId)
  const update = useUpdateVariant(routeId, variant?.id ?? '')
  const save = variant ? update : create
  const retime = useRetimeTrips(routeId)
  const [askRetime, setAskRetime] = useState(false)

  const initial = useMemo(
    () => ({
      name: !template || reversed ? '' : variant ? template.name : `${template.name} (kopija)`,
      direction: template?.directionId ?? newDirection,
      headsign: template?.headsign ?? '',
      stops: template ? fromVariant(template) : [],
    }),
    [template, variant, reversed, newDirection],
  )
  const [name, setName] = useState(initial.name)
  const [direction, setDirection] = useState<number>(initial.direction)
  const [headsign, setHeadsign] = useState(initial.headsign)
  const [stops, setStops] = useState<EditorStop[]>(initial.stops)
  const [selected, setSelected] = useState<number | null>(null)
  const [newStopAt, setNewStopAt] = useState<{ latitude: number; longitude: number } | null>(null)
  const [searching, setSearching] = useState(false)
  const [focus, setFocus] = useState<{ latitude: number; longitude: number } | null>(null)
  const focusOn = (stop: { latitude: number | null; longitude: number | null }) => {
    if (stop.latitude !== null && stop.longitude !== null) {
      setFocus({ latitude: stop.latitude, longitude: stop.longitude })
    }
  }
  // Povlačenje retka: ručka "naoruža" redak, a crta pokazuje mjesto ispred kojeg pada.
  const [armed, setArmed] = useState<number | null>(null)
  const [dragFrom, setDragFrom] = useState<number | null>(null)
  const [dropBefore, setDropBefore] = useState<number | null>(null)
  const endDrag = () => {
    setArmed(null)
    setDragFrom(null)
    setDropBefore(null)
  }
  const [mode, setMode] = useState<'stops' | 'shape'>('stops')
  const [segment, setSegment] = useState<number | null>(null)
  const [savingShape, setSavingShape] = useState(false)
  const [shapeError, setShapeError] = useState<string | null>(null)
  const kind = networkKind(routeType)
  const shape = useVariantShape({
    stops,
    kind,
    initial: savedShape ? { points: savedShape.points, stops: initial.stops } : null,
  })

  // Vožnje koriste redoslijed stanica, pa se mijenjaju samo vremena.
  const locked = (variant?.tripCount ?? 0) > 0
  const autoName = stops.length > 1 ? `${stops[0].name} → ${stops.at(-1)!.name}` : ''
  const dirty =
    name !== initial.name ||
    direction !== initial.direction ||
    headsign !== initial.headsign ||
    shape.changed ||
    JSON.stringify(stops.map(comparable)) !== JSON.stringify(initial.stops.map(comparable))

  /** Prijedlog vremena od prethodne stanice (ili 0 za prvu). */
  const suggestFrom = (
    previous: EditorStop | undefined,
    next: Pick<EditorStop, 'latitude' | 'longitude'>,
  ) =>
    previous &&
    previous.latitude !== null &&
    previous.longitude !== null &&
    next.latitude !== null &&
    next.longitude !== null
      ? suggestTravelTime(
          distance([previous.latitude, previous.longitude], [next.latitude, next.longitude]),
          routeType,
        )
      : previous
        ? 120
        : 0

  const append = (stop: MapStop) => {
    if (locked) {
      notifications.show({
        color: 'yellow',
        message: 'Varijantu koriste vožnje, pa joj se stanice ne mogu mijenjati. Napravi kopiju.',
      })
      return
    }
    if (stops.at(-1)?.stopId === stop.id) {
      return
    }
    const added: EditorStop = {
      key: nextKey++,
      stopId: stop.id,
      name: stop.name,
      platformCode: stop.platformCode,
      latitude: stop.latitude,
      longitude: stop.longitude,
      travelTime: 0,
      dwellTime: 0,
      isActive: stop.isActive,
    }
    added.travelTime = suggestFrom(stops.at(-1), added)
    setStops([...stops, added])
    setSelected(added.key)
    focusOn(added)
  }

  // Nakon premještanja ili brisanja prva stanica ima 0, ostale zadržavaju vrijeme.
  const normalize = (list: EditorStop[]) =>
    list.map((s, i) =>
      i === 0
        ? { ...s, travelTime: 0 }
        : s.travelTime === 0
          ? { ...s, travelTime: suggestFrom(list[i - 1], s) }
          : s,
    )

  const move = (index: number, delta: number) => {
    const list = [...stops]
    const [item] = list.splice(index, 1)
    list.splice(index + delta, 0, item)
    setStops(normalize(list))
  }

  const remove = (index: number) => setStops(normalize(stops.filter((_, i) => i !== index)))

  const patch = (index: number, change: Partial<EditorStop>) =>
    setStops(stops.map((s, i) => (i === index ? { ...s, ...change } : s)))

  const suggestAll = () =>
    setStops(
      stops.map((s, i) => ({ ...s, travelTime: i === 0 ? 0 : suggestFrom(stops[i - 1], s) })),
    )

  const overlay: MapOverlay = useMemo(() => {
    const placed = stops.filter((s) => s.latitude !== null && s.longitude !== null)
    return {
      // Kad se uređuje trasa, ravna crta između stanica bi je prekrila.
      line: mode === 'shape' ? [] : placed.map((s) => [s.longitude!, s.latitude!]),
      points: stops.flatMap((s, i) =>
        s.latitude !== null && s.longitude !== null
          ? [{ position: [s.longitude, s.latitude] as [number, number], label: String(i + 1) }]
          : [],
      ),
    }
  }, [stops, mode])

  // Karta se otvara na stanicama varijante, a nova varijanta na svim stanicama.
  // Kilometri kao u registriranom voznom redu: po trasi, a bez nje zračnom crtom.
  const distances = useMemo(
    () =>
      stopDistances(
        stops.map((s) =>
          s.latitude !== null && s.longitude !== null ? [s.longitude, s.latitude] : null,
        ),
        shape.active ? shape.segments : [],
      ),
    [stops, shape.active, shape.segments],
  )

  const [mapExtent] = useState(() => bounds(initial.stops, 0.002) ?? extent)
  const unpaired = stops.filter((s) => s.unpaired).length

  // Nespremljene izmjene: upozorenje pri odlasku; spremanje ga preskače.
  const leaving = useRef(false)
  const blocker = useBlocker(
    ({ currentLocation, nextLocation }) =>
      dirty && !leaving.current && currentLocation.pathname !== nextLocation.pathname,
  )
  useEffect(() => {
    if (!dirty) {
      return
    }
    const onBeforeUnload = (event: BeforeUnloadEvent) => event.preventDefault()
    window.addEventListener('beforeunload', onBeforeUnload)
    return () => window.removeEventListener('beforeunload', onBeforeUnload)
  }, [dirty])

  /** Trasa za spremanje: nova kad se promijenila, inače postojeća (i kod kopije). */
  const saveShape = async (): Promise<string | null> => {
    if (!shape.active) {
      return shape.changed ? null : (savedShape?.id ?? null)
    }
    const result = await shape.finish()
    if (!result) {
      throw new Error('Trasa nije izračunata. Pokušaj ponovno ili je ukloni.')
    }
    if (!result.changed && savedShape) {
      return savedShape.id
    }
    const created = await createShape(newShapeCode(routeCode, direction), result.points)
    return created.id
  }

  // Promijenjena vremena vožnje varijante s vožnjama mogu se presnimiti u sve vožnje.
  const times = (list: Omit<EditorStop, 'key'>[]) =>
    JSON.stringify(list.map((s) => [s.travelTime, s.dwellTime]))
  const timesChanged = locked && times(stops) !== times(initial.stops)

  const submit = async (retimeTrips: boolean) => {
    setAskRetime(false)
    setShapeError(null)
    setSavingShape(true)
    let shapeId: string | null
    try {
      shapeId = await saveShape()
    } catch (e) {
      setShapeError(errorMessage(e))
      return
    } finally {
      setSavingShape(false)
    }
    const body: VariantRequest = {
      name: name.trim() || autoName,
      directionId: direction,
      headsign: headsign.trim() || null,
      shapeId,
      stops: stops.map((s) => ({
        stopId: s.stopId,
        travelTime: s.travelTime,
        dwellTime: s.dwellTime,
      })),
    }
    save.mutate(body, {
      onSuccess: async (saved) => {
        leaving.current = true
        notifications.show({ color: 'green', message: 'Varijanta je spremljena.' })
        if (retimeTrips) {
          try {
            const result = await retime.mutateAsync(saved)
            notifications.show({
              color: result.skipped > 0 ? 'yellow' : 'green',
              message:
                `Presnimljena vremena u ${result.changed} vožnji.` +
                (result.skipped > 0
                  ? ` ${result.skipped} vožnji je preskočeno (druge stanice ili kraj iza 48:00).`
                  : ''),
            })
          } catch (e) {
            notifications.show({
              color: 'red',
              title: 'Vožnje nisu presnimljene',
              message: `Varijanta je spremljena, ali vremena vožnji nisu: ${errorMessage(e)}`,
            })
          }
        }
        void navigate(
          mirrorFrom
            ? `/routes/${routeId}/schedule?dir=${direction}&mirror=${saved.id}&from=${mirrorFrom}`
            : `/routes/${routeId}`,
        )
      },
    })
  }

  const error = save.error instanceof ApiError ? save.error : null
  const messages = [
    ...(shapeError ? [shapeError] : []),
    ...(error
      ? Object.values(error.fieldErrors).flat()
      : save.isError
        ? [errorMessage(save.error)]
        : []),
  ]

  const openShape = () => {
    setMode('shape')
    // Prvi ulazak bez trase je izračunava; nakon promjene stanica računaju se nove dionice.
    if (shape.available && !shape.computing && (!shape.active || shape.pending > 0)) {
      void shape.compute()
    }
  }

  return (
    <Stack gap="md">
      <Anchor component={Link} to={`/routes/${routeId}`} size="sm">
        <Group gap={4}>
          <IconArrowLeft size={14} />
          Linija
        </Group>
      </Anchor>
      <Group justify="space-between" align="flex-end">
        <Group gap="sm">
          {routeBadge}
          <Title order={2}>{variant ? 'Uredi varijantu' : 'Nova varijanta'}</Title>
        </Group>
        <Button
          leftSection={<IconDeviceFloppy size={16} />}
          onClick={() => (timesChanged ? setAskRetime(true) : void submit(false))}
          loading={save.isPending || savingShape || retime.isPending}
          disabled={stops.length < 2 || (!dirty && variant !== null) || shape.computing}
        >
          Spremi
        </Button>
      </Group>

      {messages.length > 0 && (
        <Alert color="red" variant="light" title="Varijanta nije spremljena">
          {messages.map((m) => (
            <Text key={m} size="sm">
              {m}
            </Text>
          ))}
        </Alert>
      )}

      <Grid gap="md">
        <Grid.Col span={{ base: 12, lg: 5 }}>
          <Stack gap="sm">
            <Paper withBorder p="md">
              <Stack gap="sm">
                <TextInput
                  label="Naziv"
                  placeholder={autoName || 'npr. Črnomerec → Sopot'}
                  description={name ? undefined : 'Prazno: prva i zadnja stanica'}
                  value={name}
                  onChange={(event) => setName(event.currentTarget.value)}
                />
                <Group grow align="flex-start">
                  <Stack gap={4}>
                    <Text size="sm" fw={500}>
                      Smjer
                    </Text>
                    <SegmentedControl
                      data={[
                        { value: '0', label: 'Polazni' },
                        { value: '1', label: 'Povratni' },
                      ]}
                      value={String(direction)}
                      onChange={(value) => setDirection(Number(value))}
                    />
                  </Stack>
                  <TextInput
                    label="Natpis smjera"
                    placeholder={stops.at(-1)?.name ?? 'Zadnja stanica'}
                    value={headsign}
                    onChange={(event) => setHeadsign(event.currentTarget.value)}
                  />
                </Group>
              </Stack>
            </Paper>

            <SegmentedControl
              fullWidth
              data={[
                { value: 'stops', label: `Stanice (${stops.length})` },
                { value: 'shape', label: shape.active ? 'Trasa' : 'Trasa (nema)' },
              ]}
              value={mode}
              onChange={(value) => (value === 'shape' ? openShape() : setMode('stops'))}
            />

            {mode === 'shape' && (
              <ShapePanel
                shape={shape}
                kind={kind}
                names={stops.map((s) => s.name)}
                selected={segment}
                onSelect={setSegment}
              />
            )}

            <Paper withBorder p="md" display={mode === 'stops' ? undefined : 'none'}>
              <Stack gap="sm">
                <Group justify="space-between">
                  <div>
                    <Title order={4}>Stanice</Title>
                    <Text size="sm" c="dimmed">
                      {stops.length} stanica · {Math.round(duration({ stops }) / 60)} min
                    </Text>
                  </div>
                  <Group gap="xs">
                    {!locked && (
                      <Tooltip label="Traži stanicu i dodaj je na kraj">
                        <ActionIcon
                          variant={searching ? 'light' : 'default'}
                          size="input-xs"
                          onClick={() => setSearching(!searching)}
                          aria-label="Traži stanicu"
                          aria-pressed={searching}
                        >
                          <IconSearch size={14} />
                        </ActionIcon>
                      </Tooltip>
                    )}
                    <Tooltip label="Vremena vožnje iz zračne udaljenosti i vrste prijevoza">
                      <Button
                        size="xs"
                        variant="default"
                        leftSection={<IconRuler2 size={14} />}
                        onClick={suggestAll}
                        disabled={stops.length < 2}
                      >
                        Predloži vremena
                      </Button>
                    </Tooltip>
                  </Group>
                </Group>
                {searching && !locked && (
                  <StopSearch onPick={append} onClose={() => setSearching(false)} />
                )}
                {locked && (
                  <Alert variant="light" color="yellow" icon={<IconLock size={16} />} p="xs">
                    <Text size="sm">
                      Varijantu koristi {variant!.tripCount} vožnji, pa se mijenjaju samo vremena.
                      Za drugi redoslijed stanica napravi kopiju.
                    </Text>
                  </Alert>
                )}
                {unpaired > 0 && (
                  <Alert variant="light" color="yellow" p="xs">
                    <Text size="sm">
                      Za {unpaired} {unpaired === 1 ? 'stanicu' : 'stanica'} nije nađena stanica
                      istog naziva s druge strane ceste, pa je ostala ista kao u polaznom smjeru
                      (označene su). Provjeri ih, posebno okretišta i jednosmjerne ulice.
                    </Text>
                  </Alert>
                )}
                {mirrorFrom && (
                  <Alert variant="light" color="blue" p="xs">
                    <Text size="sm">
                      Nakon spremanja otvara se grafikon s preslikavanjem vožnji: svaki polazak
                      polaznog smjera dobiva povratak, uz okret na terminusu koji se može dotjerati.
                    </Text>
                  </Alert>
                )}
                {stops.length === 0 ? (
                  <Text size="sm" c="dimmed">
                    Klikni stanice na karti redom kojim ih linija obilazi. Klik na prazno mjesto
                    karte dodaje novu stanicu.
                  </Text>
                ) : (
                  <ScrollArea.Autosize mah={560}>
                    <Table verticalSpacing={4} horizontalSpacing="xs">
                      <Table.Thead>
                        <Table.Tr>
                          <Table.Th w={locked ? 32 : 52}>#</Table.Th>
                          <Table.Th>Stanica</Table.Th>
                          <Table.Th w={52} ta="right">
                            <Tooltip label="Od prve stanice, po trasi; ≈ zračnom crtom gdje trase nema">
                              <span>km</span>
                            </Tooltip>
                          </Table.Th>
                          <Table.Th w={84}>Vožnja (min)</Table.Th>
                          <Table.Th w={84}>Stajanje (s)</Table.Th>
                          {!locked && <Table.Th w={84} />}
                        </Table.Tr>
                      </Table.Thead>
                      <Table.Tbody>
                        {stops.map((stop, i) => (
                          <Table.Tr
                            key={stop.key}
                            bg={
                              selected === stop.key
                                ? 'var(--mantine-color-orange-light)'
                                : undefined
                            }
                            onClick={() => setSelected(stop.key)}
                            // Dvoklik na polja za vremena označava broj, a ne pomiče kartu.
                            onDoubleClick={(event) =>
                              !(event.target as HTMLElement).closest('input, button') &&
                              focusOn(stop)
                            }
                            // Redak se vuče samo za ručku, da polja za vremena rade normalno.
                            draggable={!locked && armed === stop.key}
                            onDragStart={(event) => {
                              event.dataTransfer.effectAllowed = 'move'
                              event.dataTransfer.setData('text/plain', String(i))
                              setDragFrom(i)
                            }}
                            onDragOver={(event) => {
                              if (dragFrom === null) {
                                return
                              }
                              event.preventDefault()
                              const box = event.currentTarget.getBoundingClientRect()
                              setDropBefore(event.clientY < box.top + box.height / 2 ? i : i + 1)
                            }}
                            onDrop={(event) => {
                              event.preventDefault()
                              if (dragFrom !== null && dropBefore !== null) {
                                setStops(normalize(moveBefore(stops, dragFrom, dropBefore)))
                              }
                              endDrag()
                            }}
                            onDragEnd={endDrag}
                            style={{
                              opacity: dragFrom === i ? 0.4 : undefined,
                              boxShadow:
                                dropBefore === i
                                  ? 'inset 0 2px 0 #e8590c'
                                  : dropBefore === i + 1 && i === stops.length - 1
                                    ? 'inset 0 -2px 0 #e8590c'
                                    : undefined,
                            }}
                          >
                            <Table.Td>
                              <Group gap={4} wrap="nowrap">
                                {!locked && (
                                  <Box
                                    component="span"
                                    c="dimmed"
                                    display="inline-flex"
                                    style={{ cursor: 'grab' }}
                                    aria-label="Povuci za premještanje"
                                    onMouseDown={() => setArmed(stop.key)}
                                    onMouseUp={() => setArmed(null)}
                                  >
                                    <IconGripVertical size={14} />
                                  </Box>
                                )}
                                <StopNumber value={i + 1} />
                              </Group>
                            </Table.Td>
                            <Table.Td>
                              <Text size="sm" lineClamp={1}>
                                {stop.name}
                                {stop.platformCode && (
                                  <Text span c="dimmed" size="xs">
                                    {' '}
                                    · {stop.platformCode}
                                  </Text>
                                )}
                                {stop.unpaired && (
                                  <Badge size="xs" variant="light" color="yellow" ml={6}>
                                    provjeri
                                  </Badge>
                                )}
                                {stop.isActive === false && (
                                  <Tooltip label="Stanica je neaktivna: vožnje je preskaču, a ostale stanice zadržavaju svoja vremena.">
                                    <Badge size="xs" variant="light" color="gray" ml={6}>
                                      preskače se
                                    </Badge>
                                  </Tooltip>
                                )}
                              </Text>
                            </Table.Td>
                            <Table.Td ta="right">
                              <Text
                                size="sm"
                                c={distances[i]?.estimated ? 'dimmed' : undefined}
                                style={{ fontVariantNumeric: 'tabular-nums' }}
                              >
                                {distances[i]?.estimated ? '≈' : ''}
                                {formatKm(distances[i]?.km ?? 0)}
                              </Text>
                            </Table.Td>
                            <Table.Td>
                              {i === 0 ? (
                                <Text size="sm" c="dimmed">
                                  —
                                </Text>
                              ) : (
                                <NumberInput
                                  size="xs"
                                  min={0}
                                  step={0.5}
                                  decimalScale={1}
                                  aria-label={`Vožnja do ${stop.name}`}
                                  value={stop.travelTime / 60}
                                  onChange={(value) =>
                                    patch(i, { travelTime: Math.round(Number(value || 0) * 60) })
                                  }
                                />
                              )}
                            </Table.Td>
                            <Table.Td>
                              <NumberInput
                                size="xs"
                                min={0}
                                step={15}
                                aria-label={`Stajanje na ${stop.name}`}
                                value={stop.dwellTime}
                                onChange={(value) => patch(i, { dwellTime: Number(value || 0) })}
                              />
                            </Table.Td>
                            {!locked && (
                              <Table.Td>
                                <Group gap={0} wrap="nowrap">
                                  <ActionIcon
                                    variant="subtle"
                                    size="sm"
                                    disabled={i === 0}
                                    onClick={() => move(i, -1)}
                                    aria-label="Gore"
                                  >
                                    <IconArrowUp size={14} />
                                  </ActionIcon>
                                  <ActionIcon
                                    variant="subtle"
                                    size="sm"
                                    disabled={i === stops.length - 1}
                                    onClick={() => move(i, 1)}
                                    aria-label="Dolje"
                                  >
                                    <IconArrowDown size={14} />
                                  </ActionIcon>
                                  <ActionIcon
                                    variant="subtle"
                                    size="sm"
                                    color="red"
                                    onClick={() => remove(i)}
                                    aria-label="Ukloni stanicu"
                                  >
                                    <IconX size={14} />
                                  </ActionIcon>
                                </Group>
                              </Table.Td>
                            )}
                          </Table.Tr>
                        ))}
                      </Table.Tbody>
                    </Table>
                  </ScrollArea.Autosize>
                )}
              </Stack>
            </Paper>
          </Stack>
        </Grid.Col>
        <Grid.Col span={{ base: 12, lg: 7 }}>
          <Paper withBorder style={{ overflow: 'hidden' }}>
            <StopsMap
              h={680}
              extent={mapExtent}
              overlay={overlay}
              selectedId={stops.find((s) => s.key === selected)?.stopId ?? null}
              focus={focus}
              onSelect={mode === 'stops' ? append : undefined}
              onMapClick={mode === 'stops' && !locked ? setNewStopAt : undefined}
              panel={
                mode === 'shape'
                  ? (map) => (
                      <ShapeLayers
                        map={map}
                        segments={shape.drawn}
                        selected={segment}
                        onSelect={setSegment}
                        onDragLine={(i, grabbed, dropped) => {
                          setSegment(i)
                          void shape.dragLine(i, grabbed, dropped)
                        }}
                        onMoveVia={(i, via, to) => void shape.moveVia(i, via, to)}
                        onRemoveVia={(i, via) => void shape.removeVia(i, via)}
                      />
                    )
                  : undefined
              }
            />
          </Paper>
          <Text size="xs" c="dimmed" mt={6}>
            {mode === 'stops'
              ? 'Klik na stanicu dodaje je na kraj varijante; klik na prazno mjesto karte dodaje novu stanicu; dvoklik na redak približava kartu stanici.'
              : 'Ljubičasto: po mreži, plavo: ručno, crveno: put nije nađen, crtkano: još nije izračunato.'}
          </Text>
        </Grid.Col>
      </Grid>

      <StopDialog
        create={newStopAt}
        opened={newStopAt !== null}
        onClose={() => setNewStopAt(null)}
        onSaved={append}
      />

      <Modal
        opened={askRetime}
        onClose={() => setAskRetime(false)}
        title="Presnimiti vremena u vožnje?"
        size="md"
      >
        <Stack gap="md">
          <Text size="sm">
            Vremena vožnje su promijenjena, a varijantu koristi {variant?.tripCount ?? 0} vožnji.
            Nova vremena mogu se presnimiti u sve te vožnje, u svim tipovima dana: polazak s prve
            stanice ostaje isti, a sva ostala vremena postaju kao u varijanti, i ona ručno dotjerana
            u grafikonu.
          </Text>
          <Group justify="flex-end" gap="xs">
            <Button variant="default" onClick={() => setAskRetime(false)}>
              Odustani
            </Button>
            <Button variant="default" onClick={() => void submit(false)}>
              Samo varijanta
            </Button>
            <Button onClick={() => void submit(true)}>Presnimi sve vožnje</Button>
          </Group>
        </Stack>
      </Modal>

      <Modal
        opened={blocker.state === 'blocked'}
        onClose={() => blocker.reset?.()}
        title="Nespremljene izmjene"
        size="sm"
      >
        <Stack gap="md">
          <Text size="sm">Varijanta ima nespremljenih izmjena. Ako nastaviš, izgubit će se.</Text>
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
    </Stack>
  )
}
