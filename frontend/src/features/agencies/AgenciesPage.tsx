import {
  ActionIcon,
  Alert,
  Anchor,
  Badge,
  Button,
  Group,
  Paper,
  SimpleGrid,
  Stack,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { IconAlertTriangle, IconPencil, IconPlus, IconTrash } from '@tabler/icons-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { useRoutes } from '@/features/routes/queries'
import { useSettings } from '@/features/settings/queries'
import { formatNumber } from '@/lib/format'
import { AgencyDialog, DeleteAgencyDialog } from './AgencyDialogs'
import { type Agency, useAgencies } from './queries'

/**
 * Prijevoznici (GTFS agency) s kontaktima i brojem linija. Linije može voziti više
 * prijevoznika (npr. konzorcij u gradskom prijevozu): svaka linija ima svog.
 */
export function AgenciesPage() {
  const agencies = useAgencies()
  const settings = useSettings()
  const [newOpened, newAgency] = useDisclosure(false)
  const [editing, setEditing] = useState<Agency | null>(null)
  const [deleting, setDeleting] = useState<{ agency: Agency; routeCount: number } | null>(null)

  // GTFS: svi prijevoznici imaju istu vremensku zonu.
  const timezone = agencies.data?.[0]?.timezone
  // Bez naziva u Postavkama javne stranice prikazuju naziv prvog unesenog prijevoznika.
  const missingDisplayName =
    (agencies.data?.length ?? 0) > 1 && settings.data !== undefined && !settings.data.displayName

  return (
    <Stack gap="md">
      <Group justify="space-between" align="flex-end">
        <Title order={2}>Prijevoznici</Title>
        <Button leftSection={<IconPlus size={16} />} onClick={newAgency.open}>
          Novi prijevoznik
        </Button>
      </Group>
      <Text size="sm" c="dimmed" maw={720}>
        Ako linije vozi više tvrtki, npr. konzorcij u gradskom prijevozu, dodaj svaku kao
        prijevoznika i na liniji odaberi koji je vozi. GTFS izvoz navodi sve prijevoznike, a na
        zaslonima stanica i u PDF voznim redovima piše naziv iz{' '}
        <Anchor component={Link} to="/settings" size="sm">
          Postavki
        </Anchor>
        .
      </Text>
      {missingDisplayName && (
        <Alert
          color="yellow"
          variant="light"
          icon={<IconAlertTriangle />}
          title="Upiši naziv za javne stranice"
        >
          Linije vozi više prijevoznika, a u Postavkama nije upisan naziv, pa zasloni stanica i PDF
          vozni redovi prikazuju naziv samo jednog od njih. Upiši naziv sustava (npr. Gradski
          prijevoz) u{' '}
          <Anchor component={Link} to="/settings" size="sm">
            Postavkama
          </Anchor>
          .
        </Alert>
      )}

      <AgencyDialog opened={newOpened} onClose={newAgency.close} defaultTimezone={timezone} />
      <AgencyDialog
        opened={editing !== null}
        agency={editing ?? undefined}
        onClose={() => setEditing(null)}
      />
      <DeleteAgencyDialog
        agency={deleting?.agency ?? null}
        routeCount={deleting?.routeCount ?? 0}
        agencies={agencies.data ?? []}
        onClose={() => setDeleting(null)}
      />

      {agencies.isPending ? (
        <PageLoader />
      ) : agencies.isError ? (
        <QueryError error={agencies.error} onRetry={() => void agencies.refetch()} />
      ) : (
        <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
          {agencies.data.map((agency) => (
            <AgencyCard
              key={agency.id}
              agency={agency}
              // Instanca bez prijevoznika vraća se u čarobnjak prvog pokretanja.
              canDelete={agencies.data.length > 1}
              onEdit={() => setEditing(agency)}
              onDelete={(routeCount) => setDeleting({ agency, routeCount })}
            />
          ))}
        </SimpleGrid>
      )}
    </Stack>
  )
}

interface AgencyCardProps {
  agency: Agency
  canDelete: boolean
  onEdit: () => void
  onDelete: (routeCount: number) => void
}

function AgencyCard({ agency, canDelete, onEdit, onDelete }: AgencyCardProps) {
  // Samo ukupan broj; stranica s jednom linijom je najmanji upit.
  const routes = useRoutes({ AgencyId: agency.id, PageSize: 1 })

  const facts = [
    { label: 'Web', value: agency.url, href: agency.url },
    { label: 'Telefon', value: agency.phone, href: agency.phone && `tel:${agency.phone}` },
    { label: 'E-mail', value: agency.email, href: agency.email && `mailto:${agency.email}` },
    { label: 'Cijene karata', value: agency.fareUrl, href: agency.fareUrl },
    { label: 'Vremenska zona', value: agency.timezone },
    { label: 'Jezik', value: agency.lang },
  ].filter((fact) => fact.value)

  return (
    <Paper withBorder p="lg">
      <Stack gap="md">
        <Group justify="space-between" align="flex-start" wrap="nowrap">
          <Stack gap={2}>
            <Title order={3}>{agency.name}</Title>
            <Text size="sm" c="dimmed">
              GTFS oznaka: {agency.code}
            </Text>
          </Stack>
          <Group gap="xs" wrap="nowrap" style={{ flexShrink: 0 }}>
            {routes.data && (
              <Badge
                component={Link}
                to={`/routes?agency=${agency.id}`}
                variant="light"
                size="lg"
                tt="none"
                style={{ cursor: 'pointer' }}
              >
                {formatNumber(routes.data.totalCount)} linija
              </Badge>
            )}
            <Tooltip label="Uredi">
              <ActionIcon variant="subtle" onClick={onEdit} aria-label={`Uredi ${agency.name}`}>
                <IconPencil size={18} />
              </ActionIcon>
            </Tooltip>
            <Tooltip label={canDelete ? 'Obriši' : 'Instanca mora imati barem jednog prijevoznika'}>
              <ActionIcon
                variant="subtle"
                color="red"
                onClick={() => onDelete(routes.data?.totalCount ?? 0)}
                disabled={!canDelete || !routes.data}
                aria-label={`Obriši ${agency.name}`}
              >
                <IconTrash size={18} />
              </ActionIcon>
            </Tooltip>
          </Group>
        </Group>
        <SimpleGrid cols={2} spacing="sm" verticalSpacing="xs">
          {facts.map((fact) => (
            <div key={fact.label} style={{ minWidth: 0 }}>
              <Text size="xs" c="dimmed">
                {fact.label}
              </Text>
              {fact.href ? (
                <Anchor
                  href={fact.href}
                  target={fact.href.startsWith('http') ? '_blank' : undefined}
                  rel="noreferrer"
                  size="sm"
                  display="block"
                  truncate
                >
                  {fact.value}
                </Anchor>
              ) : (
                <Text size="sm">{fact.value}</Text>
              )}
            </div>
          ))}
        </SimpleGrid>
      </Stack>
    </Paper>
  )
}
