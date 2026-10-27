import { type BoxProps, Grid, Group, Paper, SimpleGrid, Skeleton, Stack } from '@mantine/core'

// Obrisi sadržaja dok se učitava, da stranica ne skače kad podaci stignu.
// Spinner (PageLoader) ostaje samo za prijavu i provjeru postavljanja.

/** Tablica ili popis: zaglavlje i nekoliko redaka. */
export function TableSkeleton({ rows = 6 }: { rows?: number }) {
  return (
    <Paper withBorder p="md" aria-busy="true" aria-label="Učitavanje">
      <Stack gap="md">
        <Skeleton h={12} w="35%" />
        {Array.from({ length: rows }, (_, i) => (
          <Group key={i} gap="md" wrap="nowrap">
            <Skeleton h={22} w={44} radius="sm" />
            <Skeleton h={12} w={`${50 - (i % 3) * 8}%`} />
            <Skeleton h={12} w="15%" ml="auto" />
          </Group>
        ))}
      </Stack>
    </Paper>
  )
}

/** Pravokutni blok, npr. karta, grafikon ili mjesečni prikaz. */
export function BlockSkeleton({ h = 320 }: { h?: BoxProps['h'] }) {
  return <Skeleton h={h} radius="md" aria-busy="true" aria-label="Učitavanje" />
}

/** Zaglavlje stranice s detaljima: putokaz, naslov i podnaslov. */
function HeaderSkeleton() {
  return (
    <Stack gap="sm">
      <Skeleton h={12} w={180} />
      <Skeleton h={28} w="40%" />
      <Skeleton h={12} w="25%" />
    </Stack>
  )
}

/** Stranica s detaljima (linija, stanica, kalendar, sezona). */
export function DetailSkeleton() {
  return (
    <Stack gap="md" aria-busy="true" aria-label="Učitavanje">
      <HeaderSkeleton />
      <SimpleGrid cols={{ base: 1, sm: 3 }} spacing="md">
        {Array.from({ length: 3 }, (_, i) => (
          <Skeleton key={i} h={72} radius="md" />
        ))}
      </SimpleGrid>
      <TableSkeleton />
    </Stack>
  )
}

/** Grafikon voznog reda: zaglavlje, alatna traka i grafikon. */
export function ChartSkeleton() {
  return (
    <Stack gap="md" aria-busy="true" aria-label="Učitavanje">
      <HeaderSkeleton />
      <Skeleton h={56} radius="md" />
      <BlockSkeleton h={480} />
    </Stack>
  )
}

/** Editor varijante: popis stanica lijevo, karta desno. */
export function EditorSkeleton() {
  return (
    <Stack gap="md" aria-busy="true" aria-label="Učitavanje">
      <HeaderSkeleton />
      <Grid gap="md">
        <Grid.Col span={{ base: 12, lg: 5 }}>
          <TableSkeleton rows={10} />
        </Grid.Col>
        <Grid.Col span={{ base: 12, lg: 7 }}>
          <BlockSkeleton h={{ base: 400, lg: 'calc(100vh - 220px)' }} />
        </Grid.Col>
      </Grid>
    </Stack>
  )
}

/** Obrazac: oznake i polja. */
export function FormSkeleton({ fields = 5 }: { fields?: number }) {
  return (
    <Paper withBorder p="md" aria-busy="true" aria-label="Učitavanje">
      <Stack gap="lg">
        {Array.from({ length: fields }, (_, i) => (
          <Stack key={i} gap={6}>
            <Skeleton h={10} w={120} />
            <Skeleton h={36} radius="md" />
          </Stack>
        ))}
      </Stack>
    </Paper>
  )
}
