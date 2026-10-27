import {
  ActionIcon,
  Alert,
  Button,
  Group,
  List,
  Modal,
  Paper,
  Select,
  Stack,
  Text,
  TextInput,
  Tooltip,
} from '@mantine/core'
import { DatePickerInput, type DatesRangeValue } from '@mantine/dates'
import { modals } from '@mantine/modals'
import { IconCalendar, IconPencil, IconPlus, IconTrash } from '@tabler/icons-react'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiError, errorMessage } from '@/api/errors'
import { formatDate } from '@/lib/format'
import {
  type Season,
  type SeasonPeriod,
  type SeasonPeriodRequest,
  seasonKeys,
  useSavePeriods,
} from './queries'
import { weekdays } from './seasonDays'
import { deferDelete } from '@/lib/undo'

/** Razdoblje kao zahtjev, da se popis može poslati cijeli. */
const toRequest = (p: SeasonPeriod): SeasonPeriodRequest => ({
  id: p.id,
  name: p.name,
  startDate: p.startDate,
  endDate: p.endDate,
  replacedCalendarId: p.replacedCalendarId,
  mode: p.mode,
  calendarId: p.calendarId,
})

/**
 * Razdoblja sezone (npr. školski raspusti): u razdoblju jedan tip dana vozi kao
 * drugi ili ne vozi. Dani razdoblja vide se u kalendaru, a ručna iznimka i
 * praznik imaju prednost.
 */
export function SeasonPeriods({ season }: { season: Season }) {
  const save = useSavePeriods(season.id)
  const queryClient = useQueryClient()
  const [editing, setEditing] = useState<SeasonPeriod | 'new' | null>(null)
  const name = (id: string | null) => season.dayTypes.find((t) => t.id === id)?.name ?? '?'

  /** Nakon potvrde razdoblje odmah nestaje; sprema se bez njega kad istekne „Poništi”. */
  const remove = (period: SeasonPeriod) => {
    const key = seasonKeys.detail(season.id)
    const update = (change: (periods: SeasonPeriod[]) => SeasonPeriod[]) =>
      queryClient.setQueryData<Season>(key, (current) =>
        current ? { ...current, periods: change(current.periods) } : current,
      )
    const confirm = () =>
      deferDelete({
        message: `Razdoblje „${period.name}” obrisano.`,
        apply: () => update((periods) => periods.filter((p) => p.id !== period.id)),
        revert: () =>
          update((periods) =>
            [...periods, period].sort((a, b) => a.startDate.localeCompare(b.startDate)),
          ),
        // Sprema razdoblja kakva su tada, pa se dva brisanja zaredom ne poništavaju.
        commit: () =>
          save.mutateAsync(
            (queryClient.getQueryData<Season>(key)?.periods ?? [])
              .filter((p) => p.id !== period.id)
              .map(toRequest),
          ),
        errorTitle: 'Razdoblje nije obrisano',
      })
    modals.openConfirmModal({
      title: 'Obriši razdoblje',
      children: (
        <Text size="sm">
          Razdoblje „{period.name}” bit će obrisano, a njegovi dani voze kao inače.
        </Text>
      ),
      labels: { confirm: 'Obriši', cancel: 'Odustani' },
      confirmProps: { color: 'red' },
      onConfirm: confirm,
    })
  }

  return (
    <Paper withBorder p="md">
      <Group justify="space-between" align="flex-start" mb={season.periods.length ? 'xs' : 0}>
        <div>
          <Text fw={600} size="sm">
            Razdoblja
          </Text>
          <Text size="xs" c="dimmed">
            Npr. školski raspust: radni dan za vrijeme nastave vozi kao radni dan za vrijeme
            raspusta.
          </Text>
        </div>
        <Button
          variant="default"
          size="xs"
          leftSection={<IconPlus size={14} />}
          onClick={() => setEditing('new')}
        >
          Novo razdoblje
        </Button>
      </Group>
      <Stack gap={4}>
        {season.periods.map((period) => (
          <Group key={period.id} justify="space-between" wrap="nowrap">
            <Text size="sm">
              <Text span fw={600}>
                {period.name}
              </Text>{' '}
              · {formatDate(period.startDate)} – {formatDate(period.endDate)} ·{' '}
              {name(period.replacedCalendarId)} →{' '}
              {period.mode === 'NoService' ? 'ne vozi' : name(period.calendarId)}
            </Text>
            <Group gap={2} wrap="nowrap">
              <Tooltip label="Uredi">
                <ActionIcon
                  variant="subtle"
                  size="sm"
                  onClick={() => setEditing(period)}
                  aria-label={`Uredi ${period.name}`}
                >
                  <IconPencil size={14} />
                </ActionIcon>
              </Tooltip>
              <Tooltip label="Obriši">
                <ActionIcon
                  variant="subtle"
                  size="sm"
                  color="red"
                  onClick={() => remove(period)}
                  aria-label={`Obriši ${period.name}`}
                >
                  <IconTrash size={14} />
                </ActionIcon>
              </Tooltip>
            </Group>
          </Group>
        ))}
      </Stack>

      <Modal
        opened={editing !== null}
        onClose={() => setEditing(null)}
        title={editing === 'new' ? 'Novo razdoblje' : 'Uredi razdoblje'}
        size="md"
      >
        {editing !== null && (
          <PeriodForm
            season={season}
            period={editing === 'new' ? null : editing}
            onClose={() => setEditing(null)}
          />
        )}
      </Modal>
    </Paper>
  )
}

