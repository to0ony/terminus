import {
  Box,
  Button,
  Container,
  Group,
  Paper,
  SimpleGrid,
  Stack,
  Text,
  ThemeIcon,
  Title,
  UnstyledButton,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconFileZip, IconPencilPlus, type Icon } from '@tabler/icons-react'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Navigate, useNavigate } from 'react-router'
import { Logo } from '@/components/Logo'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { useLogout } from '@/features/auth/queries'
import { GtfsImport } from '@/features/gtfs/GtfsImport'
import { BlankSetupForm } from './BlankSetupForm'
import { setupKeys, useSetupStatus } from './queries'
import classes from './SetupPage.module.css'

type Mode = 'choose' | 'import' | 'blank'

/** Čarobnjak prvog pokretanja: uvoz postojećeg GTFS-a ili početak od nule. */
export function SetupPage() {
  const { data: status, error, refetch } = useSetupStatus()
  const [mode, setMode] = useState<Mode>('choose')
  const queryClient = useQueryClient()
  const navigate = useNavigate()
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

  const finish = async (message: string, to = '/') => {
    await queryClient.invalidateQueries({ queryKey: setupKeys.status })
    notifications.show({ color: 'green', title: 'Terminus je spreman', message })
    navigate(to, { replace: true })
  }

  return (
    <Container size={mode === 'import' ? 'md' : 'sm'} py={{ base: 'md', sm: 48 }}>
      <Group justify="space-between" mb="xl">
        <Logo height={24} />
        <Button
          variant="subtle"
          size="xs"
          onClick={() => logout.mutate()}
          loading={logout.isPending}
        >
          Odjava
        </Button>
      </Group>

      <Paper withBorder shadow="sm" p={{ base: 'md', sm: 'xl' }}>
        {mode === 'choose' && (
          <Stack gap="lg">
            <div>
              <Title order={2} size="h3">
                Dobro došli u Terminus
              </Title>
              <Text c="dimmed" mt={4}>
                Za početak su potrebni podaci o prijevozu. Imaš li već vozni red u GTFS formatu?
              </Text>
            </div>
            <SimpleGrid cols={{ base: 1, xs: 2 }}>
              <Choice
                icon={IconFileZip}
                title="Uvezi postojeći GTFS"
                description="Prijevoznici, stanice, linije, trase i varijante linija iz GTFS ZIP datoteke, da ne kreneš od nule. Kalendare i vožnje radiš u aplikaciji."
                onClick={() => setMode('import')}
              />
              <Choice
                icon={IconPencilPlus}
                title="Počni od nule"
                description="Upiši podatke prijevoznika, a stanice, linije i vozni red unosiš kasnije u dashboardu."
                onClick={() => setMode('blank')}
              />
            </SimpleGrid>
          </Stack>
        )}

        {mode === 'import' && (
          <Stack gap="lg">
            <StepTitle
              title="Uvoz GTFS-a"
              description="Odaberi GTFS ZIP. Feed se prvo provjeri, a podaci se upisuju tek nakon potvrde. Uvoze se stanice, linije, trase i varijante linija; kalendare i vožnje radiš u aplikaciji."
            />
            <GtfsImport
              onImported={() =>
                finish(
                  'Osnova je uvezena. Sljedeći korak: napravi sezonu voznog reda.',
                  '/calendars',
                )
              }
              onCancel={() => setMode('choose')}
            />
          </Stack>
        )}

        {mode === 'blank' && (
          <Stack gap="lg">
            <StepTitle
              title="Prvi prijevoznik"
              description="Podaci se objavljuju u GTFS feedu (agency.txt). Mogu se kasnije promijeniti."
            />
            <BlankSetupForm
              onCreated={() => finish('Prijevoznik je stvoren.')}
              onCancel={() => setMode('choose')}
            />
          </Stack>
        )}
      </Paper>
    </Container>
  )
}

interface ChoiceProps {
  icon: Icon
  title: string
  description: string
  onClick: () => void
}

function Choice({ icon: ChoiceIcon, title, description, onClick }: ChoiceProps) {
  return (
    <UnstyledButton className={classes.choice} onClick={onClick}>
      <ThemeIcon variant="light" size={44} radius="md">
        <ChoiceIcon size={26} stroke={1.5} />
      </ThemeIcon>
      <Box mt="md">
        <Text fw={600}>{title}</Text>
        <Text size="sm" c="dimmed" mt={4}>
          {description}
        </Text>
      </Box>
    </UnstyledButton>
  )
}

function StepTitle({ title, description }: { title: string; description: string }) {
  return (
    <div>
      <Title order={2} size="h3">
        {title}
      </Title>
      <Text c="dimmed" mt={4}>
        {description}
      </Text>
    </div>
  )
}
