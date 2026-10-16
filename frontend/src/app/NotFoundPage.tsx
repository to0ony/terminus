import { Button, Center, Stack, Text, Title } from '@mantine/core'
import { Link } from 'react-router'

export function NotFoundPage() {
  return (
    <Center mih="60vh" p="md">
      <Stack align="center" gap="sm" ta="center">
        <Title order={1} c="dimmed">
          404
        </Title>
        <Text>Ova stranica ne postoji.</Text>
        <Button component={Link} to="/" variant="light">
          Na početnu
        </Button>
      </Stack>
    </Center>
  )
}
