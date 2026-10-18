import { Anchor, Button, Group, Paper, Stack, Text } from '@mantine/core'
import { IconPlus } from '@tabler/icons-react'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router'
import { type Stop, useStops } from './queries'
import { type NewStop, StopDialog } from './StopDialog'

/** Peroni kolodvora i dodavanje novog (na mjestu kolodvora, pa se premjesti). */
export function StationPlatforms({ station }: { station: Stop }) {
  const navigate = useNavigate()
  const platforms = useStops({ ParentStationId: station.id, PageSize: 100 })
  const [create, setCreate] = useState<NewStop | null>(null)

  return (
    <Paper withBorder p="md">
      <Group justify="space-between" mb="xs">
        <Text fw={600} size="sm">
          Peroni
        </Text>
        <Button
          size="xs"
          variant="light"
          leftSection={<IconPlus size={14} />}
          disabled={station.latitude === null || station.longitude === null}
          onClick={() =>
            setCreate({
              latitude: station.latitude!,
              longitude: station.longitude!,
              name: station.name,
              parentStationId: station.id,
            })
          }
        >
          Dodaj peron
        </Button>
      </Group>
      {platforms.data && platforms.data.items.length === 0 ? (
        <Text size="sm" c="dimmed">
          Kolodvor još nema perona. Vožnje staju na peronima, a kolodvor ih okuplja.
        </Text>
      ) : (
        <Stack gap={4}>
          {platforms.data?.items.map((platform) => (
            <Anchor key={platform.id} component={Link} to={`/stops/${platform.id}`} size="sm">
              {platform.name}
              {platform.platformCode && ` · peron ${platform.platformCode}`}
            </Anchor>
          ))}
        </Stack>
      )}
      <StopDialog
        create={create}
        opened={create !== null}
        onClose={() => setCreate(null)}
        // Novi peron je na mjestu kolodvora; na njegovoj stranici se premješta.
        onSaved={(saved) => void navigate(`/stops/${saved.id}`)}
      />
    </Paper>
  )
}
