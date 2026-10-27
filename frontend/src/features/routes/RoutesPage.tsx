import { Button, Group, Menu, Paper, Select, Stack, Text, TextInput, Title } from '@mantine/core'
import { useDebouncedCallback, useDisclosure } from '@mantine/hooks'
import { modals } from '@mantine/modals'
import { notifications } from '@mantine/notifications'
import {
  IconBuildingCommunity,
  IconCircleCheck,
  IconCircleOff,
  IconPlus,
  IconSearch,
  IconX,
} from '@tabler/icons-react'
import { useMemo, useRef, useState } from 'react'
import { errorMessage } from '@/api/errors'
import { BulkBar } from '@/components/DataTable/BulkBar'
import { ColumnsMenu } from '@/components/DataTable/ColumnsMenu'
import { DataTable } from '@/components/DataTable/DataTable'
import { TableFooter } from '@/components/DataTable/TableFooter'
import { useColumns } from '@/components/DataTable/useColumns'
import { useTableParams } from '@/components/DataTable/useTableParams'
import { TableSkeleton } from '@/components/Skeletons'
import { QueryError } from '@/components/QueryError'
import { useAgencies } from '@/features/agencies/queries'
import { formatNumber } from '@/lib/format'
import {
  type Route,
  type RouteSortField,
  type RouteTableParams,
  useRouteTable,
  useSetRoutesActive,
  useSetRoutesAgency,
} from './queries'
import { routeColumns } from './routeColumns'
import { RouteDialog } from './RouteDialog'
import { routeLabel } from './routeLabel'
import { routeTypeOptions } from './routeTypes'

const yesNo = (yes: string, no: string) => [
  { value: 'true', label: yes },
  { value: 'false', label: no },
]

/** Filtri u adresi; „Očisti filtre” briše sve njih. */
const filterKeys = ['q', 'agency', 'type', 'active', 'trips']

const boolParam = (value: string | null) => (value ? value === 'true' : undefined)

/** 1 linija, 2–4 linije, 5 linija (i 11–14 linija). */
const routesNoun = (count: number) =>
  [2, 3, 4].includes(count % 10) && ![12, 13, 14].includes(count % 100) ? 'linije' : 'linija'

/**
 * Linije: tablica s filtrima, slaganjem i izborom stupaca (stanje u adresi), a
 * odabrane linije se mogu aktivirati, deaktivirati ili premjestiti prijevozniku.
 */
