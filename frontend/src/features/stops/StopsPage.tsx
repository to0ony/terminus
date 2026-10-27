import {
  Alert,
  Button,
  Center,
  Grid,
  Group,
  Paper,
  SegmentedControl,
  Select,
  Stack,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useDebouncedCallback, useLocalStorage } from '@mantine/hooks'
import {
  IconLayoutColumns,
  IconMap,
  IconMapPinPlus,
  IconSearch,
  IconTable,
  IconWorldDownload,
  IconX,
} from '@tabler/icons-react'
import { useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { ColumnsMenu } from '@/components/DataTable/ColumnsMenu'
import { DataTable } from '@/components/DataTable/DataTable'
import { TableFooter } from '@/components/DataTable/TableFooter'
import { useColumns } from '@/components/DataTable/useColumns'
import { useTableParams } from '@/components/DataTable/useTableParams'
import { BlockSkeleton, TableSkeleton } from '@/components/Skeletons'
import { QueryError } from '@/components/QueryError'
import { useRoutes } from '@/features/routes/queries'
import { routeLabel } from '@/features/routes/routeLabel'
import { distance } from '@/features/variants/variantMath'
import { formatNumber } from '@/lib/format'
import { locationTypeOptions, wheelchairLabels } from './locationTypes'
import {
  type Stop,
  type StopSortField,
  type StopTableParams,
  useStopTable,
  useStopsExtent,
} from './queries'
import { OsmSuggestions } from './OsmSuggestions'
import { stopColumns } from './stopColumns'
import { type NewStop, StopDialog } from './StopDialog'
import { StopsMap } from './StopsMap'

type View = 'table' | 'split' | 'map'

const views = [
  { value: 'table', label: 'Tablica', icon: IconTable },
  { value: 'split', label: 'Tablica i karta', icon: IconLayoutColumns },
  { value: 'map', label: 'Karta', icon: IconMap },
]

const yesNo = (yes: string, no: string) => [
  { value: 'true', label: yes },
  { value: 'false', label: no },
]

/** Filtri u adresi; „Očisti filtre” briše sve njih. */
const filterKeys = ['q', 'type', 'active', 'access', 'trips', 'coords', 'route']

const boolParam = (value: string | null) => (value ? value === 'true' : undefined)

/**
 * Stanice: tablica (filtri, slaganje, izbor stupaca, stanje u adresi) i karta
 * za dodavanje stanica. Prikaz (tablica, oboje ili karta) pamti preglednik.
 */
export function StopsPage() {
  const navigate = useNavigate()
  const table = useTableParams()
  const searchRef = useRef<HTMLInputElement>(null)
  const [search, setSearch] = useState(table.get('q') ?? '')
  const [view, setView] = useLocalStorage<View>({
    key: 'terminus-stops-view',
    defaultValue: 'table',
    getInitialValueInEffect: false,
  })
  const [hoveredId, setHoveredId] = useState<string | null>(null)
  // Postavljanje novih stanica klikom na kartu; ostaje uključeno za sljedeću.
  const [placing, setPlacing] = useState(false)
  const [osm, setOsm] = useState(false)
  const [create, setCreate] = useState<NewStop | null>(null)
  const [lastCreated, setLastCreated] = useState<Stop | null>(null)
  const columns = useColumns('stops', stopColumns)
  const routes = useRoutes({ PageSize: 100 })

  const placeAt = (position: { latitude: number; longitude: number }) => {
    // Klik blizu upravo dodane stanice je najčešće drugi peron istog naziva.
    const near =
      lastCreated?.latitude != null &&
      lastCreated.longitude != null &&
      distance(
        [lastCreated.latitude, lastCreated.longitude],
        [position.latitude, position.longitude],
      ) < 150
    setCreate({ ...position, name: near ? lastCreated.name : undefined })
  }

  // Dodavanje stanica radi na karti, pa je prikazuje ako je skrivena.
  const showMap = () => {
    if (view === 'table') {
      setView('split')
    }
  }

  const applySearch = useDebouncedCallback(
    (value: string) => table.setFilter('q', value.trim()),
    300,
  )

  const clearFilters = () => {
    setSearch('')
    table.clearFilters(filterKeys)
  }

  const params: StopTableParams = {
    Search: table.get('q') ?? undefined,
    LocationType: table.get('type') ? Number(table.get('type')) : undefined,
    IsActive: boolParam(table.get('active')),
    WheelchairBoarding: table.get('access') ? Number(table.get('access')) : undefined,
    HasTrips: boolParam(table.get('trips')),
    HasCoordinates: boolParam(table.get('coords')),
    RouteId: table.get('route') ?? undefined,
    SortBy: (table.sort.key as StopSortField | null) ?? undefined,
    SortDescending: table.sort.descending || undefined,
    Page: table.page,
    PageSize: table.pageSize,
  }
  const stops = useStopTable(params)
  const extent = useStopsExtent()
  const filtered = filterKeys.some((key) => table.get(key))
  const totalPages = stops.data ? Math.ceil(stops.data.totalCount / table.pageSize) : 0

  const filterSelect = (key: string, label: string, data: { value: string; label: string }[]) => (
    <Select
      placeholder={label}
      aria-label={label}
      data={data}
      value={table.get(key)}
      onChange={(value) => table.setFilter(key, value)}
      clearable
      w={{ base: '100%', xs: 170 }}
    />
  )

  const tablePart = (
    <Stack gap="sm">
      <Group gap="sm" align="flex-end">
        <TextInput
          ref={searchRef}
          placeholder="Naziv ili oznaka"
          aria-label="Pretraži stanice"
          leftSection={<IconSearch size={16} />}
          value={search}
          onChange={(event) => {
            setSearch(event.currentTarget.value)
            applySearch(event.currentTarget.value)
          }}
          w={{ base: '100%', xs: 220 }}
        />
        {filterSelect('type', 'Sve vrste', locationTypeOptions)}
        {filterSelect('active', 'Aktivne i neaktivne', yesNo('Aktivne', 'Neaktivne'))}
        {filterSelect('trips', 'S vožnjama i bez', yesNo('S vožnjama', 'Bez vožnji'))}
        <Select
          placeholder="Sve linije"
          aria-label="Linija"
          data={routes.data?.items.map((r) => ({ value: r.id, label: routeLabel(r) }))}
          value={table.get('route')}
          onChange={(value) => table.setFilter('route', value)}
          clearable
          searchable
          w={{ base: '100%', xs: 200 }}
        />
        {filterSelect(
          'access',
          'Pristupačnost',
          Object.entries(wheelchairLabels).map(([value, label]) => ({ value, label })),
        )}
        {filterSelect('coords', 'S koordinatama i bez', yesNo('S koordinatama', 'Bez koordinata'))}
        <Group gap="xs" ml="auto">
          {filtered && (
            <Button variant="subtle" leftSection={<IconX size={14} />} onClick={clearFilters}>
              Očisti filtre
            </Button>
          )}
          <ColumnsMenu columns={columns} />
        </Group>
      </Group>

      {stops.isPending ? (
        <TableSkeleton rows={10} />
      ) : stops.isError ? (
        <QueryError error={stops.error} onRetry={() => void stops.refetch()} />
      ) : stops.data.items.length === 0 ? (
        <Paper withBorder p="xl">
          <Stack align="center" gap="sm">
            <Text c="dimmed" ta="center">
              {filtered
                ? 'Nijedna stanica ne odgovara filtrima.'
                : 'Još nema stanica. Pronađi svoj grad tražilicom na karti, pa klikni „Nova stanica” ili uvezi GTFS feed.'}
            </Text>
            {filtered && (
              <Button variant="light" onClick={clearFilters}>
                Očisti filtre
              </Button>
            )}
          </Stack>
        </Paper>
      ) : (
        <>
          <DataTable
            rows={stops.data.items}
            columns={columns.visible}
            getRowId={(row) => row.stop.id}
            getRowHref={(row) => `/stops/${row.stop.id}`}
            sort={table.sort}
            onSortChange={table.setSort}
            stale={stops.isPlaceholderData}
            onRowHover={(row) => setHoveredId(row?.stop.id ?? null)}
            maxHeight={view === 'split' ? 'calc(100dvh - 340px)' : undefined}
            onSearchKey={() => searchRef.current?.focus()}
            onPageKey={(step) => {
              const page = table.page + step
              if (page >= 1 && page <= totalPages) {
                table.setPage(page)
              }
            }}
          />
          <TableFooter
            totalCount={stops.data.totalCount}
            page={table.page}
            pageSize={table.pageSize}
            onPage={table.setPage}
            onPageSize={table.setPageSize}
          />
        </>
      )}
    </Stack>
  )

  const mapHeight = view === 'map' ? 'calc(100dvh - 200px)' : 'calc(100dvh - 190px)'
  const mapPart = (
    <Paper withBorder style={{ overflow: 'hidden' }}>
      {extent.isPending ? (
        <BlockSkeleton h={{ base: 400, lg: mapHeight }} />
      ) : (
        <StopsMap
          h={{ base: 400, lg: mapHeight }}
          mih={400}
          extent={extent.data ?? null}
          selectedId={hoveredId ?? lastCreated?.id ?? null}
          onSelect={placing ? undefined : (stop) => void navigate(`/stops/${stop.id}`)}
          onMapClick={placing ? placeAt : undefined}
          searchable
          panel={
            osm ? (map) => <OsmSuggestions map={map} onClose={() => setOsm(false)} /> : undefined
          }
        />
      )}
    </Paper>
  )

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-end">
        <Group gap="sm" align="baseline">
          <Title order={2}>Stanice</Title>
          {stops.data && (
            <Text c="dimmed" size="sm">
              {formatNumber(stops.data.totalCount)} stanica
            </Text>
          )}
        </Group>
        <Group gap="sm">
          <SegmentedControl
            value={view}
            onChange={(value) => setView(value as View)}
            aria-label="Prikaz"
            data={views.map(({ value, label, icon: Icon }) => ({
              value,
              label: (
                <Center style={{ gap: 6 }} aria-label={label}>
                  <Icon size={16} />
                  <Text size="sm" visibleFrom="sm" span>
                    {label}
                  </Text>
                </Center>
              ),
            }))}
          />
          <Button
            leftSection={<IconMapPinPlus size={16} />}
            variant={placing ? 'filled' : 'light'}
            onClick={() => {
              setPlacing(!placing)
              setOsm(false)
              showMap()
            }}
          >
            {placing ? 'Završi dodavanje' : 'Nova stanica'}
          </Button>
          <Button
            leftSection={<IconWorldDownload size={16} />}
            variant={osm ? 'filled' : 'light'}
            onClick={() => {
              setOsm(!osm)
              setPlacing(false)
              showMap()
            }}
          >
            Iz OpenStreetMapa
          </Button>
        </Group>
      </Group>
      {placing && (
        <Alert variant="light" color="blue" p="xs">
          <Text size="sm">
            Klikni na karti mjesto nove stanice; dodavanje ostaje uključeno za sljedeću. Klik blizu
            upravo dodane predlaže isti naziv, za drugi peron. Grad ili ulicu nađi tražilicom na
            karti.
          </Text>
        </Alert>
      )}
      <StopDialog
        create={create}
        opened={create !== null}
        onClose={() => setCreate(null)}
        onSaved={setLastCreated}
      />

      {view === 'table' && tablePart}
      {view === 'map' && mapPart}
      {view === 'split' && (
        <Grid gap="md">
          <Grid.Col span={{ base: 12, lg: 7 }}>{tablePart}</Grid.Col>
          <Grid.Col span={{ base: 12, lg: 5 }}>{mapPart}</Grid.Col>
        </Grid>
      )}
    </Stack>
  )
}
