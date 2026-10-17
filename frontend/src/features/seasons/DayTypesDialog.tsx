import {
  ActionIcon,
  Alert,
  Box,
  Button,
  Chip,
  Group,
  Modal,
  Paper,
  Stack,
  Text,
  TextInput,
  Tooltip,
} from '@mantine/core'
import { IconPlus, IconTrash } from '@tabler/icons-react'
import { useState } from 'react'
import { ApiError, errorMessage } from '@/api/errors'
import { formatNumber } from '@/lib/format'
import { type DayTypeRequest, type Season, useSaveDayTypes } from './queries'
import { dayTypeColor, weekdays } from './seasonDays'

type WeekdayKey = (typeof weekdays)[number]['key']

interface DayTypesDialogProps {
  season: Season
  opened: boolean
  onClose: () => void
}

/** Nazivi i dani u tjednu tipova dana; dodavanje i brisanje. */
export function DayTypesDialog({ season, opened, onClose }: DayTypesDialogProps) {
  return (
    <Modal opened={opened} onClose={onClose} title="Tipovi dana" size="lg">
      {opened && <DayTypesForm season={season} onClose={onClose} />}
    </Modal>
  )
}

interface Row extends DayTypeRequest {
  /** Ključ retka; postojeći tipovi imaju Id, novi privremeni ključ. */
  key: string
  name: string
  tripCount: number
}

function DayTypesForm({ season, onClose }: { season: Season; onClose: () => void }) {
  const save = useSaveDayTypes(season.id)
  const [rows, setRows] = useState<Row[]>(() =>
    season.dayTypes.map((t) => ({ ...t, key: t.id, tripCount: t.tripCount })),
  )

  const update = (key: string, change: Partial<Row>) =>
    setRows((current) => current.map((row) => (row.key === key ? { ...row, ...change } : row)))

  const setWeekdays = (key: string, selected: string[]) =>
    update(key, Object.fromEntries(weekdays.map((d) => [d.key, selected.includes(d.key)])))

  const add = () =>
    setRows((current) => [
      ...current,
      {
        key: crypto.randomUUID(),
        id: null,
        name: '',
        tripCount: 0,
        ...Object.fromEntries(weekdays.map((d) => [d.key, false])),
      },
    ])

  // Dan u tjednu smije voziti samo jedan tip dana.
  const owners = (day: WeekdayKey) => rows.filter((row) => row[day]).length
  const clashes = weekdays.filter((d) => owners(d.key) > 1)

  const submit = () =>
    save.mutate(
      rows.map(({ id, name, ...days }) => ({
        id,
        name: name.trim(),
        ...Object.fromEntries(weekdays.map((d) => [d.key, days[d.key] ?? false])),
      })),
      { onSuccess: onClose },
    )

  const error = save.error instanceof ApiError ? save.error : null

  return (
    <Stack gap="md">
      <Text size="sm" c="dimmed">
        Svaki dan u tjednu vozi po jednom tipu dana. Dan koji nije ni u jednom tipu ne vozi, osim
        ako ga dodaš kao iznimku u kalendaru.
      </Text>

      {rows.map((row, index) => {
        const holidayType = row.id !== null && row.id === season.holidayCalendarId
        const locked = row.tripCount > 0 || holidayType
        return (
          <Paper key={row.key} withBorder p="sm">
            <Stack gap="xs">
              <Group gap="sm" wrap="nowrap">
                <Box
                  w={14}
                  h={14}
                  style={{
                    borderRadius: 4,
                    flexShrink: 0,
                    background: `var(--mantine-color-${dayTypeColor(index)}-filled)`,
                  }}
                />
                <TextInput
                  aria-label="Naziv tipa dana"
                  placeholder="npr. Petak"
                  value={row.name}
                  onChange={(event) => update(row.key, { name: event.currentTarget.value })}
                  style={{ flex: 1 }}
                />
                {row.tripCount > 0 && (
                  <Text size="xs" c="dimmed" style={{ whiteSpace: 'nowrap' }}>
                    {formatNumber(row.tripCount)} vožnji
                  </Text>
                )}
                <Tooltip
                  label={
                    holidayType
                      ? 'Po ovom tipu se vozi na praznike'
                      : row.tripCount > 0
                        ? 'Koriste ga vožnje'
                        : 'Obriši'
                  }
                >
                  <ActionIcon
                    variant="subtle"
                    color="red"
                    aria-label="Obriši tip dana"
                    disabled={locked || rows.length === 1}
                    onClick={() =>
                      setRows((current) => current.filter((other) => other.key !== row.key))
                    }
                  >
                    <IconTrash size={16} />
                  </ActionIcon>
                </Tooltip>
              </Group>
              <Chip.Group
                multiple
                value={weekdays.filter((d) => row[d.key]).map((d) => d.key)}
                onChange={(selected) => setWeekdays(row.key, selected)}
              >
                <Group gap={6}>
                  {weekdays.map((d) => (
                    <Chip key={d.key} value={d.key} size="xs" color={dayTypeColor(index)}>
                      {d.short}
                    </Chip>
                  ))}
                </Group>
              </Chip.Group>
            </Stack>
          </Paper>
        )
      })}

      <Group>
        <Button variant="default" leftSection={<IconPlus size={16} />} onClick={add}>
          Dodaj tip dana
        </Button>
      </Group>

      {clashes.length > 0 && (
        <Alert color="yellow" variant="light">
          {clashes.map((d) => d.short).join(', ')}: dan je u više tipova dana. Ostavi ga samo u
          jednom.
        </Alert>
      )}
      {save.isError && (
        <Alert color="red" variant="light">
          {error?.fieldErrors.dayTypes?.join(' ') || errorMessage(save.error)}
        </Alert>
      )}

      <Group justify="flex-end">
        <Button variant="default" onClick={onClose}>
          Odustani
        </Button>
        <Button
          onClick={submit}
          loading={save.isPending}
          disabled={clashes.length > 0 || rows.some((r) => !r.name.trim())}
        >
          Spremi
        </Button>
      </Group>
    </Stack>
  )
}
