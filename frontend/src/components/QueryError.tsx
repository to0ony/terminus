import { Alert, Button, Group, Text } from '@mantine/core'
import { IconAlertCircle } from '@tabler/icons-react'
import { errorMessage } from '@/api/errors'

/** Greška pri učitavanju podataka stranice, s ponovnim pokušajem. */
export function QueryError({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  return (
    <Alert
      color="red"
      variant="light"
      icon={<IconAlertCircle />}
      title="Učitavanje nije uspjelo"
      m="md"
    >
      <Text size="sm">{errorMessage(error)}</Text>
      {onRetry && (
        <Group mt="sm">
          <Button size="xs" variant="light" color="red" onClick={onRetry}>
            Pokušaj ponovno
          </Button>
        </Group>
      )}
    </Alert>
  )
}
