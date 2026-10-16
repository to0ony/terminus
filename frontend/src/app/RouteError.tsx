import { Button, Center, Stack, Text, Title } from '@mantine/core'
import { isRouteErrorResponse, useRouteError } from 'react-router'

/**
 * Greška pri prikazu rute (iznimka u komponenti ili neuspjelo učitavanje
 * lazy dijela, npr. nakon nove verzije aplikacije).
 */
export function RouteError() {
  const error = useRouteError()

  if (import.meta.env.DEV) {
    console.error(error)
  }

  const message = isRouteErrorResponse(error)
    ? `${error.status} ${error.statusText}`
    : 'Stranica se nije mogla prikazati.'

  return (
    <Center mih="100vh" p="md">
      <Stack align="center" gap="sm" maw={420} ta="center">
        <Title order={2}>Nešto je pošlo po zlu</Title>
        <Text c="dimmed">{message} Osvježi stranicu; ako se ponovi, javi administratoru.</Text>
        <Button onClick={() => window.location.reload()}>Osvježi</Button>
      </Stack>
    </Center>
  )
}
