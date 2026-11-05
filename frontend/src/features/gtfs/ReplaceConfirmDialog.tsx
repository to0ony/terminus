import { Button, Checkbox, Group, List, Modal, Stack, Text } from '@mantine/core'
import { useState } from 'react'
import { formatNumber } from '@/lib/format'
import type { GtfsImportReport } from './queries'

interface ReplaceConfirmDialogProps {
  opened: boolean
  existing: GtfsImportReport['existing']
  onClose: () => void
  onConfirm: () => void
}

/** Zadnja potvrda prije uvoza: tek se ovdje brišu postojeći podaci. */
export function ReplaceConfirmDialog({
  opened,
  existing,
  onClose,
  onConfirm,
}: ReplaceConfirmDialogProps) {
  const [understood, setUnderstood] = useState(false)

  const close = () => {
    setUnderstood(false)
    onClose()
  }

  const rows = [
    { label: 'Sezone', value: existing.seasons },
    { label: 'Vožnje', value: existing.trips },
    { label: 'Linije', value: existing.routes },
    { label: 'Stanice', value: existing.stops },
    { label: 'Prijevoznici', value: existing.agencies },
  ].filter((row) => row.value > 0)

  return (
    <Modal opened={opened} onClose={close} title="Zamijeniti sve podatke?">
      <Stack gap="md">
        <Text size="sm">Uvoz trajno briše postojeći vozni red i zamjenjuje ga novim feedom:</Text>
        <List size="sm" spacing={4}>
          {rows.map((row) => (
            <List.Item key={row.label}>
              {row.label}: {formatNumber(row.value)}
            </List.Item>
          ))}
        </List>
        <Text size="sm" c="dimmed">
          Brišu se i kalendari, varijante, trase i cijene. Korisnici i postavke ostaju. Ako je
          objava uključena, javni polasci i GTFS odmah prelaze na novi feed.
        </Text>
        <Checkbox
          label="Razumijem da se postojeći podaci ne mogu vratiti"
          checked={understood}
          onChange={(event) => setUnderstood(event.currentTarget.checked)}
        />
        <Group justify="flex-end">
          <Button variant="default" onClick={close}>
            Odustani
          </Button>
          <Button
            color="red"
            disabled={!understood}
            onClick={() => {
              close()
              onConfirm()
            }}
          >
            Zamijeni podatke
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
