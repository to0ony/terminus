import { Alert, Button, Group, List, Paper, Stack, Text, Title } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle, IconDownload } from '@tabler/icons-react'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiError, errorMessage } from '@/api/errors'
import { saveFile } from '@/lib/download'
import { GtfsImport } from './GtfsImport'
import { useExportGtfs } from './queries'

/** Izvoz trenutnog voznog reda i uvoz novog (npr. za novu sezonu). */
export function GtfsPage() {
  return (
    <Stack gap="lg">
      <Title order={2}>GTFS uvoz i izvoz</Title>
      <ExportSection />
      <ImportSection />
    </Stack>
  )
}

function ExportSection() {
  const exportFeed = useExportGtfs()
  const problems = exportFeed.error instanceof ApiError ? exportFeed.error.problems : []

  return (
    <Paper withBorder p="lg">
      <Stack gap="md">
        <div>
          <Title order={3}>Izvoz</Title>
          <Text size="sm" c="dimmed">
            Cijeli vozni red kao GTFS ZIP, za Google Maps, aplikacije i druge sustave. Prije izvoza
            podaci se provjeravaju.
          </Text>
        </div>
        <Group>
          <Button
            leftSection={<IconDownload size={18} />}
            loading={exportFeed.isPending}
            onClick={() => exportFeed.mutate(undefined, { onSuccess: saveFile })}
          >
            Preuzmi GTFS
          </Button>
        </Group>
        {exportFeed.isError && (
          <Alert color="red" variant="light" icon={<IconAlertCircle />} title="Izvoz nije uspio">
            <Text size="sm">{errorMessage(exportFeed.error)}</Text>
            {problems.length > 0 && (
              <List size="sm" mt="xs" spacing={4}>
                {problems.map((problem) => (
                  <List.Item key={problem}>{problem}</List.Item>
                ))}
              </List>
            )}
          </Alert>
        )}
      </Stack>
    </Paper>
  )
}

function ImportSection() {
  const queryClient = useQueryClient()
  // Novi ključ vraća uvoz na prvi korak.
  const [run, setRun] = useState(0)
  const [imported, setImported] = useState(false)

  const handleImported = async () => {
    // Uvoz zamjenjuje sve podatke voznog reda, pa ništa iz cachea ne vrijedi.
    await queryClient.invalidateQueries()
    notifications.show({
      color: 'green',
      title: 'Uvoz je završen',
      message: 'Vozni red je zamijenjen.',
    })
    setImported(true)
  }

  const startOver = () => {
    setImported(false)
    setRun((value) => value + 1)
  }

  return (
    <Paper withBorder p="lg">
      <Stack gap="md">
        <div>
          <Title order={3}>Uvoz novog voznog reda</Title>
          <Text size="sm" c="dimmed">
            Uvoz zamjenjuje sve postojeće prijevoznike, linije, stanice, kalendare i vožnje.
            Postavke i administratori ostaju. Feed se prvo provjeri, a upisuje se tek nakon potvrde.
          </Text>
        </div>
        <GtfsImport key={run} onImported={() => void handleImported()} />
        {imported && (
          <Group justify="flex-end">
            <Button variant="default" onClick={startOver}>
              Novi uvoz
            </Button>
          </Group>
        )}
      </Stack>
    </Paper>
  )
}
