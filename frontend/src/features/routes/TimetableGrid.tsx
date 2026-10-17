import { Paper, Table, Text, Tooltip } from '@mantine/core'
import { formatGtfsTime } from '@/lib/format'
import type { RouteTimetable } from './queries'

// Prvi stupac (stanice) ostaje vidljiv pri vodoravnom pomicanju.
const stickyCell = {
  position: 'sticky',
  left: 0,
  zIndex: 1,
  background: 'var(--mantine-color-body)',
  // Na mobitelu uži, da ostane mjesta za vremena.
  minWidth: 'min(200px, 36vw)',
  maxWidth: 280,
} as const

/**
 * Vozni red kao tablica: stanice su redci, vožnje stupci (po polasku). Prazna
 * ćelija znači da vožnja tu ne staje; okvirna vremena (timepoint 0) su ukošena.
 */
export function TimetableGrid({ timetable }: { timetable: RouteTimetable }) {
  const { stops, trips } = timetable

  return (
    <Paper withBorder style={{ overflow: 'hidden' }}>
      <Table.ScrollContainer minWidth={0} maxHeight="70vh">
        <Table
          withColumnBorders
          withRowBorders
          stickyHeader
          verticalSpacing={4}
          horizontalSpacing={8}
          fz="sm"
          style={{ fontVariantNumeric: 'tabular-nums' }}
        >
          <Table.Thead>
            <Table.Tr>
              <Table.Th style={{ ...stickyCell, zIndex: 3 }}>Stanica</Table.Th>
              {trips.map((trip, index) => (
                <Table.Th key={trip.id} ta="center">
                  <Tooltip label={trip.headsign ?? trip.code} openDelay={300}>
                    <Text size="xs" c="dimmed" span>
                      {index + 1}
                    </Text>
                  </Tooltip>
                </Table.Th>
              ))}
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {stops.map((stop, row) => (
              <Table.Tr key={`${stop.id}-${row}`}>
                <Table.Td style={stickyCell}>
                  <Text size="sm" truncate title={stop.name}>
                    {stop.name}
                    {stop.platformCode && (
                      <Text span size="xs" c="dimmed">
                        {' '}
                        · {stop.platformCode}
                      </Text>
                    )}
                  </Text>
                </Table.Td>
                {trips.map((trip) => {
                  const time = trip.times[row]
                  return (
                    <Table.Td key={trip.id} ta="center">
                      {time && (
                        <Text span size="sm" fs={time.timepoint ? undefined : 'italic'}>
                          {formatGtfsTime(time.departureTime)}
                        </Text>
                      )}
                    </Table.Td>
                  )
                })}
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </Table.ScrollContainer>
    </Paper>
  )
}
