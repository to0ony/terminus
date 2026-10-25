import {
  ActionIcon,
  Anchor,
  Badge,
  Button,
  Group,
  Menu,
  Paper,
  SegmentedControl,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { DatePickerInput } from '@mantine/dates'
import { useDisclosure } from '@mantine/hooks'
import {
  IconArrowLeft,
  IconCalendar,
  IconChartLine,
  IconDots,
  IconFileTypePdf,
  IconPencil,
  IconTrash,
} from '@tabler/icons-react'
import { Link, useParams, useSearchParams } from 'react-router'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { useAgencies } from '@/features/agencies/queries'
import { formatNumber } from '@/lib/format'
import { directionOptions } from './directions'
import { type TimetableParams, useRoute, useRoutePatterns, useRouteTimetable } from './queries'
import { RouteBadge } from './RouteBadge'
import { useRouteVariants } from '@/features/variants/queries'
import { VariantsSection } from '@/features/variants/VariantsSection'
import { DeleteRouteDialog } from './DeleteRouteDialog'
import { RouteDiagramPdfDialog } from './RouteDiagramPdfDialog'
import { RouteDialog } from './RouteDialog'
import { routeTypeLabel } from './routeTypes'
import { TimetableGrid } from './TimetableGrid'

/** Linija i njezin vozni red za odabrani dan i smjer (?date=YYYY-MM-DD&dir=0|1). */
export function RouteDetailPage() {
  const { routeId = '' } = useParams()
  const [searchParams, setSearchParams] = useSearchParams()

  const route = useRoute(routeId)
  const patterns = useRoutePatterns(routeId)
  const agencies = useAgencies()
  const variants = useRouteVariants(routeId)
  const [editOpened, edit] = useDisclosure(false)
  const [deleteOpened, deleteDialog] = useDisclosure(false)
  const [pdfOpened, pdf] = useDisclosure(false)

  const directions = directionOptions(patterns.data ?? [])
  const direction =
    directions.find((d) => d.value === searchParams.get('dir'))?.value ?? directions[0]?.value
  const date = searchParams.get('date')

  const params: TimetableParams = {
    Date: date ?? undefined,
    DirectionId: direction === undefined ? undefined : Number(direction),
  }
  // Smjer ovisi o obrascima, pa se vozni red traži tek kad su učitani.
  const timetable = useRouteTimetable(routeId, params, patterns.isSuccess)

  const setParam = (key: string, value: string | null) =>
    setSearchParams(
      (current) => {
        if (value) {
          current.set(key, value)
        } else {
          current.delete(key)
        }
        return current
      },
      { replace: true },
    )

  if (route.isPending) {
    return <PageLoader />
  }
  if (route.isError) {
    return <QueryError error={route.error} onRetry={() => void route.refetch()} />
  }

  const agency = agencies.data?.find((a) => a.id === route.data.agencyId)

  return (
    <Stack gap="md">
      <Anchor component={Link} to="/routes" size="sm">
        <Group gap={4}>
          <IconArrowLeft size={14} />
          Linije
        </Group>
      </Anchor>

      <Group justify="space-between" align="flex-start">
        <Stack gap={4}>
          <Group gap="sm" wrap="nowrap" align="center">
            <RouteBadge route={route.data} size="lg" />
            <Title order={2}>
              {route.data.longName ?? route.data.shortName ?? route.data.code}
            </Title>
          </Group>
          <Group gap="xs">
            <Text size="sm" c="dimmed">
              {routeTypeLabel(route.data.routeType)}
              {agency && ` · ${agency.name}`}
            </Text>
            {!route.data.isActive && (
              <Badge variant="light" color="gray">
                Neaktivna
              </Badge>
            )}
          </Group>
          {route.data.description && <Text size="sm">{route.data.description}</Text>}
        </Stack>
        <Group gap="sm">
          <Button variant="default" leftSection={<IconPencil size={16} />} onClick={edit.open}>
            Uredi liniju
          </Button>
          <Button
            component={Link}
            to={`/routes/${routeId}/schedule${direction === undefined ? '' : `?dir=${direction}`}`}
            leftSection={<IconChartLine size={16} />}
          >
            Grafikon i vožnje
          </Button>
          <Menu position="bottom-end" withinPortal>
            <Menu.Target>
              <ActionIcon variant="default" size={36} aria-label="Ostale radnje">
                <IconDots size={16} />
              </ActionIcon>
            </Menu.Target>
            <Menu.Dropdown>
              <Menu.Item
                leftSection={<IconFileTypePdf size={14} />}
                // Shema se crta iz varijanti, pa bez stanica nema što ispisati.
                disabled={!variants.data?.some((v) => v.stops.length > 0)}
                onClick={pdf.open}
              >
                PDF shema linije
              </Menu.Item>
              <Menu.Divider />
              <Menu.Item
                color="red"
                leftSection={<IconTrash size={14} />}
                // Broj varijanti i vožnji mora biti poznat prije potvrde.
                disabled={!variants.isSuccess || !patterns.isSuccess}
                onClick={deleteDialog.open}
              >
                Obriši liniju…
              </Menu.Item>
            </Menu.Dropdown>
          </Menu>
        </Group>
      </Group>
      <RouteDialog route={route.data} opened={editOpened} onClose={edit.close} />
      <RouteDiagramPdfDialog route={route.data} opened={pdfOpened} onClose={pdf.close} />
      <DeleteRouteDialog
        route={route.data}
        opened={deleteOpened}
        onClose={deleteDialog.close}
        variantCount={variants.data?.length ?? 0}
        tripCount={(patterns.data ?? []).reduce((sum, p) => sum + p.tripCount, 0)}
      />

      <VariantsSection routeId={routeId} hasTrips={(patterns.data?.length ?? 0) > 0} />

      <Title order={3}>Vozni red po danu</Title>

      <Group gap="sm" align="flex-end">
        <DatePickerInput
          label="Dan"
          leftSection={<IconCalendar size={16} />}
          value={date ?? timetable.data?.date ?? null}
          onChange={(value) => setParam('date', value)}
          valueFormat="dddd, D. M. YYYY."
          w={{ base: '100%', xs: 240 }}
        />
        {directions.length > 0 && (
          <SegmentedControl
            data={directions}
            value={direction}
            onChange={(value) => setParam('dir', value)}
          />
        )}
      </Group>

      {patterns.isError ? (
        <QueryError error={patterns.error} onRetry={() => void patterns.refetch()} />
      ) : timetable.isError ? (
        <QueryError error={timetable.error} onRetry={() => void timetable.refetch()} />
      ) : !timetable.data ? (
        <PageLoader />
      ) : timetable.data.trips.length === 0 ? (
        <Paper withBorder p="xl">
          <Text c="dimmed" ta="center">
            Linija taj dan ne vozi{directions.length > 0 && ' u ovom smjeru'}.
          </Text>
        </Paper>
      ) : (
        <Stack gap="xs" style={{ opacity: timetable.isPlaceholderData ? 0.6 : 1 }}>
          <Group gap="xs">
            <Text size="sm" c="dimmed">
              {formatNumber(timetable.data.trips.length)} vožnji · kalendari:
            </Text>
            {timetable.data.calendars.map((calendar) => (
              <Badge key={calendar.id} variant="light" tt="none" title={calendar.name ?? undefined}>
                {calendar.code}
              </Badge>
            ))}
          </Group>
          <TimetableGrid timetable={timetable.data} />
        </Stack>
      )}
    </Stack>
  )
}
