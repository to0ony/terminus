import {
  Alert,
  Button,
  Group,
  Input,
  List,
  Modal,
  SegmentedControl,
  Stack,
  Text,
  TextInput,
} from '@mantine/core'
import { DatePickerInput, type DatesRangeValue } from '@mantine/dates'
import { IconAlertCircle, IconCalendar } from '@tabler/icons-react'
import dayjs from 'dayjs'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { ApiError, errorMessage } from '@/api/errors'
import { HolidayFields } from './HolidayFields'
import { type HolidayRule, holidayRuleToApi } from './holidayRule'
import { useCreateSeason, useHolidayCountries } from './queries'
import { type DayTypePreset, dayTypePresets, weekdayLabel } from './seasonDays'

interface NewSeasonDialogProps {
  opened: boolean
  onClose: () => void
}

/**
 * Nova sezona: razdoblje, tipovi dana (zadano radni dan, subota, nedjelja) i
 * praznici. Iznimke se dodaju poslije, klikom na dan u kalendaru sezone.
 */
export function NewSeasonDialog({ opened, onClose }: NewSeasonDialogProps) {
  const navigate = useNavigate()
  const create = useCreateSeason()
  const countries = useHolidayCountries()

  const [range, setRange] = useState<DatesRangeValue<string>>(defaultRange)
  const [name, setName] = useState<string | null>(null)
  const [preset, setPreset] = useState<DayTypePreset>('week')
  // undefined: korisnik još nije birao, pa vrijedi prijedlog prema zoni prijevoznika.
  const [chosenCountry, setCountry] = useState<string | null | undefined>(undefined)
  const [rule, setRule] = useState<HolidayRule>(`type:${lastIndex('week')}`)

  const country = chosenCountry === undefined ? (countries.data?.suggested ?? null) : chosenCountry

  const [from, to] = range
  const dayTypes = dayTypePresets[preset].dayTypes
  const shownName = name ?? suggestedName(from, to)

  const close = () => {
    create.reset()
    onClose()
  }

  const selectPreset = (value: DayTypePreset) => {
    setPreset(value)
    // Praznici zadano voze kao zadnji tip dana (nedjelja, vikend…).
    setRule(`type:${lastIndex(value)}`)
  }

  const submit = () => {
    if (!from || !to) {
      return
    }
    const { holidayMode, dayTypeKey } = holidayRuleToApi(rule)
    create.mutate(
      {
        name: shownName,
        startDate: from,
        endDate: to,
        dayTypes: dayTypes.map((d) => ({ ...d })),
        holidayCountry: country,
        holidayMode: country ? holidayMode : 'Regular',
        holidayDayTypeIndex: dayTypeKey === null ? null : Number(dayTypeKey),
      },
      {
        onSuccess: (season) => {
          close()
          void navigate(`/seasons/${season.id}`)
        },
      },
    )
  }

  const error = create.error instanceof ApiError ? create.error : null
  const fieldError = (key: string) => error?.fieldErrors[key]?.[0]
  const formError =
    create.isError && !error?.fieldErrors.name && !error?.fieldErrors.endDate
      ? errorMessage(create.error)
      : null

  return (
    <Modal opened={opened} onClose={close} title="Nova sezona voznog reda" size="lg">
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          Sezona je razdoblje u kojem vrijedi jedan vozni red, npr. zimski od rujna do lipnja. Za
          promjenu voznog reda od nekog datuma napravi novu sezonu od tog dana.
        </Text>

        <DatePickerInput
          type="range"
          label="Razdoblje"
          leftSection={<IconCalendar size={16} />}
          valueFormat="D. M. YYYY."
          value={range}
          onChange={setRange}
          error={fieldError('endDate')}
        />

        <TextInput
          label="Naziv"
          value={shownName}
          onChange={(event) => setName(event.currentTarget.value)}
          error={fieldError('name')}
        />

        <Input.Wrapper
          label="Tipovi dana"
          description="Kasnije ih možeš preimenovati ili dodati nove (npr. Petak)."
        >
          <SegmentedControl
            fullWidth
            mt={4}
            value={preset}
            onChange={(value) => selectPreset(value as DayTypePreset)}
            data={Object.entries(dayTypePresets).map(([value, p]) => ({ value, label: p.label }))}
          />
          <Text size="xs" c="dimmed" mt={6}>
            {dayTypes.map((d) => `${d.name}: ${weekdayLabel(d)}`).join(' · ')}
          </Text>
        </Input.Wrapper>

        <HolidayFields
          country={country}
          onCountryChange={setCountry}
          rule={rule}
          onRuleChange={setRule}
          dayTypes={dayTypes.map((d, index) => ({ key: String(index), name: d.name }))}
          countryError={fieldError('holidayCountry')}
        />

        {formError && (
          <Alert color="red" variant="light" icon={<IconAlertCircle />}>
            <Text size="sm">{formError}</Text>
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
          <Button variant="default" onClick={close}>
            Odustani
          </Button>
          <Button onClick={submit} loading={create.isPending} disabled={!from || !to}>
            Napravi sezonu
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

/** Godinu dana od danas. */
function defaultRange(): DatesRangeValue<string> {
  const start = dayjs()
  return [start.format('YYYY-MM-DD'), start.add(1, 'year').subtract(1, 'day').format('YYYY-MM-DD')]
}

/** "Vozni red 2026/27" ili "Vozni red 2026". */
function suggestedName(from: string | null, to: string | null): string {
  if (!from || !to) {
    return 'Vozni red'
  }
  const [start, end] = [from.slice(0, 4), to.slice(0, 4)]
  return start === end ? `Vozni red ${start}` : `Vozni red ${start}/${end.slice(2)}`
}

function lastIndex(preset: DayTypePreset): number {
  return dayTypePresets[preset].dayTypes.length - 1
}
