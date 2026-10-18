import {
  ActionIcon,
  Alert,
  Badge,
  Button,
  Group,
  Paper,
  Stack,
  Table,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { modals } from '@mantine/modals'
import { notifications } from '@mantine/notifications'
import {
  IconArrowsExchange,
  IconCopy,
  IconPencil,
  IconPlus,
  IconTrash,
  IconWand,
} from '@tabler/icons-react'
import { Link, useNavigate } from 'react-router'
import { errorMessage } from '@/api/errors'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { formatNumber } from '@/lib/format'
import { useDeleteVariant, useDeriveVariants, useRouteVariants, type Variant } from './queries'
import { duration } from './variantMath'

interface VariantsSectionProps {
  routeId: string
  /** Linija već ima vožnji, pa se varijante mogu prepoznati iz njih. */
  hasTrips: boolean
}

/**
 * Varijante linije po smjeru: redoslijed stanica s vremenima vožnje, predložak
 * za vožnje u grafikonu. Uređuju se na karti.
 */
export function VariantsSection({ routeId, hasTrips }: VariantsSectionProps) {
  const navigate = useNavigate()
  const variants = useRouteVariants(routeId)
  const remove = useDeleteVariant(routeId)
  const derive = useDeriveVariants(routeId)

  const confirmDelete = (variant: Variant) =>
    modals.openConfirmModal({
      title: 'Obriši varijantu',
      children: <Text size="sm">Varijanta „{variant.name}” bit će obrisana.</Text>,
      labels: { confirm: 'Obriši', cancel: 'Odustani' },
      confirmProps: { color: 'red' },
      onConfirm: () =>
        remove.mutate(variant.id, {
          onError: (error) =>
            notifications.show({
              color: 'red',
              title: 'Brisanje nije uspjelo',
              message: errorMessage(error),
            }),
        }),
    })

  return (
    <Paper withBorder p="md">
      <Stack gap="sm">
        <Group justify="space-between" align="flex-start">
          <div>
            <Title order={3}>Varijante</Title>
            <Text size="sm" c="dimmed">
              Redoslijed stanica po smjeru s vremenima vožnje; po njima nastaju vožnje u grafikonu.
            </Text>
          </div>
          <Button
            leftSection={<IconPlus size={16} />}
            onClick={() => void navigate(`/routes/${routeId}/variants/new`)}
          >
            Nova varijanta
          </Button>
        </Group>

        {variants.isPending ? (
          <PageLoader />
        ) : variants.isError ? (
          <QueryError error={variants.error} onRetry={() => void variants.refetch()} />
        ) : variants.data.length === 0 ? (
          <Alert variant="light" color="blue">
            <Stack gap="xs" align="flex-start">
              <Text size="sm">
                Linija još nema varijanti. Napravi varijantu odabirom stanica na karti
                {hasTrips ? ' ili je prepoznaj iz postojećih vožnji.' : '.'}
              </Text>
              {hasTrips && (
                <Button
                  size="xs"
                  variant="light"
                  leftSection={<IconWand size={14} />}
                  loading={derive.isPending}
                  onClick={() => derive.mutate()}
                >
                  Prepoznaj iz vožnji
                </Button>
              )}
            </Stack>
          </Alert>
        ) : (
          <Table.ScrollContainer minWidth={640}>
            <Table verticalSpacing="xs">
              <Table.Thead>
                <Table.Tr>
                  <Table.Th>Varijanta</Table.Th>
                  <Table.Th w={140}>Smjer</Table.Th>
                  <Table.Th w={90}>Stanica</Table.Th>
                  <Table.Th w={90}>Trajanje</Table.Th>
                  <Table.Th w={90}>Vožnji</Table.Th>
                  <Table.Th w={110} />
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>
                {variants.data.map((variant) => (
                  <Table.Tr key={variant.id}>
                    <Table.Td>
                      <Group gap={6} wrap="nowrap">
                        <Text
                          component={Link}
                          to={`/routes/${routeId}/variants/${variant.id}`}
                          size="sm"
                          fw={500}
                        >
                          {variant.name}
                        </Text>
                        {variant.sortOrder === 0 && (
                          <Badge size="xs" variant="light">
                            glavna
                          </Badge>
                        )}
                        {variant.source === 'Gtfs' && (
                          <Badge size="xs" variant="outline" color="gray">
                            GTFS
                          </Badge>
                        )}
                      </Group>
                    </Table.Td>
                    <Table.Td>
                      <Text size="sm" c="dimmed">
                        {variant.headsign ? `→ ${variant.headsign}` : '—'}
                      </Text>
                    </Table.Td>
                    <Table.Td>{variant.stops.length}</Table.Td>
                    <Table.Td>{Math.round(duration(variant) / 60)} min</Table.Td>
                    <Table.Td>{formatNumber(variant.tripCount)}</Table.Td>
                    <Table.Td>
                      <Group gap={4} justify="flex-end" wrap="nowrap">
                        <Tooltip label="Uredi">
                          <ActionIcon
                            variant="subtle"
                            component={Link}
                            to={`/routes/${routeId}/variants/${variant.id}`}
                            aria-label="Uredi varijantu"
                          >
                            <IconPencil size={16} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Kopiraj u novu varijantu">
                          <ActionIcon
                            variant="subtle"
                            component={Link}
                            to={`/routes/${routeId}/variants/new?from=${variant.id}`}
                            aria-label="Kopiraj varijantu"
                          >
                            <IconCopy size={16} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Napravi povratni smjer">
                          <ActionIcon
                            variant="subtle"
                            component={Link}
                            to={`/routes/${routeId}/variants/new?reverse=${variant.id}`}
                            aria-label="Napravi povratni smjer"
                          >
                            <IconArrowsExchange size={16} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label={variant.tripCount > 0 ? 'Koriste je vožnje' : 'Obriši'}>
                          <ActionIcon
                            variant="subtle"
                            color="red"
                            disabled={variant.tripCount > 0}
                            onClick={() => confirmDelete(variant)}
                            aria-label="Obriši varijantu"
                          >
                            <IconTrash size={16} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </Table.ScrollContainer>
        )}
      </Stack>
    </Paper>
  )
}
