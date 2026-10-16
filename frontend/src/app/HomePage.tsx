import { Center, Image, Stack, Text } from '@mantine/core'

// Privremena početna stranica dok ne nastanu prijava i dashboard.
export function HomePage() {
  return (
    <Center h="100vh">
      <Stack align="center" gap="xs">
        <Image src="/logo.png" alt="Terminus" w={96} />
        <Text c="dimmed">Dashboard je u izradi.</Text>
      </Stack>
    </Center>
  )
}
