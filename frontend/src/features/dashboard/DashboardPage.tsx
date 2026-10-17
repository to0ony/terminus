import {
  Alert,
  Badge,
  Group,
  Paper,
  SimpleGrid,
  Stack,
  Text,
  ThemeIcon,
  Title,
  UnstyledButton,
} from '@mantine/core'
import {
  IconAlertTriangle,
  IconBuildingCommunity,
  IconBus,
  IconCalendarCheck,
  IconMapPin,
  IconRoute,
  type Icon,
} from '@tabler/icons-react'
import { Link } from 'react-router'
import { QueryError } from '@/components/QueryError'
import { useSetupStatus } from '@/features/setup/queries'
import { formatDate, formatNumber } from '@/lib/format'
import { type ServiceOverview, useServiceOverview } from './queries'
import { validity } from './validity'

/** Pregled: koliko podataka ima, što danas vozi i do kada vrijedi vozni red. */
export function DashboardPage() {
  const setup = useSetupStatus()
  const overview = useServiceOverview()
  const counts = setup.data?.counts

  const stats: { label: string; value?: number; to: string; icon: Icon }[] = [
    { label: 'Linije', value: counts?.routes, to: '/routes', icon: IconRoute },
    { label: 'Stanice', value: counts?.stops, to: '/stops', icon: IconMapPin },
    { label: 'Vožnje', value: counts?.trips, to: '/routes', icon: IconBus },
    {
      label: 'Prijevoznici',
      value: counts?.agencies,
      to: '/agencies',
      icon: IconBuildingCommunity,
    },
  ]

  return (
    <Stack gap="lg">
      <Title order={2}>Pregled</Title>

      {overview.isError ? (
        <QueryError error={overview.error} onRetry={() => void overview.refetch()} />
      ) : (
        overview.data && <ServiceStatus overview={overview.data} />
      )}

      <SimpleGrid cols={{ base: 2, md: 4 }} spacing="md">
        {stats.map((stat) => (
          <UnstyledButton key={stat.label} component={Link} to={stat.to}>
            <Paper withBorder p="md" h="100%">
              <Group justify="space-between" align="flex-start" wrap="nowrap">
                <div>
                  <Text size="sm" c="dimmed">
                    {stat.label}
                  </Text>
                  <Text fw={700} fz={28} lh={1.2}>
                    {stat.value === undefined ? '–' : formatNumber(stat.value)}
                  </Text>
                </div>
                <ThemeIcon variant="light" size="lg">
                  <stat.icon size={20} stroke={1.6} />
                </ThemeIcon>
              </Group>
            </Paper>
          </UnstyledButton>
        ))}
      </SimpleGrid>
    </Stack>
  )
}

function ServiceStatus({ overview }: { overview: ServiceOverview }) {
  const status = validity(overview)

  return (
    <Stack gap="md">
      {status.state === 'expired' && (
        <Alert
          color="red"
          variant="light"
          icon={<IconAlertTriangle />}
          title="Vozni red je istekao"
        >
          Od danas nijedan kalendar ne vozi. Uvezi novi GTFS feed na stranici GTFS uvoz i izvoz.
        </Alert>
      )}
      {status.state === 'expiring' && (
        <Alert
          color="yellow"
          variant="light"
          icon={<IconAlertTriangle />}
          title="Vozni red uskoro istječe"
        >
          Zadnji dan s voznim redom je {formatDate(status.lastDate)}
          {status.daysLeft === 0 ? ' (danas)' : ` (još ${daysText(status.daysLeft)})`}. Pripremi i
          uvezi novi feed na vrijeme.
        </Alert>
      )}

      <SimpleGrid cols={{ base: 1, sm: 2 }} spacing="md">
        <Paper withBorder p="md">
          <Group gap="xs" mb="xs">
            <IconCalendarCheck size={18} stroke={1.6} />
            <Text fw={600} size="sm">
              Danas, {formatDate(overview.date)}
            </Text>
          </Group>
          {overview.activeCalendars.length === 0 ? (
            <Text size="sm" c="dimmed">
              Danas ne vozi nijedan kalendar.
            </Text>
          ) : (
            <Group gap={6}>
              {overview.activeCalendars.map((calendar) => (
                <Badge
                  key={calendar.id}
                  component={Link}
                  to={`/calendars/${calendar.id}`}
                  variant="light"
                  tt="none"
                  style={{ cursor: 'pointer' }}
                >
                  {calendar.name}
                </Badge>
              ))}
            </Group>
          )}
        </Paper>
        <Paper withBorder p="md">
          <Text fw={600} size="sm" mb="xs">
            Vozni red vrijedi do
          </Text>
          {status.state === 'expired' ? (
            <Text size="sm" c="red">
              Istekao
            </Text>
          ) : (
            <Group gap="xs">
              <Text size="sm">{formatDate(status.lastDate)}</Text>
              <Badge
                variant="light"
                color={status.state === 'expiring' ? 'yellow' : 'green'}
                tt="none"
              >
                {status.daysLeft === 0 ? 'danas' : `još ${daysText(status.daysLeft)}`}
              </Badge>
            </Group>
          )}
        </Paper>
      </SimpleGrid>
    </Stack>
  )
}

// Hrvatska množina: 1 dan, 2–4 dana, 5+ dana; 21 dan, 22 dana, 11–14 dana.
function daysText(days: number) {
  const lastTwo = days % 100
  const last = days % 10
  const unit = last === 1 && lastTwo !== 11 ? 'dan' : 'dana'
  return `${formatNumber(days)} ${unit}`
}
