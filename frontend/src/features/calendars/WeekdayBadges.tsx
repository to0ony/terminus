import { Badge, Group, Text } from '@mantine/core'
import type { ServiceCalendar } from './queries'
import { hasWeekdays, weekdays } from './serviceDays'

/** Dani u tjednu kalendara; bez ijednog piše da vozi samo po datumima. */
export function WeekdayBadges({ calendar }: { calendar: ServiceCalendar }) {
  if (!hasWeekdays(calendar)) {
    return (
      <Text size="sm" c="dimmed">
        Samo po datumima
      </Text>
    )
  }
  return (
    <Group gap={4} wrap="nowrap">
      {weekdays.map((day) => (
        <Badge
          key={day.key}
          size="sm"
          radius="sm"
          tt="none"
          variant={calendar[day.key] ? 'filled' : 'default'}
          c={calendar[day.key] ? undefined : 'dimmed'}
          w={38}
          px={0}
        >
          {day.short}
        </Badge>
      ))}
    </Group>
  )
}
