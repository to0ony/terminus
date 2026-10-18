import { Alert, Button, Group, Paper, Stack, Text } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconArrowsMove } from '@tabler/icons-react'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { errorMessage } from '@/api/errors'
import { type Stop, type StopRequest, useSaveStop, useStopsExtent } from './queries'
import { StopsMap } from './StopsMap'

/**
 * Stanica na karti; „Premjesti” postavlja nove koordinate klikom na kartu.
 * Stanica bez koordinata se tako prvi put smješta.
 */
export function StopLocation({ stop }: { stop: Stop }) {
  const navigate = useNavigate()
  const extent = useStopsExtent()
  const save = useSaveStop(stop.id)
  const [moving, setMoving] = useState(false)
  const [position, setPosition] = useState<{ latitude: number; longitude: number } | null>(null)

  const placed = stop.latitude !== null && stop.longitude !== null
  const bounds = placed
    ? {
        minLatitude: stop.latitude! - 0.003,
        maxLatitude: stop.latitude! + 0.003,
        minLongitude: stop.longitude! - 0.005,
        maxLongitude: stop.longitude! + 0.005,
      }
    : (extent.data ?? null)

  const cancel = () => {
    setMoving(false)
    setPosition(null)
  }

  const submit = () => {
    if (!position) {
      return
    }
    // PUT zamjenjuje cijelu stanicu, pa se ostala polja šalju kakva jesu.
    const body: StopRequest = {
      code: stop.code,
      publicCode: stop.publicCode,
      name: stop.name,
      ttsName: stop.ttsName,
      description: stop.description,
      zoneId: stop.zoneId,
      url: stop.url,
      locationType: stop.locationType,
      parentStationId: stop.parentStationId,
      timezone: stop.timezone,
      platformCode: stop.platformCode,
      wheelchairBoarding: stop.wheelchairBoarding,
      isActive: stop.isActive,
      latitude: Math.round(position.latitude * 1e6) / 1e6,
      longitude: Math.round(position.longitude * 1e6) / 1e6,
    }
    save.mutate(
      { body },
      {
        onSuccess: () => {
          cancel()
          notifications.show({ color: 'green', message: 'Položaj stanice je spremljen.' })
        },
        onError: (error) =>
          notifications.show({
            color: 'red',
            title: 'Položaj nije spremljen',
            message: errorMessage(error),
          }),
      },
    )
  }

  if (!placed && !moving) {
    return (
      <Alert variant="light" color="gray">
        <Group justify="space-between">
          <Text size="sm">Stanica nema koordinate.</Text>
          <Button size="xs" variant="default" onClick={() => setMoving(true)}>
            Smjesti na kartu
          </Button>
        </Group>
      </Alert>
    )
  }

  return (
    <Stack gap={6}>
      <Paper withBorder style={{ overflow: 'hidden' }}>
        <StopsMap
          key={stop.id}
          h={300}
          extent={bounds}
          selectedId={stop.id}
          onSelect={moving ? undefined : (selected) => void navigate(`/stops/${selected.id}`)}
          onMapClick={moving ? setPosition : undefined}
          overlay={
            position
              ? {
                  line: placed
                    ? [
                        [stop.longitude!, stop.latitude!],
                        [position.longitude, position.latitude],
                      ]
                    : [],
                  points: [{ position: [position.longitude, position.latitude], label: '' }],
                }
              : null
          }
        />
      </Paper>
      {moving ? (
        <Group justify="space-between" gap="xs">
          <Text size="xs" c="dimmed">
            {position
              ? `Novi položaj: ${position.latitude.toFixed(5)}, ${position.longitude.toFixed(5)}`
              : 'Klikni novo mjesto stanice na karti (izvan drugih stanica).'}
          </Text>
          <Group gap="xs">
            <Button size="xs" variant="default" onClick={cancel}>
              Odustani
            </Button>
            <Button size="xs" onClick={submit} disabled={!position} loading={save.isPending}>
              Spremi položaj
            </Button>
          </Group>
        </Group>
      ) : (
        <Group justify="flex-end">
          <Button
            size="xs"
            variant="subtle"
            leftSection={<IconArrowsMove size={14} />}
            onClick={() => setMoving(true)}
          >
            Premjesti
          </Button>
        </Group>
      )}
    </Stack>
  )
}
