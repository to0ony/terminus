import {
  Anchor,
  Badge,
  Button,
  Group,
  Pagination,
  Paper,
  Select,
  Stack,
  Table,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useDebouncedCallback, useDisclosure } from '@mantine/hooks'
import { IconPlus, IconSearch } from '@tabler/icons-react'
import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { TableSkeleton } from '@/components/Skeletons'
import { QueryError } from '@/components/QueryError'
import { useAgencies } from '@/features/agencies/queries'
import { formatNumber } from '@/lib/format'
import { type RouteListParams, useRoutes } from './queries'
import { RouteBadge } from './RouteBadge'
import { RouteDialog } from './RouteDialog'
import { routeTypeLabel, routeTypeOptions } from './routeTypes'

const pageSize = 50

const statusOptions = [
  { value: 'true', label: 'Aktivne' },
  { value: 'false', label: 'Neaktivne' },
]

/** Popis linija s pretragom i filtrima; stanje je u adresi (q, agency, type, active, page). */
export function RoutesPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const [search, setSearch] = useState(searchParams.get('q') ?? '')
  const [newOpened, newRoute] = useDisclosure(false)

  const query = searchParams.get('q')
  const agencyId = searchParams.get('agency')
  const routeType = searchParams.get('type')
  const isActive = searchParams.get('active')
  const page = Number(searchParams.get('page')) || 1

  // Filtar mijenja adresu i vraća na prvu stranicu.
  const setFilter = (key: string, value: string | null) => {
    setSearchParams(
      (params) => {
        if (value) {
          params.set(key, value)
        } else {
          params.delete(key)
        }
        params.delete('page')
        return params
      },
      { replace: true },
    )
  }

  const applySearch = useDebouncedCallback((value: string) => setFilter('q', value.trim()), 300)

  const params: RouteListParams = {
    Search: query ?? undefined,
    AgencyId: agencyId ?? undefined,
    RouteType: routeType ? Number(routeType) : undefined,
    IsActive: isActive ? isActive === 'true' : undefined,
    Page: page,
    PageSize: pageSize,
  }

  const routes = useRoutes(params)
  const agencies = useAgencies()
  const agencyNames = new Map(agencies.data?.map((a) => [a.id, a.name]))
  const showAgency = (agencies.data?.length ?? 0) > 1
  const filtered = Boolean(query || agencyId || routeType || isActive)

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-end">
        <Title order={2}>Linije</Title>
        <Group gap="sm">
          {routes.data && (
            <Text c="dimmed" size="sm">
              {formatNumber(routes.data.totalCount)} linija
            </Text>
          )}
          <Button leftSection={<IconPlus size={16} />} onClick={newRoute.open}>
            Nova linija
          </Button>
        </Group>
      </Group>
      <RouteDialog opened={newOpened} onClose={newRoute.close} />

      <Group gap="sm" align="flex-end">
        <TextInput
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
        {showAgency && (
          <Select
            placeholder="Svi prijevoznici"
            aria-label="Prijevoznik"
            data={agencies.data?.map((a) => ({ value: a.id, label: a.name }))}
            value={agencyId}
            onChange={(value) => setFilter('agency', value)}
            clearable
            searchable
            w={{ base: '100%', xs: 220 }}
          />
        )}
        <Select
          placeholder="Sve vrste"
          aria-label="Vrsta prijevoza"
          data={routeTypeOptions}
          value={routeType}
          onChange={(value) => setFilter('type', value)}
          clearable
          w={{ base: '100%', xs: 160 }}
        />
        <Select
          placeholder="Aktivne i neaktivne"
          aria-label="Status"
          data={statusOptions}
          value={isActive}
          onChange={(value) => setFilter('active', value)}
          clearable
          w={{ base: '100%', xs: 180 }}
        />
      </Group>

      {routes.isPending ? (
        <TableSkeleton rows={10} />
      ) : routes.isError ? (
        <QueryError error={routes.error} onRetry={() => void routes.refetch()} />
      ) : routes.data.items.length === 0 ? (
        <Paper withBorder p="xl">
          <Text c="dimmed" ta="center">
            {filtered
              ? 'Nijedna linija ne odgovara filtrima.'
              : 'Još nema linija. Uvezi GTFS feed na stranici GTFS uvoz i izvoz.'}
          </Text>
        </Paper>
      ) : (
        <>
          <Paper withBorder style={{ overflow: 'hidden' }}>
            <Table.ScrollContainer minWidth={560}>
              <Table
                highlightOnHover
                verticalSpacing="xs"
                style={{ opacity: routes.isPlaceholderData ? 0.6 : 1 }}
              >
                <Table.Thead>
                  <Table.Tr>
                    <Table.Th w={90}>Linija</Table.Th>
                    <Table.Th>Naziv</Table.Th>
                    <Table.Th w={160}>Vrsta</Table.Th>
                    {showAgency && <Table.Th>Prijevoznik</Table.Th>}
                    <Table.Th w={110}>Status</Table.Th>
                  </Table.Tr>
                </Table.Thead>
                <Table.Tbody>
                  {routes.data.items.map((route) => (
                    <Table.Tr
                      key={route.id}
                      onClick={() => void navigate(`/routes/${route.id}`)}
                      style={{ cursor: 'pointer' }}
                    >
                      <Table.Td>
                        <RouteBadge route={route} />
                      </Table.Td>
                      <Table.Td>
                        <Anchor
                          component={Link}
                          to={`/routes/${route.id}`}
                          c="inherit"
                          size="sm"
                          onClick={(event) => event.stopPropagation()}
                        >
                          {route.longName ?? route.description ?? route.code}
                        </Anchor>
                      </Table.Td>
                      <Table.Td>{routeTypeLabel(route.routeType)}</Table.Td>
                      {showAgency && <Table.Td>{agencyNames.get(route.agencyId) ?? '—'}</Table.Td>}
                      <Table.Td>
                        <Badge variant="light" color={route.isActive ? 'green' : 'gray'}>
                          {route.isActive ? 'Aktivna' : 'Neaktivna'}
                        </Badge>
                      </Table.Td>
                    </Table.Tr>
                  ))}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
          </Paper>

          <PageNumbers
            totalCount={routes.data.totalCount}
            page={page}
            onChange={(value) =>
              setSearchParams((params) => {
                params.set('page', String(value))
                return params
              })
            }
          />
        </>
      )}
    </Stack>
  )
}

function PageNumbers({
  totalCount,
  page,
  onChange,
}: {
  totalCount: number
  page: number
  onChange: (page: number) => void
}) {
  const total = Math.ceil(totalCount / pageSize)
  if (total <= 1) {
    return null
  }
  return (
    <Group justify="center">
      <Pagination total={total} value={page} onChange={onChange} />
    </Group>
  )
}
