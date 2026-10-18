import { Alert, Button, Checkbox, Group, List, Modal, Stack, Text, TextInput } from '@mantine/core'
import { DatePickerInput, type DatesRangeValue } from '@mantine/dates'
import { notifications } from '@mantine/notifications'
import { IconCalendar } from '@tabler/icons-react'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { ApiError, errorMessage } from '@/api/errors'
import { formatNumber } from '@/lib/format'
import { copyName, nextYear } from './copySeason'
import { type Season, useCopySeason } from './queries'

interface CopySeasonDialogProps {
  season: Season
  opened: boolean
  onClose: () => void
}

/** Kopija sezone za drugo razdoblje, zadano isto razdoblje sljedeće godine. */
export function CopySeasonDialog({ season, opened, onClose }: CopySeasonDialogProps) {
  return (
    <Modal opened={opened} onClose={onClose} title="Kopiraj sezonu" size="lg">
      {opened && <CopySeasonForm season={season} onClose={onClose} />}
    </Modal>
  )
}

function CopySeasonForm({ season, onClose }: { season: Season; onClose: () => void }) {
  const navigate = useNavigate()
  const copy = useCopySeason(season.id)
  const tripCount = season.dayTypes.reduce((sum, t) => sum + t.tripCount, 0)

  const [name, setName] = useState(() => copyName(season.name))
  const [range, setRange] = useState<DatesRangeValue<string>>(() => [
    nextYear(season.startDate),
    nextYear(season.endDate),
  ])
  const [withTrips, setWithTrips] = useState(tripCount > 0)
  const [from, to] = range

  const submit = () => {
    if (!from || !to) {
      return
    }
    copy.mutate(
      { name: name.trim(), startDate: from, endDate: to, withTrips },
      {
        onSuccess: (created) => {
          notifications.show({
            color: 'green',
            message: `Sezona „${created.name}” je napravljena.`,
          })
          onClose()
          void navigate(`/seasons/${created.id}`)
        },
      },
    )
  }

  const error = copy.error instanceof ApiError ? copy.error : null

  return (
    <Stack gap="md">
      <Text size="sm">
        Nova sezona dobiva iste tipove dana ({season.dayTypes.map((t) => t.name).join(', ')}) i
        pravilo za praznike, a praznici se dohvaćaju za novo razdoblje. Ručne iznimke dana se ne
        kopiraju jer vrijede za određene datume.
      </Text>
      <TextInput
        label="Naziv"
        value={name}
        onChange={(event) => setName(event.currentTarget.value)}
        error={error?.fieldErrors.name?.[0]}
        data-autofocus
      />
      <DatePickerInput
        type="range"
        label="Razdoblje"
        description="Ne smije se preklapati s drugom sezonom."
        leftSection={<IconCalendar size={16} />}
        valueFormat="D. M. YYYY."
        value={range}
        onChange={setRange}
        error={error?.fieldErrors.endDate?.[0]}
      />
      <Checkbox
        label={`Kopiraj i vožnje (${formatNumber(tripCount)})`}
        description="Svaka vožnja dolazi u isti tip dana nove sezone, s istim vremenima. Kopije se uređuju neovisno o izvornoj sezoni."
        checked={withTrips}
        onChange={(event) => setWithTrips(event.currentTarget.checked)}
        disabled={tripCount === 0}
      />

      {copy.isError && !error?.fieldErrors.name && !error?.fieldErrors.endDate && (
        <Alert color="red" variant="light">
          <Text size="sm">{errorMessage(copy.error)}</Text>
          {error && error.problems.length > 0 && (
            <List size="sm" mt="xs">
              {error.problems.map((problem) => (
                <List.Item key={problem}>{problem}</List.Item>
              ))}
            </List>
          )}
        </Alert>
      )}

      <Group justify="flex-end">
        <Button variant="default" onClick={onClose}>
          Odustani
        </Button>
        <Button onClick={submit} loading={copy.isPending} disabled={!from || !to || !name.trim()}>
          Kopiraj sezonu
        </Button>
      </Group>
    </Stack>
  )
}
