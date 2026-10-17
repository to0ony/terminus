import { Alert, Button, Group, List, Modal, Stack, Text, TextInput } from '@mantine/core'
import { DatePickerInput, type DatesRangeValue } from '@mantine/dates'
import { IconCalendar } from '@tabler/icons-react'
import { useState } from 'react'
import { ApiError, errorMessage } from '@/api/errors'
import { HolidayFields } from './HolidayFields'
import { type HolidayRule, holidayRuleToApi } from './holidayRule'
import { type Season, useUpdateSeason } from './queries'

interface EditSeasonDialogProps {
  season: Season
  opened: boolean
  onClose: () => void
}

/** Naziv, razdoblje i praznici sezone. */
export function EditSeasonDialog({ season, opened, onClose }: EditSeasonDialogProps) {
  return (
    <Modal opened={opened} onClose={onClose} title="Uredi sezonu" size="lg">
      {opened && <EditSeasonForm season={season} onClose={onClose} />}
    </Modal>
  )
}

function EditSeasonForm({ season, onClose }: { season: Season; onClose: () => void }) {
  const update = useUpdateSeason(season.id)
  const [name, setName] = useState(season.name)
  const [range, setRange] = useState<DatesRangeValue<string>>([season.startDate, season.endDate])
  const [country, setCountry] = useState(season.holidayCountry ?? null)
  const [rule, setRule] = useState<HolidayRule>(
    season.holidayMode === 'RunsAs' && season.holidayCalendarId
      ? `type:${season.holidayCalendarId}`
      : season.holidayMode === 'NoService'
        ? 'NoService'
        : 'Regular',
  )

  const [from, to] = range

  const submit = () => {
    if (!from || !to) {
      return
    }
    const { holidayMode, dayTypeKey } = holidayRuleToApi(rule)
    update.mutate(
      {
        name: name.trim(),
        startDate: from,
        endDate: to,
        holidayCountry: country,
        holidayMode: country ? holidayMode : 'Regular',
        holidayCalendarId: country ? dayTypeKey : null,
      },
      { onSuccess: onClose },
    )
  }

  const error = update.error instanceof ApiError ? update.error : null

  return (
    <Stack gap="md">
      <TextInput
        label="Naziv"
        value={name}
        onChange={(event) => setName(event.currentTarget.value)}
        error={error?.fieldErrors.name?.[0]}
      />
      <DatePickerInput
        type="range"
        label="Razdoblje"
        description="Iznimke izvan novog razdoblja se brišu, a praznici se ponovno dohvaćaju."
        leftSection={<IconCalendar size={16} />}
        valueFormat="D. M. YYYY."
        value={range}
        onChange={setRange}
        error={error?.fieldErrors.endDate?.[0]}
      />
      <HolidayFields
        country={country}
        onCountryChange={setCountry}
        rule={rule}
        onRuleChange={setRule}
        dayTypes={season.dayTypes.map((t) => ({ key: t.id, name: t.name }))}
      />

      {update.isError && !error?.fieldErrors.name && !error?.fieldErrors.endDate && (
        <Alert color="red" variant="light">
          <Text size="sm">{errorMessage(update.error)}</Text>
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
        <Button onClick={submit} loading={update.isPending} disabled={!from || !to || !name.trim()}>
          Spremi
        </Button>
      </Group>
    </Stack>
  )
}
