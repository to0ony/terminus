import { Text, Tooltip } from '@mantine/core'
import { IconWheelchair, IconWheelchairOff } from '@tabler/icons-react'
import { wheelchairLabels } from './locationTypes'

/** Pristupačnost stanice kao ikona; bez podatka crtica. */
export function StopAccess({ value }: { value: number }) {
  if (value === 0) {
    return (
      <Text size="sm" c="dimmed">
        —
      </Text>
    )
  }
  const Icon = value === 1 ? IconWheelchair : IconWheelchairOff
  return (
    <Tooltip label={wheelchairLabels[value]}>
      <Icon
        size={18}
        color={value === 1 ? 'var(--mantine-color-green-6)' : 'var(--mantine-color-red-6)'}
        aria-label={wheelchairLabels[value]}
      />
    </Tooltip>
  )
}
