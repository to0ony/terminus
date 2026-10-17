import {
  Badge,
  Button,
  Checkbox,
  Group,
  Paper,
  ScrollArea,
  Stack,
  Text,
  TextInput,
  Title,
} from '@mantine/core'
import { IconSearch } from '@tabler/icons-react'
import { useMemo, useState } from 'react'
import { formatNumber } from '@/lib/format'
import type { GtfsImportVariant } from './queries'

interface VariantSelectionProps {
  variants: GtfsImportVariant[]
  selected: Set<string>
  onChange: (selected: Set<string>) => void
}

/**
 * Prepoznate varijante linija i koje se uvoze. Zadano su odabrane glavne i
 * česte; rijetke (npr. vožnje u garažu) korisnik uključuje sam.
 */
export function VariantSelection({ variants, selected, onChange }: VariantSelectionProps) {
  const [search, setSearch] = useState('')

  const routes = useMemo(() => {
    const groups = new Map<string, GtfsImportVariant[]>()
    for (const variant of variants) {
      groups.set(variant.routeCode, [...(groups.get(variant.routeCode) ?? []), variant])
    }
    return [...groups.values()]
  }, [variants])

  const query = search.trim().toLowerCase()
  const shown = routes.filter(
    (group) =>
      !query ||
      group.some(
        (v) =>
          (v.routeName ?? v.routeCode).toLowerCase().includes(query) ||
          v.name.toLowerCase().includes(query),
      ),
  )

  const toggle = (key: string, checked: boolean) => {
    const next = new Set(selected)
    if (checked) {
      next.add(key)
    } else {
      next.delete(key)
    }
    onChange(next)
  }

  const choose = (predicate: (v: GtfsImportVariant) => boolean) =>
    onChange(new Set(variants.filter(predicate).map((v) => v.key)))

  return (
    <Stack gap="sm">
      <div>
        <Title order={4}>Varijante linija</Title>
        <Text size="sm" c="dimmed">
          Redoslijed stanica svake linije po smjeru, s vremenima vožnje iz feeda. Uvezene varijante
          su predložak za vožnje koje ćeš dodati; možeš ih poslije urediti ili obrisati.
        </Text>
      </div>

      <Group justify="space-between" gap="xs">
        <Text size="sm" fw={500}>
          Odabrano {formatNumber(selected.size)} od {formatNumber(variants.length)}
        </Text>
        <Group gap="xs">
          <Button size="compact-sm" variant="default" onClick={() => choose((v) => v.included)}>
            Zadano
          </Button>
          <Button size="compact-sm" variant="default" onClick={() => choose((v) => v.main)}>
            Samo glavne
          </Button>
          <Button size="compact-sm" variant="default" onClick={() => choose(() => true)}>
            Sve
          </Button>
        </Group>
      </Group>

      <TextInput
        placeholder="Linija ili stanica"
        aria-label="Pretraži varijante"
        leftSection={<IconSearch size={16} />}
        value={search}
        onChange={(event) => setSearch(event.currentTarget.value)}
      />

      <ScrollArea.Autosize mah={460} type="auto">
        <Stack gap="xs">
          {shown.map((group) => (
            <Paper key={group[0].routeCode} withBorder p="xs">
              <Text size="sm" fw={700} mb={4}>
                {group[0].routeName ?? group[0].routeCode}
              </Text>
              <Stack gap={4}>
                {group.map((variant) => (
                  <Checkbox
                    key={variant.key}
                    checked={selected.has(variant.key)}
                    onChange={(event) => toggle(variant.key, event.currentTarget.checked)}
                    label={
                      <Group gap={6} wrap="nowrap">
                        <Text size="sm">{variant.name}</Text>
                        {variant.main && (
                          <Badge size="xs" variant="light">
                            glavna
                          </Badge>
                        )}
                      </Group>
                    }
                    description={`${variant.stopCount} stanica · ${Math.round(variant.duration / 60)} min · ${formatNumber(variant.tripCount)} vožnji (${Math.round(variant.share * 100)} % smjera)`}
                  />
                ))}
              </Stack>
            </Paper>
          ))}
        </Stack>
      </ScrollArea.Autosize>
    </Stack>
  )
}
