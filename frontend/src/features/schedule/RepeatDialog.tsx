import { Button, Group, Modal, NumberInput, Stack, Text, TextInput } from '@mantine/core'
import { useState } from 'react'
import { formatTime, minute, parseTime, taktDepartures } from './model'

interface CopyDialogProps {
  opened: boolean
  count: number
  onClose: () => void
  /** Kopije svake odabrane vožnje pomaknute za interval, zadani broj puta. */
  onCopy: (interval: number, times: number) => void
}

/** Kopiranje odabranih vožnji s pomakom (npr. svakih 10 min, 5 puta). */
export function CopyDialog({ opened, count, onClose, onCopy }: CopyDialogProps) {
  const [interval, setGap] = useState<number | string>(10)
  const [times, setTimes] = useState<number | string>(1)

  const valid = Number(interval) > 0 && Number(times) >= 1

  return (
    <Modal opened={opened} onClose={onClose} title="Kopiraj vožnje" size="sm">
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          {count === 1 ? 'Odabrana vožnja' : `Odabranih ${count} vožnji`} se kopira s pomakom;
          vremena vožnje ostaju ista.
        </Text>
        <Group grow>
          <NumberInput
            label="Svakih (min)"
            min={1}
            max={720}
            value={interval}
            onChange={(value) => setGap(String(value))}
          />
          <NumberInput
            label="Puta"
            min={1}
            max={100}
            value={times}
            onChange={(value) => setTimes(String(value))}
          />
        </Group>
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            Odustani
          </Button>
          <Button
            disabled={!valid}
            onClick={() => {
              onCopy(Number(interval) * minute, Number(times))
              onClose()
            }}
          >
            Kopiraj
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}

interface TaktDialogProps {
  opened: boolean
  onClose: () => void
  /** Prvi polasci novih vožnji. */
  onCreate: (departures: number[]) => void
  /** Postojeći prvi polasci, da se ne naprave dvostruki. */
  existing: number[]
  /** Naziv varijante po kojoj nastaju vožnje; null kad se kopira postojeća vožnja. */
  template: string | null
}

/** Generator takta: polasci od–do svakih N minuta, po predlošku glavnog obrasca. */
export function TaktDialog({ opened, onClose, onCreate, existing, template }: TaktDialogProps) {
  const [from, setFrom] = useState('05:00')
  const [to, setTo] = useState('23:00')
  const [every, setEvery] = useState<number | string>(15)

  const start = parseTime(from)
  const end = parseTime(to)
  const departures =
    start !== null && end !== null && end >= start && Number(every) > 0
      ? taktDepartures(start, end, Number(every) * minute)
      : []
  // Polazak unutar minute od postojećeg se preskače.
  const fresh = departures.filter((t) => !existing.some((e) => Math.abs(e - t) < minute))

  return (
    <Modal opened={opened} onClose={onClose} title="Generator takta" size="sm">
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          {template
            ? `Nove vožnje po varijanti „${template}”.`
            : 'Nove vožnje s vremenima vožnje glavnog obrasca linije.'}{' '}
          Polasci koji već postoje se preskaču.
        </Text>
        <Group grow>
          <TextInput
            label="Od"
            value={from}
            onChange={(event) => setFrom(event.currentTarget.value)}
            error={start === null ? 'HH:MM' : undefined}
          />
          <TextInput
            label="Do"
            value={to}
            onChange={(event) => setTo(event.currentTarget.value)}
            error={end === null ? 'HH:MM' : undefined}
          />
        </Group>
        <NumberInput
          label="Svakih (min)"
          min={1}
          max={720}
          value={every}
          onChange={(value) => setEvery(String(value))}
        />
        <Text size="sm">
          {fresh.length === 0
            ? 'Nema novih polazaka.'
            : `${fresh.length} novih vožnji: ${formatTime(fresh[0])} … ${formatTime(fresh.at(-1)!)}`}
          {departures.length > fresh.length && ` (${departures.length - fresh.length} već postoji)`}
        </Text>
        <Group justify="flex-end">
          <Button variant="default" onClick={onClose}>
            Odustani
          </Button>
          <Button
            disabled={fresh.length === 0}
            onClick={() => {
              onCreate(fresh)
              onClose()
            }}
          >
            Dodaj vožnje
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
