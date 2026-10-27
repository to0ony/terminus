import {
  Badge,
  Box,
  Button,
  Grid,
  Group,
  Paper,
  ScrollArea,
  Stack,
  Text,
  Title,
  UnstyledButton,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { modals } from '@mantine/modals'
import { notifications } from '@mantine/notifications'
import { IconCopy, IconPencil, IconTrash } from '@tabler/icons-react'
import { useState } from 'react'
import { useNavigate, useParams } from 'react-router'
import { errorMessage } from '@/api/errors'
import { PageBreadcrumbs } from '@/components/PageBreadcrumbs'
import { DetailSkeleton } from '@/components/Skeletons'
import { QueryError } from '@/components/QueryError'
import { formatDate, formatNumber } from '@/lib/format'
import { DayDialog } from './DayDialog'
import { DayTypesDialog } from './DayTypesDialog'
import { CopySeasonDialog } from './CopySeasonDialog'
import { EditSeasonDialog } from './EditSeasonDialog'
import { type Season, useDeleteSeason, useSeason } from './queries'
import { SeasonMonths } from './SeasonMonths'
import { SeasonPeriods } from './SeasonPeriods'
import { dayModeLabel, dayTypeColor, weekdayLabel, weekdayOf, weekdays } from './seasonDays'

const regionNames = new Intl.DisplayNames(['hr'], { type: 'region' })

/**
 * Sezona voznog reda: tipovi dana, pravilo za praznike i kalendar svih
 * mjeseci. Klik na dan mijenja kako taj dan vozi.
 */
export function SeasonPage() {
  const { seasonId = '' } = useParams()
  const navigate = useNavigate()
  const season = useSeason(seasonId)
  const remove = useDeleteSeason()
  const [selectedDate, setSelectedDate] = useState<string | null>(null)
  const [editOpened, edit] = useDisclosure(false)
  const [typesOpened, types] = useDisclosure(false)
  const [copyOpened, copy] = useDisclosure(false)

  if (season.isPending) {
    return <DetailSkeleton />
  }
  if (season.isError) {
    return <QueryError error={season.error} onRetry={() => void season.refetch()} />
  }

  const data = season.data
  const tripCount = data.dayTypes.reduce((sum, t) => sum + t.tripCount, 0)

  const confirmDelete = () =>
    modals.openConfirmModal({
      title: 'Obriši sezonu',
      children:
        tripCount > 0 ? (
          <Text size="sm">
            Sezona „{data.name}”, njezini tipovi dana i iznimke bit će obrisani, zajedno s{' '}
            <b>{formatNumber(tripCount)} vožnji</b> koje ih koriste. Ovo se ne može poništiti.
          </Text>
        ) : (
          <Text size="sm">
            Sezona „{data.name}”, njezini tipovi dana i iznimke bit će obrisani.
          </Text>
        ),
      labels: { confirm: tripCount > 0 ? 'Obriši s vožnjama' : 'Obriši', cancel: 'Odustani' },
      confirmProps: { color: 'red' },
      onConfirm: () =>
        remove.mutate(
          { id: data.id, withTrips: tripCount > 0 },
          {
            onSuccess: () => void navigate('/calendars'),
            onError: (error) =>
              notifications.show({
                color: 'red',
                title: 'Brisanje nije uspjelo',
                message: errorMessage(error),
              }),
          },
        ),
    })

  return (
    <Stack gap="md">
      <PageBreadcrumbs items={[{ label: 'Kalendari', to: '/calendars' }, { label: data.name }]} />

      <Group justify="space-between" align="flex-start">
        <Stack gap={4}>
          <Title order={2}>{data.name}</Title>
          <Text size="sm" c="dimmed">
            {formatDate(data.startDate)} – {formatDate(data.endDate)} · {holidayText(data)}
          </Text>
        </Stack>
        <Group gap="sm">
          <Button variant="default" leftSection={<IconPencil size={16} />} onClick={edit.open}>
            Uredi
          </Button>
          <Button variant="default" leftSection={<IconCopy size={16} />} onClick={copy.open}>
            Kopiraj
          </Button>
          <Button
            variant="subtle"
            color="red"
            leftSection={<IconTrash size={16} />}
            onClick={confirmDelete}
          >
            Obriši
          </Button>
        </Group>
      </Group>

      <Paper withBorder p="md">
        <Group justify="space-between" align="flex-start" gap="md">
          <Group gap="lg">
            {data.dayTypes.map((t, index) => (
              <Group key={t.id} gap={8} wrap="nowrap">
                <Box w={16} h={16} style={swatch(dayTypeColor(index))} />
                <div>
                  <Text size="sm" fw={600} lh={1.2}>
                    {t.name}
                  </Text>
                  <Text size="xs" c="dimmed">
                    {weekdayLabel(t)} · {formatNumber(t.tripCount)} vožnji
                  </Text>
                </div>
              </Group>
            ))}
          </Group>
          <Button variant="default" size="xs" onClick={types.open}>
            Uredi tipove dana
          </Button>
        </Group>
      </Paper>

      <SeasonPeriods season={data} />

      <Grid gap="md">
        <Grid.Col span={{ base: 12, lg: 9 }}>
          <Stack gap="xs">
            <Text size="sm" c="dimmed">
              Klikni na dan da promijeniš kako vozi. ★ praznik · uokviren dan je ručna iznimka ·
              prekriženo ne vozi.
            </Text>
            <SeasonMonths season={data} onDayClick={setSelectedDate} />
          </Stack>
        </Grid.Col>
        <Grid.Col span={{ base: 12, lg: 3 }}>
          <Exceptions season={data} onSelect={setSelectedDate} />
        </Grid.Col>
      </Grid>

      <DayDialog season={data} date={selectedDate} onClose={() => setSelectedDate(null)} />
      <EditSeasonDialog season={data} opened={editOpened} onClose={edit.close} />
      <CopySeasonDialog season={data} opened={copyOpened} onClose={copy.close} />
      <DayTypesDialog season={data} opened={typesOpened} onClose={types.close} />
    </Stack>
  )
}

/**
 * Dani koji odstupaju, da se na jednom mjestu vidi što je drukčije. Dani
 * razdoblja su u popisu razdoblja, pa ovdje nisu.
 */
function Exceptions({ season, onSelect }: { season: Season; onSelect: (date: string) => void }) {
  const days = season.days.filter((d) => d.source !== 'Period')
  return (
    <Paper withBorder p="md" pos={{ lg: 'sticky' }} top={80}>
      <Group justify="space-between" mb="xs">
        <Text fw={600} size="sm">
          Iznimke
        </Text>
        <Badge variant="light" color="gray">
          {days.length}
        </Badge>
      </Group>
      {days.length === 0 ? (
        <Text size="sm" c="dimmed">
          Nema iznimki. Klikni na dan u kalendaru da je dodaš.
        </Text>
      ) : (
        <ScrollArea.Autosize mah={560}>
          <Stack gap={2}>
            {days.map((day) => (
              <UnstyledButton
                key={day.date}
                onClick={() => onSelect(day.date)}
                p={6}
                style={{ borderRadius: 'var(--mantine-radius-sm)' }}
                className="mantine-focus-auto"
              >
                <Group gap={8} wrap="nowrap" align="flex-start">
                  <Text size="sm" w={16} c={day.source === 'Holiday' ? 'yellow' : 'dimmed'}>
                    {day.source === 'Holiday' ? '★' : '•'}
                  </Text>
                  <div style={{ minWidth: 0 }}>
                    <Text size="sm">
                      {shortDate(day.date)} → {dayModeLabel(season, day)}
                    </Text>
                    {day.note && (
                      <Text size="xs" c="dimmed" truncate>
                        {day.note}
                      </Text>
                    )}
                  </div>
                </Group>
              </UnstyledButton>
            ))}
          </Stack>
        </ScrollArea.Autosize>
      )}
    </Paper>
  )
}

function holidayText(season: Season): string {
  if (!season.holidayCountry) {
    return 'bez automatskih praznika'
  }
  const country = regionNames.of(season.holidayCountry) ?? season.holidayCountry
  const rule =
    season.holidayMode === 'NoService'
      ? 'ne vozi'
      : season.holidayMode === 'RunsAs'
        ? `vozi kao ${season.dayTypes.find((t) => t.id === season.holidayCalendarId)?.name ?? '?'}`
        : 'vozi normalno'
  return `praznici: ${country}, ${rule}`
}

/** "Pet 25. 12." */
function shortDate(date: string): string {
  const weekday = weekdays.find((d) => d.key === weekdayOf(date))?.short ?? ''
  return `${weekday} ${Number(date.slice(8))}. ${Number(date.slice(5, 7))}.`
}

/** Kao dan u kalendaru: svijetla boja tipa dana. */
function swatch(color: string) {
  return {
    borderRadius: 4,
    background: `var(--mantine-color-${color}-light)`,
    border: `2px solid var(--mantine-color-${color}-filled)`,
  }
}
