import { Box, Paper, SimpleGrid, Text } from '@mantine/core'
import { Month } from '@mantine/dates'
import type { CSSProperties } from 'react'
import type { Season } from './queries'
import { dayMap, dayState, dayTypeColor, monthsOf } from './seasonDays'

const monthFormat = new Intl.DateTimeFormat('hr', {
  month: 'long',
  year: 'numeric',
  timeZone: 'UTC',
})

interface SeasonMonthsProps {
  season: Season
  onDayClick: (date: string) => void
}

/**
 * Svi mjeseci sezone. Dan je obojan bojom tipa dana koji tada vozi; praznik
 * ima zvjezdicu, ručna iznimka točkicu, a dan bez vožnje je prekrižen.
 */
export function SeasonMonths({ season, onDayClick }: SeasonMonthsProps) {
  const days = dayMap(season)
  const indexOf = new Map(season.dayTypes.map((t, i) => [t.id, i]))

  const dayStyle = (date: string): CSSProperties => {
    const state = dayState(season, days, date)
    if (state.kind === 'outside') {
      return { opacity: 0.35 }
    }
    if (!state.dayType) {
      return {
        color: 'var(--mantine-color-dimmed)',
        textDecoration: 'line-through',
        background: 'var(--mantine-color-default-hover)',
      }
    }
    const color = dayTypeColor(indexOf.get(state.dayType.id) ?? 0)
    return {
      background: `var(--mantine-color-${color}-light)`,
      color: `var(--mantine-color-${color}-light-color)`,
      fontWeight: state.kind === 'regular' ? undefined : 700,
      // Iznimka je uokvirena, da se vidi i kad je boja ista kao inače.
      outline:
        state.kind === 'manual' ? `2px solid var(--mantine-color-${color}-filled)` : undefined,
      outlineOffset: -2,
    }
  }

  return (
    <SimpleGrid cols={{ base: 1, xs: 2, lg: 3 }} spacing="md">
      {monthsOf(season.startDate, season.endDate).map((month) => (
        <Paper key={month} withBorder p="sm">
          <Text fw={600} size="sm" mb={6} tt="capitalize">
            {monthFormat.format(new Date(`${month}T00:00:00Z`))}
          </Text>
          <Month
            month={month}
            locale="hr"
            firstDayOfWeek={1}
            hideOutsideDates
            weekendDays={[]}
            highlightToday
            fullWidth
            size="sm"
            getDayProps={(date) => {
              const outside = date < season.startDate || date > season.endDate
              return {
                'data-date': date,
                style: dayStyle(date),
                disabled: outside,
                onClick: outside ? undefined : () => onDayClick(date),
                title: days.get(date)?.note ?? undefined,
              }
            }}
            renderDay={(date) => {
              const day = days.get(date)
              return (
                <Box pos="relative" w="100%" h="100%" style={centered}>
                  {Number(date.slice(8))}
                  {day && (
                    <Box component="span" style={marker} aria-hidden>
                      {day.source === 'Holiday' ? '★' : '•'}
                    </Box>
                  )}
                </Box>
              )
            }}
          />
        </Paper>
      ))}
    </SimpleGrid>
  )
}

const centered: CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
}

const marker: CSSProperties = {
  position: 'absolute',
  top: -1,
  right: 1,
  fontSize: 9,
  lineHeight: 1,
}
