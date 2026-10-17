import { Anchor, Badge, Group, Paper, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { Link } from 'react-router'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { useRoutes } from '@/features/routes/queries'
import { formatNumber } from '@/lib/format'
import { type Agency, useAgencies } from './queries'

/** Prijevoznici (GTFS agency) s kontaktima i brojem linija. */
export function AgenciesPage() {
  const agencies = useAgencies()

  return (
    <Stack gap="md">
      <Title order={2}>Prijevoznici</Title>

      {agencies.isPending ? (
        <PageLoader />
      ) : agencies.isError ? (
        <QueryError error={agencies.error} onRetry={() => void agencies.refetch()} />
      ) : (
        <SimpleGrid cols={{ base: 1, md: 2 }} spacing="md">
          {agencies.data.map((agency) => (
            <AgencyCard key={agency.id} agency={agency} />
          ))}
        </SimpleGrid>
      )}
    </Stack>
  )
}

function AgencyCard({ agency }: { agency: Agency }) {
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
          {routes.data && (
            <Badge
              component={Link}
              to={`/routes?agency=${agency.id}`}
              variant="light"
              size="lg"
              tt="none"
              style={{ cursor: 'pointer', flexShrink: 0 }}
            >
              {formatNumber(routes.data.totalCount)} linija
            </Badge>
          )}
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
