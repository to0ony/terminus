import { Alert, Button, Group, Modal, Radio, Stack, Text, TextInput } from '@mantine/core'
import { useState } from 'react'
import { errorMessage } from '@/api/errors'
import { formatDate } from '@/lib/format'
import { type Season, useResetSeasonDay, useSetSeasonDay } from './queries'
import { dayMap, dayState, weekdayOf, weekdays } from './seasonDays'

interface DayDialogProps {
  season: Season
  /** Odabrani dan (YYYY-MM-DD); null zatvara dijalog. */
  date: string | null
  onClose: () => void
}

/** Kako vozi jedan dan: kao neki tip dana, ne vozi ili normalno, uz napomenu. */
export function DayDialog({ season, date, onClose }: DayDialogProps) {
  return (
    <Modal opened={date !== null} onClose={onClose} title={date && title(date)} size="md">
      {/* Novi ključ vraća formu na stanje odabranog dana. */}
      {date && <DayForm key={date} season={season} date={date} onClose={onClose} />}
    </Modal>
  )
}

function DayForm({ season, date, onClose }: { season: Season; date: string; onClose: () => void }) {
  const setDay = useSetSeasonDay(season.id)
  const resetDay = useResetSeasonDay(season.id)

  const days = dayMap(season)
  const state = dayState(season, days, date)
  const day = state.day
  const regular = season.dayTypes.find((t) => t[weekdayOf(date)])

  const [choice, setChoice] = useState(() =>
    day?.mode === 'NoService' ? 'none' : (state.dayType?.id ?? 'none'),
  )
  const [note, setNote] = useState(day?.note ?? '')

  const save = () => {
    // Običan dan bez promjene ne treba iznimku.
    if (!day && choice === (regular?.id ?? 'none') && !note.trim()) {
      onClose()
      return
    }
    // Odabir tipa koji inače vozi taj dan je "normalno"; tako praznik može voziti kao radni dan.
    const mode = choice === 'none' ? 'NoService' : choice === regular?.id ? 'Regular' : 'RunsAs'
    setDay.mutate(
      {
        date,
        mode,
        calendarId: mode === 'RunsAs' ? choice : null,
        note: note.trim() || null,
      },
      { onSuccess: onClose },
    )
  }

  const reset = () => resetDay.mutate(date, { onSuccess: onClose })
  const error = setDay.error ?? resetDay.error

  return (
    <Stack gap="md">
      <Text size="sm" c="dimmed">
        {day?.source === 'Holiday'
          ? `Praznik: ${day.note}. Vozi po pravilu za praznike, a ovdje ga možeš promijeniti samo za ovaj dan.`
          : day?.source === 'Period'
            ? `Razdoblje „${day.note}”. Vozi po pravilu razdoblja, a ovdje ga možeš promijeniti samo za ovaj dan.`
            : day
              ? 'Ručna iznimka.'
              : regular
                ? `Inače vozi kao ${regular.name}.`
                : 'Ovaj dan u tjednu inače ne vozi nijedan tip dana.'}
      </Text>

      <Radio.Group label="Vozi" value={choice} onChange={(value) => setChoice(String(value))}>
        <Stack gap={8} mt="xs">
          {season.dayTypes.map((t) => (
            <Radio
              key={t.id}
              value={t.id}
              label={t.id === regular?.id ? `kao ${t.name} (inače)` : `kao ${t.name}`}
            />
          ))}
          <Radio value="none" label="ne vozi" />
        </Stack>
      </Radio.Group>

      <TextInput
        label="Napomena"
        placeholder="npr. Badnjak, Dan grada"
        maxLength={200}
        value={note}
        onChange={(event) => setNote(event.currentTarget.value)}
      />

      {error && (
        <Alert color="red" variant="light">
          {errorMessage(error)}
        </Alert>
      )}

      <Group justify="space-between">
        {day?.source === 'Manual' ? (
          <Button variant="subtle" color="red" onClick={reset} loading={resetDay.isPending}>
            {/* Praznik se tada vraća na pravilo za praznike. */}
            Ukloni iznimku
          </Button>
        ) : (
          <span />
        )}
        <Group gap="sm">
          <Button variant="default" onClick={onClose}>
            Odustani
          </Button>
          <Button onClick={save} loading={setDay.isPending}>
            Spremi
          </Button>
        </Group>
      </Group>
    </Stack>
  )
}

function title(date: string): string {
  const weekday = weekdays.find((d) => d.key === weekdayOf(date))?.name ?? ''
  return `${weekday.charAt(0).toUpperCase()}${weekday.slice(1)}, ${formatDate(date)}`
}
