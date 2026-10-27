import {
  Anchor,
  Box,
  Button,
  Card,
  Group,
  Paper,
  SimpleGrid,
  Stack,
  Table,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { useDebouncedValue, useDisclosure } from '@mantine/hooks'
import { IconPlus, IconSearch } from '@tabler/icons-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { TableSkeleton } from '@/components/Skeletons'
import { QueryError } from '@/components/QueryError'
import { NewSeasonDialog } from '@/features/seasons/NewSeasonDialog'
import { type Season, useSeasons } from '@/features/seasons/queries'
import { dayTypeColor, weekdayLabel } from '@/features/seasons/seasonDays'
import { formatDate, formatNumber } from '@/lib/format'
import { useCalendars } from './queries'
import { WeekdayBadges } from './WeekdayBadges'

/**
 * Sezone voznog reda (tipovi dana, praznici, iznimke) i ostali kalendari,
 * npr. uvezeni iz GTFS-a. Kalendara je obično desetak, pa se prikazuju svi.
 */
export function CalendarsPage() {
  const seasons = useSeasons()
  const [newOpened, newSeason] = useDisclosure(false)

  return (
    <Stack gap="lg">
      <Group justify="space-between" align="flex-end">
        <Title order={2}>Kalendari</Title>
        <Button leftSection={<IconPlus size={16} />} onClick={newSeason.open}>
          Nova sezona
        </Button>
      </Group>

      <Stack gap="sm">
        <div>
          <Title order={3}>Sezone voznog reda</Title>
          <Text size="sm" c="dimmed">
            Kada vozi koji tip dana (radni dan, subota, nedjelja), praznici i ostale iznimke.
          </Text>
        </div>
        {seasons.isPending ? (
          <TableSkeleton rows={2} />
        ) : seasons.isError ? (
          <QueryError error={seasons.error} onRetry={() => void seasons.refetch()} />
        ) : seasons.data.length === 0 ? (
          <Paper withBorder p="xl">
            <Stack align="center" gap="sm">
              <Text c="dimmed" ta="center">
                Još nema sezona. Sezona određuje razdoblje voznog reda i dane u kojima vozi koji tip
                dana; praznici se dodaju sami.
              </Text>
              <Button variant="light" leftSection={<IconPlus size={16} />} onClick={newSeason.open}>
                Napravi prvu sezonu
              </Button>
            </Stack>
          </Paper>
        ) : (
          <SimpleGrid cols={{ base: 1, sm: 2, lg: 3 }} spacing="md">
            {seasons.data.map((season) => (
              <SeasonCard key={season.id} season={season} />
            ))}
          </SimpleGrid>
        )}
      </Stack>

      <OtherCalendars />

      <NewSeasonDialog opened={newOpened} onClose={newSeason.close} />
    </Stack>
  )
}

function SeasonCard({ season }: { season: Season }) {
  return (
    <Card withBorder component={Link} to={`/seasons/${season.id}`} padding="md">
      <Text fw={600}>{season.name}</Text>
      <Text size="sm" c="dimmed" mb="sm">
        {formatDate(season.startDate)} – {formatDate(season.endDate)}
      </Text>
      <Stack gap={6}>
        {season.dayTypes.map((t, index) => (
          <Group key={t.id} gap={8} wrap="nowrap">
            <Box
              w={12}
              h={12}
              style={{
                borderRadius: 3,
                flexShrink: 0,
                background: `var(--mantine-color-${dayTypeColor(index)}-filled)`,
              }}
            />
            <Text size="sm" style={{ flex: 1 }}>
              {t.name}
            </Text>
            <Text size="xs" c="dimmed">
              {weekdayLabel(t)}
            </Text>
          </Group>
        ))}
      </Stack>
      <Text size="xs" c="dimmed" mt="sm">
        {formatNumber(season.days.length)} iznimki
      </Text>
    </Card>
  )
}

/** Kalendari izvan sezona, npr. uvezeni iz GTFS-a. */
function OtherCalendars() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [debouncedSearch] = useDebouncedValue(search.trim(), 300)
  const calendars = useCalendars({ Search: debouncedSearch || undefined, PageSize: 100 })

  const items = calendars.data?.items.filter((c) => !c.seasonId) ?? []
  if (calendars.isSuccess && items.length === 0 && !debouncedSearch) {
    return null
  }

  return (
    <Stack gap="sm">
      <Group justify="space-between" align="flex-end">
        <div>
          <Title order={3}>Ostali kalendari</Title>
          <Text size="sm" c="dimmed">
            Kalendari koji nisu u sezoni, npr. uvezeni iz GTFS-a.
          </Text>
        </div>
        <TextInput
          placeholder="Oznaka ili naziv"
          aria-label="Pretraži kalendare"
          leftSection={<IconSearch size={16} />}
          value={search}
          onChange={(event) => setSearch(event.currentTarget.value)}
          w={{ base: '100%', xs: 280 }}
        />
      </Group>

      {calendars.isPending ? (
        <TableSkeleton />
      ) : calendars.isError ? (
        <QueryError error={calendars.error} onRetry={() => void calendars.refetch()} />
      ) : items.length === 0 ? (
        <Paper withBorder p="xl">
          <Text c="dimmed" ta="center">
            Nijedan kalendar ne odgovara pretrazi.
          </Text>
        </Paper>
      ) : (
        <Paper withBorder style={{ overflow: 'hidden' }}>
          <Table.ScrollContainer minWidth={640}>
            <Table
              highlightOnHover
              verticalSpacing="xs"
              style={{ opacity: calendars.isPlaceholderData ? 0.6 : 1 }}
            >
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Naziv</Table.Th>
                  <Table.Th w={120}>Oznaka</Table.Th>
                  <Table.Th w={310}>Dani</Table.Th>
                  <Table.Th w={220}>Razdoblje</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {items.map((calendar) => (
                  <Table.Tr
                    key={calendar.id}
                    onClick={() => void navigate(`/calendars/${calendar.id}`)}
                    style={{ cursor: 'pointer' }}
                  >
                    <Table.Td>
                      <Anchor
                        component={Link}
                        to={`/calendars/${calendar.id}`}
                        c="inherit"
                        size="sm"
                        onClick={(event) => event.stopPropagation()}
                      >
                        {calendar.name}
                      </Anchor>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {calendar.code}
                      </Text>
                    </Table.Td>
                    <Table.Td>
                      <WeekdayBadges calendar={calendar} />
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm">
                        {formatDate(calendar.startDate)} – {formatDate(calendar.endDate)}
                      </Text>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </Table.ScrollContainer>
        </Paper>
      )}
    </Stack>
  )
}
