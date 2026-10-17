import { Anchor, Box, Group, Paper, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { Calendar } from '@mantine/dates'
import { useMediaQuery } from '@mantine/hooks'
import { IconArrowLeft } from '@tabler/icons-react'
import dayjs from 'dayjs'
import type { CSSProperties } from 'react'
import { Link, useParams } from 'react-router'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { formatDate, formatNumber } from '@/lib/format'
import { useCalendar, useCalendarDates } from './queries'
import {
  exceptionAdded,
  exceptionMap,
  exceptionRemoved,
  type ServiceDay,
  serviceDay,
} from './serviceDays'
import { WeekdayBadges } from './WeekdayBadges'

const dayStyles: Record<Exclude<ServiceDay, null>, CSSProperties> = {
  regular: {
    background: 'var(--mantine-color-blue-light)',
    color: 'var(--mantine-color-blue-light-color)',
  },
  added: {
    background: 'var(--mantine-color-green-light)',
    color: 'var(--mantine-color-green-light-color)',
    fontWeight: 700,
  },
  removed: {
    background: 'var(--mantine-color-red-light)',
    color: 'var(--mantine-color-red-light-color)',
    textDecoration: 'line-through',
  },
}

/** Kalendar: pravila, iznimke i pregled dana u kojima vozi. */
export function CalendarDetailPage() {
  const { calendarId = '' } = useParams()
  const calendar = useCalendar(calendarId)
  const dates = useCalendarDates(calendarId)
  const narrow = useMediaQuery('(max-width: 62em)')

  if (calendar.isPending) {
    return <PageLoader />
  }
  if (calendar.isError) {
    return <QueryError error={calendar.error} onRetry={() => void calendar.refetch()} />
  }

  const exceptions = exceptionMap(dates.data ?? [])
  const added = dates.data?.filter((d) => d.exceptionType === exceptionAdded).length ?? 0
  const removed = dates.data?.filter((d) => d.exceptionType === exceptionRemoved).length ?? 0
  const today = dayjs().format('YYYY-MM-DD')

  return (
    <Stack gap="md">
      <Anchor component={Link} to="/calendars" size="sm">
        <Group gap={4}>
          <IconArrowLeft size={14} />
          Kalendari
        </Group>
      </Anchor>

      <Stack gap={4}>
        <Title order={2}>{calendar.data.name}</Title>
        <Text size="sm" c="dimmed">
          {calendar.data.code}
        </Text>
      </Stack>

      <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md">
        <Paper withBorder p="md">
          <Text size="xs" c="dimmed" mb={6}>
            Dani u tjednu
          </Text>
          <WeekdayBadges calendar={calendar.data} />
        </Paper>
        <Paper withBorder p="md">
          <Text size="xs" c="dimmed" mb={6}>
            Razdoblje
          </Text>
          <Text size="sm">
            {formatDate(calendar.data.startDate)} – {formatDate(calendar.data.endDate)}
          </Text>
        </Paper>
        <Paper withBorder p="md">
          <Text size="xs" c="dimmed" mb={6}>
            Iznimke
          </Text>
          <Text size="sm">
            {dates.isPending
              ? 'Učitavanje…'
              : `${formatNumber(added)} dodanih, ${formatNumber(removed)} ukinutih dana`}
          </Text>
        </Paper>
      </SimpleGrid>

      {dates.isError ? (
        <QueryError error={dates.error} onRetry={() => void dates.refetch()} />
      ) : dates.isPending ? (
        <PageLoader />
      ) : (
        <Paper withBorder p="md">
          <Stack gap="md">
            <Group gap="lg">
              <Legend style={dayStyles.regular} label="Vozi (dan u tjednu)" />
              <Legend style={dayStyles.added} label="Vozi (dodani datum)" />
              <Legend style={dayStyles.removed} label="Ne vozi (ukinuti datum)" />
            </Group>
            <Calendar
              static
              numberOfColumns={narrow ? 1 : 3}
              defaultDate={today}
              maxLevel="year"
              hideOutsideDates
              // Crveni vikendi bi se miješali s ukinutim danima.
              weekendDays={[]}
              getDayProps={(date) => {
                const state = serviceDay(calendar.data, exceptions, date)
                return {
                  style: {
                    ...(state ? dayStyles[state] : undefined),
                    ...(date === today ? { outline: '2px solid var(--mantine-color-text)' } : {}),
                  },
                }
              }}
            />
          </Stack>
        </Paper>
      )}
    </Stack>
  )
}

function Legend({ style, label }: { style: CSSProperties; label: string }) {
  return (
    <Group gap={6}>
      <Box w={18} h={18} style={{ ...style, borderRadius: 4 }} />
      <Text size="sm">{label}</Text>
    </Group>
  )
}
