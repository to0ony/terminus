import {
  Alert,
  Button,
  Group,
  Kbd,
  Menu,
  Paper,
  ScrollArea,
  Select,
  Stack,
  Table,
  Text,
  TextInput,
} from '@mantine/core'
import { IconCalendarPlus, IconCopy, IconTrash } from '@tabler/icons-react'
import { useState } from 'react'
import {
  type EditorTrip,
  firstDeparture,
  formatTime,
  minute,
  parseTime,
  setStopTime,
} from './model'

interface TripPanelProps {
  trips: EditorTrip[]
  selected: string[]
  errors: Map<string, string>
  stopName: (id: string) => string
  calendars: { value: string; label: string }[]
  /** Tip dana koji se uređuje; ostali su ponuđeni za kopiranje. */
  dayTypeId: string
  onChange: (key: string, update: (trip: EditorTrip) => EditorTrip) => void
  onShift: (delta: number) => void
  onCopy: () => void
  /** Kopira odabrane vožnje (ili sve, za null) u drugi tip dana. */
  onCopyTo: (calendarId: string, keys: string[] | null) => void
  onDelete: () => void
  onTakt: () => void
  /** Postoji li predložak za nove vožnje. */
  canCreate: boolean
}

/** Detalji odabrane vožnje ili zajedničke radnje za više njih. */
export function TripPanel(props: TripPanelProps) {
  const { trips, selected } = props
  const chosen = trips.filter((t) => selected.includes(t.key))

  return (
    <Paper withBorder p="md" pos="sticky" top={72}>
      {chosen.length === 0 ? (
        <Overview {...props} />
      ) : chosen.length === 1 ? (
        // Novi ključ vraća polja na vrijednosti druge vožnje.
        <TripDetails key={chosen[0].key} {...props} trip={chosen[0]} />
      ) : (
        <Stack gap="sm">
          <Text fw={600}>{chosen.length} vožnji odabrano</Text>
          <ShiftButtons onShift={props.onShift} />
          <Actions onCopy={props.onCopy} onDelete={props.onDelete} />
          <CopyToMenu
            {...props}
            label="Kopiraj u tip dana…"
            onPick={(id) => props.onCopyTo(id, selected)}
          />
          {chosen.some((t) => props.errors.has(t.key)) && (
            <Alert color="red" variant="light">
              Neke odabrane vožnje imaju neispravna vremena.
            </Alert>
          )}
        </Stack>
      )}
    </Paper>
  )
}

function Overview(props: TripPanelProps) {
  const { trips, onTakt, canCreate } = props
  const departures = trips.map(firstDeparture).sort((a, b) => a - b)
  return (
    <Stack gap="sm">
      <Text fw={600}>{trips.length} vožnji</Text>
      {departures.length > 0 && (
        <Text size="sm" c="dimmed">
          Prvi polazak {formatTime(departures[0])}, zadnji {formatTime(departures.at(-1)!)}
        </Text>
      )}
      <Button variant="light" onClick={onTakt} disabled={!canCreate}>
        Generator takta…
      </Button>
      {trips.length > 0 && (
        <CopyToMenu
          {...props}
          label="Kopiraj sve u tip dana…"
          onPick={(id) => props.onCopyTo(id, null)}
        />
      )}
      {!canCreate && (
        <Text size="xs" c="dimmed">
          Nove vožnje trebaju varijantu linije ili postojeću vožnju za vremena vožnje. Dodaj
          varijantu na stranici linije ili gore pod „Usporedi s” odaberi tip dana koji ima vožnji.
        </Text>
      )}
      <Stack gap={4}>
        <Text size="xs" fw={600}>
          Upute
        </Text>
        <Help keys="Klik" text="odabir vožnje (Ctrl+klik dodaje)" />
        <Help keys="Povuci crtu" text="pomak cijele vožnje" />
        <Help keys="Povuci točkicu" text="vrijeme na stanici; sa Shiftom i sve iza" />
        <Help keys="Klik u retku prve stanice" text="nova vožnja" />
        <Help keys="Povuci po praznom" text="odabir okvirom" />
        <Help keys="← →" text="±1 min (Shift ±5)" />
        <Help keys="Del" text="briše odabrane" />
        <Help keys="Ctrl+Z / Ctrl+Y" text="poništi / ponovi" />
        <Help keys="Kotačić" text="zum; Shift+kotačić pomak" />
      </Stack>
    </Stack>
  )
}

function Help({ keys, text }: { keys: string; text: string }) {
  return (
    <Text size="xs" c="dimmed">
      <Kbd size="xs">{keys}</Kbd> {text}
    </Text>
  )
}

