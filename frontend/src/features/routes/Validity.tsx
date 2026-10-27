import { Badge, Group, Text, Tooltip } from '@mantine/core'
import dayjs from 'dayjs'
import { expiryWarningDays } from '@/features/dashboard/validity'
import { formatDate } from '@/lib/format'

/**
 * Razdoblje voznog reda linije (kalendari njezinih vožnji). Istekao ili onaj
 * koji uskoro istječe označen je kao na Pregledu; bez vožnji crtica.
 */
export function Validity({ from, to }: { from: string | null; to: string | null }) {
  if (!from || !to) {
    return (
      <Text size="sm" c="dimmed">
        —
      </Text>
    )
  }
  const daysLeft = dayjs(to).diff(dayjs().startOf('day'), 'day')
  return (
    <Group gap={6} wrap="nowrap">
      <Text size="sm" style={{ whiteSpace: 'nowrap' }}>
        {formatDate(from)} – {formatDate(to)}
      </Text>
      {daysLeft < 0 ? (
        <Badge size="xs" variant="light" color="red" tt="none">
          isteklo
        </Badge>
      ) : (
        daysLeft < expiryWarningDays && (
          <Tooltip label={`Vozni red vrijedi još ${daysLeft + 1} d.`}>
            <Badge size="xs" variant="light" color="yellow" tt="none">
              uskoro istječe
            </Badge>
          </Tooltip>
        )
      )}
    </Group>
  )
}
