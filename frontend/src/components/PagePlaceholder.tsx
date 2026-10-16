import { Stack, Text, Title } from '@mantine/core'

/** Privremeni sadržaj stranice koja još nije izrađena. */
export function PagePlaceholder({ title, description }: { title: string; description?: string }) {
  return (
    <Stack gap="xs">
      <Title order={2}>{title}</Title>
      <Text c="dimmed">{description ?? 'Stranica je u izradi.'}</Text>
    </Stack>
  )
}
