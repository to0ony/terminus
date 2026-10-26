import { Center, Group, Stack, Text, Title } from '@mantine/core'
import { IconMapOff } from '@tabler/icons-react'
import { PageLoader } from '@/components/PageLoader'
import { usePublicSettings } from '@/features/board/queries'
import { LiveApiContext, publicApi } from './api'
import { LiveMapView } from './LiveMapPage'

/**
 * Javna karta uživo (/karta), bez prijave: ista karta kao u dashboardu, ali
 * samo uživo (bez simulacije), s nazivom i logom prijevoznika. Dostupna kad je
 * u postavkama uključena javna karta.
 */
export function PublicLiveMapPage() {
  const settings = usePublicSettings()

  if (settings.isPending) {
    return <PageLoader />
  }

  if (!settings.data?.mapEnabled) {
    return (
      <Center h="100dvh" p="md">
        <Stack align="center" gap="xs">
          <IconMapOff size={40} stroke={1.5} />
          <Title order={3}>Karta nije dostupna</Title>
          <Text c="dimmed" ta="center">
            {settings.data?.displayName ?? 'Prijevoznik'} trenutno nema javnu kartu.
          </Text>
        </Stack>
      </Center>
    )
  }

  const brand = (
    <Group gap="sm" wrap="nowrap">
      {settings.data.logoUrl && (
        <img
          src={settings.data.logoUrl}
          alt=""
          style={{ height: 28, maxWidth: 120, objectFit: 'contain' }}
        />
      )}
      <Text fw={700}>{settings.data.displayName ?? 'Vozni red'}</Text>
    </Group>
  )

  return (
    <LiveApiContext.Provider value={publicApi}>
      <LiveMapView standalone brand={brand} />
    </LiveApiContext.Provider>
  )
}