function TripDetails({
  trip,
  errors,
  stopName,
  calendars,
  onChange,
  onShift,
  onCopy,
  onDelete,
  ...props
}: TripPanelProps & { trip: EditorTrip }) {
  const [headsign, setHeadsign] = useState(trip.headsign ?? '')
  const error = errors.get(trip.key)

  return (
    <Stack gap="sm">
      <div>
        <Text fw={600}>Vožnja {formatTime(firstDeparture(trip))}</Text>
        <Text size="xs" c="dimmed">
          {trip.code ?? 'nova, još nije spremljena'}
        </Text>
      </div>

      {error && (
        <Alert color="red" variant="light" p="xs">
          <Text size="xs">{error}</Text>
        </Alert>
      )}

      <TextInput
        label="Natpis smjera"
        size="xs"
        value={headsign}
        onChange={(event) => setHeadsign(event.currentTarget.value)}
        onBlur={() => {
          const value = headsign.trim() || null
          if (value !== trip.headsign) {
            onChange(trip.key, (t) => ({ ...t, headsign: value }))
          }
        }}
      />
      <Select
        label="Tip dana"
        size="xs"
        data={calendars}
        value={trip.serviceCalendarId}
        allowDeselect={false}
        onChange={(value) =>
          value &&
          value !== trip.serviceCalendarId &&
          onChange(trip.key, (t) => ({ ...t, serviceCalendarId: value }))
        }
      />

      <ShiftButtons onShift={onShift} />

      <ScrollArea.Autosize mah={380} type="auto">
        <Table verticalSpacing={2} horizontalSpacing={4} fz="xs">
          <Table.Thead>
            <Table.Tr>
              <Table.Th>Stanica</Table.Th>
              <Table.Th w={58}>Dolazak</Table.Th>
              <Table.Th w={58}>Polazak</Table.Th>
            </Table.Tr>
          </Table.Thead>
          <Table.Tbody>
            {trip.stopTimes.map((st, index) => (
              <Table.Tr key={`${index}-${st.stopId}`}>
                <Table.Td>
                  <Text size="xs" truncate maw={130} title={stopName(st.stopId)}>
                    {stopName(st.stopId)}
                  </Text>
                </Table.Td>
                <Table.Td>
                  <TimeInput
                    value={st.arrivalTime}
                    onCommit={(arrival) =>
                      onChange(trip.key, (t) =>
                        setStopTime(t, index, arrival, Math.max(arrival, st.departureTime)),
                      )
                    }
                  />
                </Table.Td>
                <Table.Td>
                  <TimeInput
                    value={st.departureTime}
                    onCommit={(departure) =>
                      onChange(trip.key, (t) =>
                        setStopTime(t, index, Math.min(st.arrivalTime, departure), departure),
                      )
                    }
                  />
                </Table.Td>
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </ScrollArea.Autosize>

      <Actions onCopy={onCopy} onDelete={onDelete} />
      <CopyToMenu
        calendars={calendars}
        dayTypeId={props.dayTypeId}
        label="Kopiraj u tip dana…"
        onPick={(id) => props.onCopyTo(id, [trip.key])}
      />
    </Stack>
  )
}

/** Vrijeme HH:MM; sprema se na Enter ili izlaz iz polja. */
function TimeInput({ value, onCommit }: { value: number; onCommit: (seconds: number) => void }) {
  const [text, setText] = useState(formatTime(value))
  const [shown, setShown] = useState(value)
  // Vrijeme se promijenilo izvana (povlačenje, poništavanje).
  if (shown !== value) {
    setShown(value)
    setText(formatTime(value))
  }
  const parsed = parseTime(text)

  const commit = () => {
    if (parsed === null) {
      setText(formatTime(value))
    } else if (parsed !== Math.floor(value / minute) * minute) {
      // Ista minuta ne briše sekunde uvezenog vremena (npr. 07:05:30).
      onCommit(parsed)
    }
  }

  return (
    <TextInput
      size="xs"
      value={text}
      error={parsed === null}
      onChange={(event) => setText(event.currentTarget.value)}
      onBlur={commit}
      onKeyDown={(event) => {
        if (event.key === 'Enter') {
          commit()
        }
      }}
      styles={{
        input: { paddingInline: 4, textAlign: 'center', fontVariantNumeric: 'tabular-nums' },
      }}
    />
  )
}

function ShiftButtons({ onShift }: { onShift: (delta: number) => void }) {
  return (
    <Group gap={4} grow>
      {[-5, -1, 1, 5].map((m) => (
        <Button key={m} size="compact-xs" variant="default" onClick={() => onShift(m * minute)}>
          {m > 0 ? `+${m}` : `−${-m}`} min
        </Button>
      ))}
    </Group>
  )
}

/**
 * Kopija u drugi tip dana, s istim vremenima; npr. subota kao radni dan bez
 * jutarnjeg vrha: kopiraj sve, pa u suboti obriši višak.
 */
function CopyToMenu({
  calendars,
  dayTypeId,
  label,
  onPick,
}: {
  calendars: { value: string; label: string }[]
  dayTypeId: string
  label: string
  onPick: (calendarId: string) => void
}) {
  const targets = calendars.filter((c) => c.value !== dayTypeId)
  if (targets.length === 0) {
    return null
  }
  return (
    <Menu position="bottom-start">
      <Menu.Target>
        <Button size="xs" variant="default" leftSection={<IconCalendarPlus size={14} />}>
          {label}
        </Button>
      </Menu.Target>
      <Menu.Dropdown>
        {targets.map((t) => (
          <Menu.Item key={t.value} onClick={() => onPick(t.value)}>
            {t.label}
          </Menu.Item>
        ))}
      </Menu.Dropdown>
    </Menu>
  )
}

function Actions({ onCopy, onDelete }: { onCopy: () => void; onDelete: () => void }) {
  return (
    <Group gap="xs" grow>
      <Button size="xs" variant="default" leftSection={<IconCopy size={14} />} onClick={onCopy}>
        Kopiraj…
      </Button>
      <Button
        size="xs"
        variant="light"
        color="red"
        leftSection={<IconTrash size={14} />}
        onClick={onDelete}
      >
        Obriši
      </Button>
    </Group>
  )
}
