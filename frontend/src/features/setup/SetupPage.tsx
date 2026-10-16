import { Button, Center, Group, Paper, Stack, Text, Title } from '@mantine/core'
import { Navigate } from 'react-router'
import { Logo } from '@/components/Logo'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { useLogout } from '@/features/auth/queries'
import { useSetupStatus } from './queries'

/** Čarobnjak prvog pokretanja: uvoz GTFS-a ili početak od nule (u izradi). */
export function SetupPage() {
  const { data: status, error, refetch } = useSetupStatus()
  const logout = useLogout()

  if (error) {
    return <QueryError error={error} onRetry={() => refetch()} />
  }

  if (!status) {
    return <PageLoader />
  }

  if (status.completed) {
    return <Navigate to="/" replace />
  }

  return (
    <Center mih="100vh" p="md">
      <Paper withBorder shadow="sm" p="xl" w="100%" maw={560}>
        <Stack>
          <Logo height={24} />
          <Title order={2} size="h3">
            Dobro došli u Terminus
          </Title>
          <Text>
            Instanca još nema podataka. Uvezi postojeći GTFS feed ili počni od nule s prvim
            prijevoznikom.
          </Text>
          <Text c="dimmed" size="sm">
            Čarobnjak je u izradi.
          </Text>
          <Group justify="flex-end">
            <Button variant="subtle" onClick={() => logout.mutate()} loading={logout.isPending}>
              Odjava
            </Button>
          </Group>
        </Stack>
      </Paper>
    </Center>
  )
}
