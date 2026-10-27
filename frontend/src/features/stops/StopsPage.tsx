import {
  Anchor,
  Alert,
  Badge,
  Button,
  Grid,
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
import { useDebouncedCallback } from '@mantine/hooks'
import { IconMapPinPlus, IconSearch, IconWorldDownload } from '@tabler/icons-react'
import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { BlockSkeleton, TableSkeleton } from '@/components/Skeletons'
import { QueryError } from '@/components/QueryError'
import { distance } from '@/features/variants/variantMath'
import { formatNumber } from '@/lib/format'
import { locationTypeLabel, locationTypeOptions } from './locationTypes'
import { type Stop, type StopListParams, useStops, useStopsExtent } from './queries'
import { OsmSuggestions } from './OsmSuggestions'
import { type NewStop, StopDialog } from './StopDialog'
import { StopsMap } from './StopsMap'

const pageSize = 25

/** Popis stanica (q, type, page u adresi) i karta; odabir vodi na stanicu. */
export function StopsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const navigate = useNavigate()
  const [search, setSearch] = useState(searchParams.get('q') ?? '')
  const [hoveredId, setHoveredId] = useState<string | null>(null)
  // Postavljanje novih stanica klikom na kartu; ostaje uključeno za sljedeću.
  const [placing, setPlacing] = useState(false)
  const [osm, setOsm] = useState(false)
  const [create, setCreate] = useState<NewStop | null>(null)
  const [lastCreated, setLastCreated] = useState<Stop | null>(null)

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

  const query = searchParams.get('q')
  const locationType = searchParams.get('type')
  const page = Number(searchParams.get('page')) || 1

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

  const params: StopListParams = {
    Search: query ?? undefined,
    LocationType: locationType ? Number(locationType) : undefined,
    Page: page,
    PageSize: pageSize,
  }
  const stops = useStops(params)
  const extent = useStopsExtent()
  const totalPages = stops.data ? Math.ceil(stops.data.totalCount / pageSize) : 0

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-end">
        <Title order={2}>Stanice</Title>
        <Group gap="sm">
          {stops.data && (
            <Text c="dimmed" size="sm">
              {formatNumber(stops.data.totalCount)} stanica
            </Text>
          )}
          <Button
            leftSection={<IconMapPinPlus size={16} />}
            variant={placing ? 'filled' : 'light'}
            onClick={() => {
              setPlacing(!placing)
              setOsm(false)
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

      <Grid gap="md">
        <Grid.Col span={{ base: 12, lg: 5 }}>
          <Stack gap="sm">
            <Group gap="sm" grow>
              <TextInput
                placeholder="Naziv ili oznaka"
                aria-label="Pretraži stanice"
                leftSection={<IconSearch size={16} />}
                value={search}
                onChange={(event) => {
                  setSearch(event.currentTarget.value)
                  applySearch(event.currentTarget.value)
                }}
              />
              <Select
                placeholder="Sve vrste"
                aria-label="Vrsta stanice"
                data={locationTypeOptions}
                value={locationType}
                onChange={(value) => setFilter('type', value)}
                clearable
              />
            </Group>

            {stops.isPending ? (
              <TableSkeleton rows={10} />
            ) : stops.isError ? (
              <QueryError error={stops.error} onRetry={() => void stops.refetch()} />
            ) : stops.data.items.length === 0 ? (
              <Paper withBorder p="xl">
                <Text c="dimmed" ta="center">
                  {query || locationType
                    ? 'Nijedna stanica ne odgovara filtrima.'
                    : 'Još nema stanica. Pronađi svoj grad tražilicom na karti, pa klikni „Nova stanica” ili uvezi GTFS feed.'}
                </Text>
              </Paper>
            ) : (
              <>
                <Paper withBorder style={{ overflow: 'hidden' }}>
                  <Table
                    highlightOnHover
                    verticalSpacing={6}
                    style={{ opacity: stops.isPlaceholderData ? 0.6 : 1 }}
                  >
                    <Table.Thead>
                      <Table.Tr>
                        <Table.Th>Naziv</Table.Th>
                        <Table.Th w={110}>Oznaka</Table.Th>
                        <Table.Th w={70}>Peron</Table.Th>
                      </Table.Tr>
                    </Table.Thead>
                    <Table.Tbody>
                      {stops.data.items.map((stop) => (
                        <Table.Tr
                          key={stop.id}
                          onClick={() => void navigate(`/stops/${stop.id}`)}
                          onMouseEnter={() => setHoveredId(stop.id)}
                          onMouseLeave={() => setHoveredId(null)}
                          style={{ cursor: 'pointer' }}
                        >
                          <Table.Td>
                            <Group gap={6}>
                              <Anchor
                                component={Link}
                                to={`/stops/${stop.id}`}
                                c="inherit"
                                size="sm"
                                onClick={(event) => event.stopPropagation()}
                              >
                                {stop.name}
                              </Anchor>
                              {stop.locationType !== 0 && (
                                <Badge size="xs" variant="light" tt="none">
                                  {locationTypeLabel(stop.locationType)}
                                </Badge>
                              )}
                              {!stop.isActive && (
                                <Badge size="xs" variant="light" color="gray" tt="none">
                                  Neaktivna
                                </Badge>
                              )}
                            </Group>
                          </Table.Td>
                          <Table.Td>
                            <Text size="sm" c="dimmed">
                              {stop.publicCode ?? stop.code}
                            </Text>
                          </Table.Td>
                          <Table.Td>{stop.platformCode}</Table.Td>
                        </Table.Tr>
                      ))}
                    </Table.Tbody>
                  </Table>
                </Paper>
                {totalPages > 1 && (
                  <Group justify="center">
                    <Pagination
                      size="sm"
                      total={totalPages}
                      value={page}
                      siblings={0}
                      onChange={(value) =>
                        setSearchParams((current) => {
                          current.set('page', String(value))
                          return current
                        })
                      }
                    />
                  </Group>
                )}
              </>
            )}
          </Stack>
        </Grid.Col>

        <Grid.Col span={{ base: 12, lg: 7 }}>
          <Paper withBorder style={{ overflow: 'hidden' }}>
            {extent.isPending ? (
              <BlockSkeleton h={{ base: 400, lg: 'calc(100vh - 190px)' }} />
            ) : (
              <StopsMap
                h={{ base: 400, lg: 'calc(100vh - 190px)' }}
                mih={400}
                extent={extent.data ?? null}
                selectedId={hoveredId ?? lastCreated?.id ?? null}
                onSelect={placing ? undefined : (stop) => void navigate(`/stops/${stop.id}`)}
                onMapClick={placing ? placeAt : undefined}
                searchable
                panel={
                  osm
                    ? (map) => <OsmSuggestions map={map} onClose={() => setOsm(false)} />
                    : undefined
                }
              />
            )}
          </Paper>
        </Grid.Col>
      </Grid>
    </Stack>
  )
}