function PeriodForm({
  season,
  period,
  onClose,
}: {
  season: Season
  period: SeasonPeriod | null
  onClose: () => void
}) {
  const save = useSavePeriods(season.id)
  // Raspust je obično tip dana bez dana u tjednu, koji vozi samo kroz razdoblja.
  const withoutWeekdays = season.dayTypes.filter((t) => !weekdays.some((d) => t[d.key]))
  const replacedDefault =
    season.dayTypes.find((t) => t.monday) ??
    season.dayTypes.find((t) => !withoutWeekdays.includes(t))

  const [name, setName] = useState(period?.name ?? '')
  const [range, setRange] = useState<DatesRangeValue<string>>([
    period?.startDate ?? null,
    period?.endDate ?? null,
  ])
  const [replaced, setReplaced] = useState<string | null>(
    period?.replacedCalendarId ?? replacedDefault?.id ?? null,
  )
  const [target, setTarget] = useState<string | null>(
    period
      ? period.mode === 'NoService'
        ? 'none'
        : period.calendarId
      : (withoutWeekdays[0]?.id ?? null),
  )
  const [from, to] = range

  const submit = () => {
    if (!from || !to || !replaced || !target) {
      return
    }
    const edited: SeasonPeriodRequest = {
      id: period?.id ?? null,
      name: name.trim(),
      startDate: from,
      endDate: to,
      replacedCalendarId: replaced,
      mode: target === 'none' ? 'NoService' : 'RunsAs',
      calendarId: target === 'none' ? null : target,
    }
    const others = season.periods.filter((p) => p.id !== period?.id).map(toRequest)
    save.mutate([...others, edited], { onSuccess: onClose })
  }

  const error = save.error instanceof ApiError ? save.error : null
  const messages = error
    ? Object.values(error.fieldErrors).flat()
    : save.isError
      ? [errorMessage(save.error)]
      : []

  return (
    <Stack gap="md">
      <TextInput
        label="Naziv"
        placeholder="npr. Zimski raspust"
        value={name}
        onChange={(event) => setName(event.currentTarget.value)}
        data-autofocus
      />
      <DatePickerInput
        type="range"
        label="Razdoblje"
        leftSection={<IconCalendar size={16} />}
        valueFormat="D. M. YYYY."
        minDate={season.startDate}
        maxDate={season.endDate}
        value={range}
        onChange={setRange}
      />
      <Select
        label="Umjesto tipa dana"
        data={season.dayTypes.map((t) => ({ value: t.id, label: t.name }))}
        value={replaced}
        onChange={(value) => setReplaced(value === null ? null : String(value))}
        allowDeselect={false}
      />
      <Select
        label="Vozi"
        data={[
          ...season.dayTypes
            .filter((t) => t.id !== replaced)
            .map((t) => ({ value: t.id, label: `kao ${t.name}` })),
          { value: 'none', label: 'ne vozi' },
        ]}
        value={target}
        onChange={(value) => setTarget(value === null ? null : String(value))}
        allowDeselect={false}
      />
      {withoutWeekdays.length === 0 && (
        <Text size="xs" c="dimmed">
          Za raspust dodaj tip dana bez dana u tjednu (npr. „Radni dan – raspust”) pod „Uredi tipove
          dana”; vozit će samo u razdobljima.
        </Text>
      )}

      {messages.length > 0 && (
        <Alert color="red" variant="light">
          <List size="sm">
            {messages.map((m) => (
              <List.Item key={m}>{m}</List.Item>
            ))}
          </List>
        </Alert>
      )}

      <Group justify="flex-end">
        <Button variant="default" onClick={onClose}>
          Odustani
        </Button>
        <Button
          onClick={submit}
          loading={save.isPending}
          disabled={!name.trim() || !from || !to || !replaced || !target}
        >
          Spremi
        </Button>
      </Group>
    </Stack>
  )
}