export function RoutesPage() {
  const table = useTableParams(50)
  const searchRef = useRef<HTMLInputElement>(null)
  const [search, setSearch] = useState(table.get('q') ?? '')
  const [newOpened, newRoute] = useDisclosure(false)
  // Odabrane linije, i s drugih stranica tablice.
  const [selected, setSelected] = useState<Map<string, Route>>(new Map())
  const agencies = useAgencies()
  const showAgency = (agencies.data?.length ?? 0) > 1
  const allColumns = useMemo(() => routeColumns(showAgency), [showAgency])
  const columns = useColumns('routes', allColumns)
  const setActive = useSetRoutesActive()
  const setAgency = useSetRoutesAgency()

  const applySearch = useDebouncedCallback(
    (value: string) => table.setFilter('q', value.trim()),
    300,
  )

  const clearFilters = () => {
    setSearch('')
    table.clearFilters(filterKeys)
  }

  const params: RouteTableParams = {
    Search: table.get('q') ?? undefined,
    AgencyId: table.get('agency') ?? undefined,
    RouteType: table.get('type') ? Number(table.get('type')) : undefined,
    IsActive: boolParam(table.get('active')),
    HasTrips: boolParam(table.get('trips')),
    SortBy: (table.sort.key as RouteSortField | null) ?? undefined,
    SortDescending: table.sort.descending || undefined,
    Page: table.page,
    PageSize: table.pageSize,
  }
  const routes = useRouteTable(params)
  const filtered = filterKeys.some((key) => table.get(key))
  const totalPages = routes.data ? Math.ceil(routes.data.totalCount / table.pageSize) : 0

  const changeSelection = (ids: Set<string>) => {
    const onPage = new Map(routes.data?.items.map((row) => [row.route.id, row.route]))
    setSelected(
      new Map(
        [...ids].flatMap((id) => {
          const route = selected.get(id) ?? onPage.get(id)
          return route ? [[id, route] as const] : []
        }),
      ),
    )
  }

  const done = (message: string) => {
    notifications.show({ color: 'green', message })
    setSelected(new Map())
  }

  const failed = (error: Error) =>
    notifications.show({
      color: 'red',
      title: 'Linije nisu promijenjene',
      message: errorMessage(error),
    })

  const applyActive = (isActive: boolean) =>
    setActive.mutate(
      { ids: [...selected.keys()], isActive },
      {
        onSuccess: ({ changed }) =>
          done(
            changed === 0
              ? `Odabrane linije su već ${isActive ? 'aktivne' : 'neaktivne'}.`
              : `${isActive ? 'Aktivirano' : 'Deaktivirano'}: ${changed} ${routesNoun(changed)}.`,
          ),
        onError: failed,
      },
    )

  const names = () =>
    [...selected.values()].slice(0, 6).map(routeLabel).join(', ') + (selected.size > 6 ? ' …' : '')

  const confirmDeactivate = () =>
    modals.openConfirmModal({
      title: 'Deaktiviraj linije',
      children: (
        <Text size="sm">
          Odabrane linije ({selected.size}) postaju neaktivne i ne izvoze se u GTFS: {names()}.
          Vožnje i vozni red ostaju.
        </Text>
      ),
      labels: { confirm: 'Deaktiviraj', cancel: 'Odustani' },
      confirmProps: { color: 'red' },
      onConfirm: () => applyActive(false),
    })

  const confirmAgency = (agency: { id: string; name: string }) =>
    modals.openConfirmModal({
      title: 'Promijeni prijevoznika',
      children: (
        <Text size="sm">
          Odabrane linije ({selected.size}) prelaze prijevozniku {agency.name}: {names()}. Vožnje i
          vozni red ostaju.
        </Text>
      ),
      labels: { confirm: 'Premjesti', cancel: 'Odustani' },
      onConfirm: () =>
        setAgency.mutate(
          { ids: [...selected.keys()], agencyId: agency.id },
          {
            onSuccess: ({ changed }) =>
              done(
                changed === 0
                  ? `Odabrane linije već vozi ${agency.name}.`
                  : `${agency.name}: premješteno ${changed} ${routesNoun(changed)}.`,
              ),
            onError: failed,
          },
        ),
    })

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

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-end">
        <Group gap="sm" align="baseline">
          <Title order={2}>Linije</Title>
          {routes.data && (
            <Text c="dimmed" size="sm">
              {formatNumber(routes.data.totalCount)} linija
            </Text>
          )}
        </Group>
        <Button leftSection={<IconPlus size={16} />} onClick={newRoute.open}>
          Nova linija
        </Button>
      </Group>
      <RouteDialog opened={newOpened} onClose={newRoute.close} />

      <Group gap="sm" align="flex-end">
        <TextInput
          ref={searchRef}
          placeholder="Oznaka ili naziv"
          aria-label="Pretraži linije"
          leftSection={<IconSearch size={16} />}
          value={search}
          onChange={(event) => {
            setSearch(event.currentTarget.value)
            applySearch(event.currentTarget.value)
          }}
          w={{ base: '100%', xs: 240 }}
        />
        {showAgency &&
          filterSelect(
            'agency',
            'Svi prijevoznici',
            agencies.data?.map((a) => ({ value: a.id, label: a.name })) ?? [],
          )}
        {filterSelect('type', 'Sve vrste', routeTypeOptions)}
        {filterSelect('active', 'Aktivne i neaktivne', yesNo('Aktivne', 'Neaktivne'))}
        {filterSelect('trips', 'S vožnjama i bez', yesNo('S vožnjama', 'Bez vožnji'))}
        <Group gap="xs" ml="auto">
          {filtered && (
            <Button variant="subtle" leftSection={<IconX size={14} />} onClick={clearFilters}>
              Očisti filtre
            </Button>
          )}
          <ColumnsMenu columns={columns} />
        </Group>
      </Group>

      {routes.isPending ? (
        <TableSkeleton rows={10} />
      ) : routes.isError ? (
        <QueryError error={routes.error} onRetry={() => void routes.refetch()} />
      ) : routes.data.items.length === 0 ? (
        <Paper withBorder p="xl">
          <Stack align="center" gap="sm">
            <Text c="dimmed" ta="center">
              {filtered
                ? 'Nijedna linija ne odgovara filtrima.'
                : 'Još nema linija. Dodaj prvu ili uvezi GTFS feed na stranici GTFS uvoz i izvoz.'}
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
            rows={routes.data.items}
            columns={columns.visible}
            getRowId={(row) => row.route.id}
            getRowHref={(row) => `/routes/${row.route.id}`}
            selection={new Set(selected.keys())}
            onSelectionChange={changeSelection}
            sort={table.sort}
            onSortChange={table.setSort}
            stale={routes.isPlaceholderData}
            onSearchKey={() => searchRef.current?.focus()}
            onPageKey={(step) => {
              const page = table.page + step
              if (page >= 1 && page <= totalPages) {
                table.setPage(page)
              }
            }}
          />
          <TableFooter
            totalCount={routes.data.totalCount}
            page={table.page}
            pageSize={table.pageSize}
            onPage={table.setPage}
            onPageSize={table.setPageSize}
          />
        </>
      )}

      <BulkBar count={selected.size} noun={routesNoun} onClear={() => setSelected(new Map())}>
        <Button
          size="xs"
          variant="light"
          color="green"
          leftSection={<IconCircleCheck size={14} />}
          loading={setActive.isPending && setActive.variables.isActive}
          onClick={() => applyActive(true)}
        >
          Aktiviraj
        </Button>
        <Button
          size="xs"
          variant="light"
          color="red"
          leftSection={<IconCircleOff size={14} />}
          loading={setActive.isPending && !setActive.variables.isActive}
          onClick={confirmDeactivate}
        >
          Deaktiviraj
        </Button>
        {showAgency && (
          <Menu position="top">
            <Menu.Target>
              <Button
                size="xs"
                variant="light"
                leftSection={<IconBuildingCommunity size={14} />}
                loading={setAgency.isPending}
              >
                Prijevoznik
              </Button>
            </Menu.Target>
            <Menu.Dropdown>
              <Menu.Label>Premjesti prijevozniku</Menu.Label>
              {agencies.data?.map((agency) => (
                <Menu.Item key={agency.id} onClick={() => confirmAgency(agency)}>
                  {agency.name}
                </Menu.Item>
              ))}
            </Menu.Dropdown>
          </Menu>
        )}
      </BulkBar>
    </Stack>
  )
}
