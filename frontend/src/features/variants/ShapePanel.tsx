import {
  ActionIcon,
  Alert,
  Badge,
  Button,
  Group,
  Loader,
  Paper,
  ScrollArea,
  SegmentedControl,
  Stack,
  Table,
  Text,
  Title,
  Tooltip,
} from '@mantine/core'
import { IconEraser, IconRefresh, IconRoute, IconTrash } from '@tabler/icons-react'
import type { NetworkKind } from './osmNetwork'
import type { VariantShape } from './useVariantShape'

const networkLabels: Record<NetworkKind, { mode: string; source: string }> = {
  road: { mode: 'Po cesti', source: 'ceste' },
  tram: { mode: 'Po pruzi', source: 'tramvajske pruge' },
  rail: { mode: 'Po pruzi', source: 'pruge' },
}

interface ShapePanelProps {
  shape: VariantShape
  kind: NetworkKind
  /** Nazivi stanica varijante redom. */
  names: string[]
  selected: number | null
  onSelect: (segment: number) => void
}

/** Trasa varijante po dionicama: izračun iz OpenStreetMapa, način i popravci po dionici. */
export function ShapePanel({ shape, kind, names, selected, onSelect }: ShapePanelProps) {
  const labels = networkLabels[kind]

  return (
    <Paper withBorder p="md">
      <Stack gap="sm">
        <div>
          <Title order={4}>Trasa</Title>
          <Text size="sm" c="dimmed">
            Prati {labels.source} iz OpenStreetMapa od stanice do stanice. Gdje ne ide dobro, povuci
            crtu na karti ili dionicu prebaci na ručno.
          </Text>
        </div>

        {!shape.available ? (
          <Alert variant="light" color="yellow" p="xs">
            <Text size="sm">Za trasu varijanta treba barem dvije stanice s koordinatama.</Text>
          </Alert>
        ) : (
          <Group gap="xs">
            <Button
              size="xs"
              leftSection={<IconRoute size={14} />}
              onClick={() => void shape.compute(shape.active)}
              loading={shape.computing}
            >
              {shape.active ? 'Izračunaj ponovno' : 'Napravi trasu'}
            </Button>
            {shape.active && (
              <Button
                size="xs"
                variant="subtle"
                color="red"
                leftSection={<IconTrash size={14} />}
                onClick={shape.remove}
                disabled={shape.computing}
              >
                Ukloni trasu
              </Button>
            )}
          </Group>
        )}

        {shape.computing && (
          <Group gap="xs">
            <Loader size="xs" />
            <Text size="sm" c="dimmed">
              Tražim put po mreži iz OpenStreetMapa…
            </Text>
          </Group>
        )}
        {shape.error && (
          <Alert variant="light" color="red" p="xs">
            <Text size="sm">{shape.error}</Text>
          </Alert>
        )}

        {shape.active && shape.segments.length > 0 && (
          <ScrollArea.Autosize mah={480}>
            <Table verticalSpacing={4} horizontalSpacing="xs">
              <Table.Tbody>
                {shape.segments.map((segment, i) => (
                  <Table.Tr
                    key={i}
                    bg={selected === i ? 'var(--mantine-color-violet-light)' : undefined}
                    onClick={() => onSelect(i)}
                    style={{ cursor: 'pointer' }}
                  >
                    <Table.Td>
                      <Text size="sm" lineClamp={1}>
                        {names[i]} → {names[i + 1]}
                      </Text>
                      <Group gap={4} mt={2}>
                        {segment === null ? (
                          <Badge size="xs" variant="light" color="gray">
                            nije izračunata
                          </Badge>
                        ) : segment.failed ? (
                          <Badge size="xs" variant="light" color="red">
                            nema puta
                          </Badge>
                        ) : null}
                        {segment && segment.via.length > 0 && (
                          <Badge size="xs" variant="light" color="violet">
                            {segment.via.length}{' '}
                            {segment.via.length === 1 ? 'međutočka' : 'međutočke'}
                          </Badge>
                        )}
                      </Group>
                    </Table.Td>
                    <Table.Td w={1}>
                      <Group gap={2} wrap="nowrap" onClick={(event) => event.stopPropagation()}>
                        <SegmentedControl
                          size="xs"
                          data={[
                            { value: 'network', label: labels.mode },
                            { value: 'manual', label: 'Ručno' },
                          ]}
                          value={segment?.mode ?? 'network'}
                          onChange={(value) => void shape.setMode(i, value as 'network' | 'manual')}
                          disabled={shape.computing}
                        />
                        <Tooltip label="Izračunaj dionicu ponovno">
                          <ActionIcon
                            variant="subtle"
                            size="sm"
                            onClick={() => void shape.recompute(i)}
                            disabled={shape.computing}
                            aria-label="Izračunaj dionicu ponovno"
                          >
                            <IconRefresh size={14} />
                          </ActionIcon>
                        </Tooltip>
                        <Tooltip label="Ukloni međutočke">
                          <ActionIcon
                            variant="subtle"
                            size="sm"
                            color="gray"
                            onClick={() => void shape.clearVia(i)}
                            disabled={shape.computing || !segment || segment.via.length === 0}
                            aria-label="Ukloni međutočke"
                          >
                            <IconEraser size={14} />
                          </ActionIcon>
                        </Tooltip>
                      </Group>
                    </Table.Td>
                  </Table.Tr>
                ))}
              </Table.Tbody>
            </Table>
          </ScrollArea.Autosize>
        )}

        {shape.active && (
          <Text size="xs" c="dimmed">
            Ručna dionica je ravna crta kroz međutočke; povlačenjem crte dodaješ točke. Trasa se
            sprema s varijantom, a dobivaju je i sve njezine vožnje.
          </Text>
        )}
      </Stack>
    </Paper>
  )
}
