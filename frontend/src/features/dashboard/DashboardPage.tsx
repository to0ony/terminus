import {
  Anchor,
  Badge,
  Group,
  List,
  Loader,
  Paper,
  SimpleGrid,
  Skeleton,
  Stack,
  Text,
  ThemeIcon,
  Title,
  Tooltip,
  UnstyledButton,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { notifications } from '@mantine/notifications'
import {
  IconAlertTriangle,
  IconBuildingCommunity,
  IconCalendarCheck,
  IconCircleCheck,
  IconFileTypePdf,
  IconFileZip,
  IconMap2,
  IconMapPin,
  IconMapPinPlus,
  IconRoute,
  type Icon,
} from '@tabler/icons-react'
import dayjs from 'dayjs'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import { QueryError } from '@/components/QueryError'
import { useExportGtfs, useGtfsCheck } from '@/features/gtfs/queries'
import { type RouteTableRow, useRouteTable } from '@/features/routes/queries'
import { RouteBadge } from '@/features/routes/RouteBadge'
import { RouteDialog } from '@/features/routes/RouteDialog'
import { useSetupStatus } from '@/features/setup/queries'
import { saveFile } from '@/lib/download'
import { formatDate, formatNumber } from '@/lib/format'
import { type ServiceOverview, useServiceOverview } from './queries'
import { expiryWarningDays, validity } from './validity'

/** Koliko problema GTFS-a se vidi na pregledu; ostali su na stranici GTFS. */
const shownProblems = 5

/**
 * Pregled: što treba popraviti (GTFS provjere, istek voznog reda i linija),
 * brze akcije i kratki sažetak (linije, stanice, prijevoznici, što danas vozi).
 */
export function DashboardPage() {
  return (
    <Stack gap="lg">
      <Title order={2}>Pregled</Title>
      <Health />
      <QuickActions />
      <Summary />
    </Stack>
  )
}

/** Problemi zbog kojih feed nije spreman ili vozni red istječe; inače jedan zeleni redak. */
function Health() {
  const check = useGtfsCheck()
  const overview = useServiceOverview()
  // Linije kojima prvima istječe vozni red; dovoljno je prvih nekoliko.
  const routes = useRouteTable({ SortBy: 'ValidTo', IsActive: true, HasTrips: true, PageSize: 5 })

  if (check.isError) {
    return <QueryError error={check.error} onRetry={() => void check.refetch()} />
  }
  if (overview.isError) {
    return <QueryError error={overview.error} onRetry={() => void overview.refetch()} />
  }
  if (!check.data || !overview.data || routes.isPending) {
    return <Skeleton h={56} radius="md" />
  }

  const problems = check.data
  const status = validity(overview.data)
  const today = dayjs(overview.data.date)
  const ending = (routes.data?.items ?? []).filter(
    (row) => row.validTo && dayjs(row.validTo).diff(today, 'day') < expiryWarningDays,
  )

  if (problems.length === 0 && status.state === 'valid' && ending.length === 0) {
    return (
      <Paper withBorder p="sm" px="md">
        <Group gap="sm" wrap="nowrap">
          <ThemeIcon variant="light" color="green" radius="xl">
            <IconCircleCheck size={18} />
          </ThemeIcon>
          <Text size="sm">
            Sve je u redu: GTFS feed je spreman za izvoz, a vozni red vrijedi do{' '}
            {formatDate(status.lastDate)}.
          </Text>
        </Group>
      </Paper>
    )
  }

  return (
    <Stack gap="sm">
      {problems.length > 0 && (
        <Issue color="red" title="GTFS feed se ne može izvesti">
          <List size="sm" spacing={4}>
            {problems.slice(0, shownProblems).map((problem) => (
              <List.Item key={problem}>{problem}</List.Item>
            ))}
          </List>
          <Anchor component={Link} to="/gtfs" size="sm" mt={6} display="inline-block">
            {problems.length > shownProblems
              ? `I još ${problems.length - shownProblems} na stranici GTFS uvoz i izvoz`
              : 'GTFS uvoz i izvoz'}
          </Anchor>
        </Issue>
      )}
      {status.state === 'expired' && (
        <Issue color="red" title="Vozni red je istekao">
          <Text size="sm">
            Od danas nijedan kalendar ne vozi. Dodaj novu sezonu u kalendarima ili uvezi novi feed.
          </Text>
        </Issue>
      )}
      {status.state === 'expiring' && (
        <Issue color="yellow" title="Vozni red uskoro istječe">
          <Text size="sm">
            Zadnji dan s voznim redom je {formatDate(status.lastDate)}
            {status.daysLeft === 0 ? ' (danas)' : ` (još ${daysText(status.daysLeft)})`}.
          </Text>
        </Issue>
      )}
      {status.state === 'valid' && ending.length > 0 && (
        <EndingRoutes rows={ending} today={today} />
      )}
    </Stack>
  )
}

/** Linije kojima je vozni red istekao ili istječe, a cijeli sustav još vozi. */
function EndingRoutes({ rows, today }: { rows: RouteTableRow[]; today: dayjs.Dayjs }) {
  return (
    <Issue color="yellow" title="Linije kojima istječe vozni red">
      <Stack gap={6}>
        {rows.map(({ route, validTo }) => (
          <Group key={route.id} gap="xs" wrap="nowrap">
            <RouteBadge route={route} size="sm" />
            <Anchor component={Link} to={`/routes/${route.id}`} size="sm" truncate>
              {route.longName ?? route.code}
            </Anchor>
            <Text size="sm" c="dimmed" style={{ whiteSpace: 'nowrap' }}>
              {dayjs(validTo).isBefore(today) ? 'isteklo' : 'do'} {formatDate(validTo!)}
            </Text>
          </Group>
        ))}
        <Anchor component={Link} to="/routes?sort=ValidTo" size="sm">
          Sve linije po isteku voznog reda
        </Anchor>
      </Stack>
    </Issue>
  )
}

function Issue({
  color,
  title,
  children,
}: {
  color: 'red' | 'yellow'
  title: string
  children: ReactNode
}) {
  return (
    <Paper withBorder p="md" style={{ borderLeft: `4px solid var(--mantine-color-${color}-6)` }}>
      <Group gap="xs" mb={6} wrap="nowrap">
        <IconAlertTriangle size={18} color={`var(--mantine-color-${color}-6)`} />
        <Text fw={600} size="sm">
          {title}
        </Text>
      </Group>
      {children}
    </Paper>
  )
}

/** Najčešći poslovi jednim klikom. */
function QuickActions() {
  const [routeOpened, routeDialog] = useDisclosure(false)
  const check = useGtfsCheck()
  const exportFeed = useExportGtfs()
  const blocked = (check.data?.length ?? 0) > 0

  const exportGtfs = () =>
    exportFeed.mutate(undefined, {
      onSuccess: saveFile,
      onError: (error) =>
        notifications.show({
          color: 'red',
          title: 'Izvoz nije uspio',
          message: errorMessage(error),
        }),
    })

  return (
    <Stack gap="xs">
      <Text fw={600} size="sm">
        Brze akcije
      </Text>
      <SimpleGrid cols={{ base: 2, sm: 3, lg: 5 }} spacing="sm">
        <Action icon={IconRoute} label="Nova linija" onClick={routeDialog.open} />
        <Action icon={IconMapPinPlus} label="Nova stanica" to="/stops?add=1" />
        <Action
          icon={IconFileZip}
          label="Izvezi GTFS"
          onClick={exportGtfs}
          loading={exportFeed.isPending}
          disabled={blocked ? 'Prvo riješi probleme GTFS-a' : undefined}
        />
        <Action
          icon={IconFileTypePdf}
          label="PDF stanica"
          hint="Odaberi stanice u tablici"
          to="/stops"
        />
        <Action icon={IconMap2} label="Karta uživo" to="/live" />
      </SimpleGrid>
      <RouteDialog opened={routeOpened} onClose={routeDialog.close} />
    </Stack>
  )
}

interface ActionProps {
  icon: Icon
  label: string
  hint?: string
  to?: string
  onClick?: () => void
  loading?: boolean
  /** Razlog zašto akcija trenutno ne radi. */
  disabled?: string
}

function Action({ icon: ActionIcon, label, hint, to, onClick, loading, disabled }: ActionProps) {
  const content = (
    <Paper withBorder p="sm" h="100%" style={{ opacity: disabled ? 0.5 : 1 }}>
      <Group gap="sm" wrap="nowrap">
        <ThemeIcon variant="light" size="lg" radius="md">
          {loading ? <Loader size={18} /> : <ActionIcon size={20} stroke={1.6} />}
        </ThemeIcon>
        <div style={{ minWidth: 0 }}>
          <Text size="sm" fw={500}>
            {label}
          </Text>
          {hint && (
            <Text size="xs" c="dimmed" truncate>
              {hint}
            </Text>
          )}
        </div>
      </Group>
    </Paper>
  )

  if (disabled) {
    return (
      <Tooltip label={disabled}>
        <div>{content}</div>
      </Tooltip>
    )
  }
  return to ? (
    <UnstyledButton component={Link} to={to}>
      {content}
    </UnstyledButton>
  ) : (
    <UnstyledButton onClick={onClick} disabled={loading}>
      {content}
    </UnstyledButton>
  )
}

/** Koliko čega ima i što danas vozi. */
function Summary() {
  const setup = useSetupStatus()
  const overview = useServiceOverview()
  const counts = setup.data?.counts

  const stats: { label: string; value?: number; to: string; icon: Icon }[] = [
    { label: 'Linije', value: counts?.routes, to: '/routes', icon: IconRoute },
    { label: 'Stanice', value: counts?.stops, to: '/stops', icon: IconMapPin },
    {
      label: 'Prijevoznici',
      value: counts?.agencies,
      to: '/agencies',
      icon: IconBuildingCommunity,
    },
  ]

  return (
    <SimpleGrid cols={{ base: 1, sm: 2, lg: 4 }} spacing="md">
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
      {overview.data && <Today overview={overview.data} />}
    </SimpleGrid>
  )
}

/** Kalendari koji danas voze i do kada vrijedi vozni red. */
function Today({ overview }: { overview: ServiceOverview }) {
  const status = validity(overview)
  return (
    <Paper withBorder p="md" h="100%">
      <Group gap="xs" mb="xs" wrap="nowrap">
        <IconCalendarCheck size={18} stroke={1.6} />
        <Text fw={600} size="sm">
          Danas, {formatDate(overview.date)}
        </Text>
      </Group>
      {overview.activeCalendars.length === 0 ? (
        <Text size="sm" c="dimmed">
          Ne vozi nijedan kalendar.
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
      {status.state !== 'expired' && (
        <Text size="xs" c="dimmed" mt="xs">
          Vozni red vrijedi do {formatDate(status.lastDate)}
        </Text>
      )}
    </Paper>
  )
}

// Hrvatska množina: 1 dan, 2–4 dana, 5+ dana; 21 dan, 22 dana, 11–14 dana.
function daysText(days: number) {
  const lastTwo = days % 100
  const last = days % 10
  const unit = last === 1 && lastTwo !== 11 ? 'dan' : 'dana'
  return `${formatNumber(days)} ${unit}`
}
