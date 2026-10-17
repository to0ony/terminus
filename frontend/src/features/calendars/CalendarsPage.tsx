import { Anchor, Group, Paper, Stack, Table, Text, TextInput, Title } from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { IconSearch } from '@tabler/icons-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { formatDate, formatNumber } from '@/lib/format'
import { useCalendars } from './queries'
import { WeekdayBadges } from './WeekdayBadges'

/**
 * Kalendari voznog reda (GTFS calendar). Instanca ih obično ima desetak, pa
 * se prikazuju svi, bez stranica.
 */
export function CalendarsPage() {
  const navigate = useNavigate()
  const [search, setSearch] = useState('')
  const [debouncedSearch] = useDebouncedValue(search.trim(), 300)
  const calendars = useCalendars({ Search: debouncedSearch || undefined, PageSize: 100 })

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-end">
        <Title order={2}>Kalendari</Title>
        {calendars.data && (
          <Text c="dimmed" size="sm">
            {formatNumber(calendars.data.totalCount)} kalendara
          </Text>
        )}
      </Group>

      <TextInput
        placeholder="Oznaka ili naziv"
        aria-label="Pretraži kalendare"
        leftSection={<IconSearch size={16} />}
        value={search}
        onChange={(event) => setSearch(event.currentTarget.value)}
        w={{ base: '100%', xs: 280 }}
      />

      {calendars.isPending ? (
        <PageLoader />
      ) : calendars.isError ? (
        <QueryError error={calendars.error} onRetry={() => void calendars.refetch()} />
      ) : calendars.data.items.length === 0 ? (
        <Paper withBorder p="xl">
          <Text c="dimmed" ta="center">
            {debouncedSearch
              ? 'Nijedan kalendar ne odgovara pretrazi.'
              : 'Još nema kalendara. Uvezi GTFS feed na stranici GTFS uvoz i izvoz.'}
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
                {calendars.data.items.map((calendar) => (
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
