import {
  ActionIcon,
  Badge,
  Group,
  Loader,
  SegmentedControl,
  Slider,
  Stack,
  Switch,
  Text,
  Tooltip,
} from '@mantine/core'
import { DatePickerInput } from '@mantine/dates'
import { IconCalendar, IconPlayerPauseFilled, IconPlayerPlayFilled } from '@tabler/icons-react'
import { day as daySeconds, formatTime } from './clock'
import type { SimulationClock } from './useSimulationClock'

/** Brzine simulacije: koliko puta brže od stvarnog vremena. */
const speeds = [1, 5, 10, 30, 60, 120, 300, 600]

interface ClockPanelProps {
  clock: SimulationClock
  /** Smije li se birati simulacija (javna karta je samo uživo). */
  simulation: boolean
  /** Na karti su sva vozila, ne samo odabranog smjera. */
  showAll: boolean
  onShowAllChange: (showAll: boolean) => void
  loadingAll: boolean
}

/** Uživo ili simulacija (dan, vrijeme, pokretanje i brzina) i prikaz svih vozila. */
export function ClockPanel({
  clock,
  simulation: canSimulate,
  showAll,
  onShowAllChange,
  loadingAll,
}: ClockPanelProps) {
  const simulation = canSimulate && clock.mode === 'simulation'
  const speedIndex = Math.max(0, speeds.indexOf(clock.speed))

  return (
    <Stack gap="xs">
      <Group justify="space-between" wrap="nowrap" gap="xs">
        <Group gap="sm" wrap="nowrap">
          {canSimulate && (
            <SegmentedControl
              size="xs"
              value={clock.mode}
              onChange={(value) => clock.setMode(value as SimulationClock['mode'])}
              data={[
                { value: 'live', label: 'Uživo' },
                { value: 'simulation', label: 'Simulacija' },
              ]}
            />
          )}
          <Switch
            size="xs"
            label="Sva vozila"
            checked={showAll}
            onChange={(event) => onShowAllChange(event.currentTarget.checked)}
            thumbIcon={loadingAll ? <Loader size={8} /> : undefined}
          />
        </Group>
        <Group gap="xs" wrap="nowrap">
          {!simulation && (
            <Badge color="green" variant="dot" tt="none">
              Danas
            </Badge>
          )}
          <Text ff="monospace" fw={700} fz="xl">
            {formatTime(clock.time)}
          </Text>
        </Group>
      </Group>

      {simulation && (
        <>
          <Group gap="sm" wrap="nowrap">
            <DatePickerInput
              size="xs"
              aria-label="Dan"
              leftSection={<IconCalendar size={14} />}
              value={clock.date}
              onChange={(value) => value && clock.setDate(value)}
              valueFormat="DD. MM. YYYY."
              w={150}
            />
            <Tooltip label={clock.playing ? 'Zaustavi' : 'Pokreni'}>
              <ActionIcon
                variant="filled"
                radius="xl"
                onClick={() => {
                  if (!clock.playing && clock.time >= daySeconds - 1) {
                    clock.setTime(0)
                  }
                  clock.setPlaying(!clock.playing)
                }}
                aria-label={clock.playing ? 'Zaustavi' : 'Pokreni'}
              >
                {clock.playing ? (
                  <IconPlayerPauseFilled size={14} />
                ) : (
                  <IconPlayerPlayFilled size={14} />
                )}
              </ActionIcon>
            </Tooltip>
            <Slider
              style={{ flex: 1 }}
              min={0}
              max={daySeconds - 1}
              step={60}
              value={clock.time}
              onChange={clock.setTime}
              label={(value) => formatTime(value, false)}
              aria-label="Vrijeme"
            />
          </Group>
          <Group gap="sm" wrap="nowrap">
            <Text size="xs" c="dimmed" w={60}>
              Brzina
            </Text>
            <Slider
              style={{ flex: 1 }}
              min={0}
              max={speeds.length - 1}
              step={1}
              value={speedIndex}
              onChange={(index) => clock.setSpeed(speeds[index])}
              label={(index) => `${speeds[index]}×`}
              marks={speeds.map((speed, index) => ({ value: index, label: `${speed}×` }))}
              mb="md"
              aria-label="Brzina"
            />
          </Group>
        </>
      )}
    </Stack>
  )
}
