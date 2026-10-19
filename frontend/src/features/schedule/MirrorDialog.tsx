import {
  ActionIcon,
  Alert,
  Badge,
  Button,
  Checkbox,
  Group,
  Modal,
  NumberInput,
  ScrollArea,
  SegmentedControl,
  Select,
  Stack,
  Table,
  Text,
  Tooltip,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconLock, IconLockOpen } from '@tabler/icons-react'
import { useState } from 'react'
import { errorMessage } from '@/api/errors'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { useRouteVariants } from '@/features/variants/queries'
import { type MirrorSettings, mirrorRows, mirrorTrips } from './mirror'
import {
  changes,
  formatTime,
  fromResponse,
  minute,
  type ScheduleVariant,
  variantOffsets,
} from './model'
import { useSaveSchedule, useSchedule } from './queries'

interface MirrorDialogProps {
  opened: boolean
  onClose: () => void
  /** Spremljeno; grafikon se učitava ponovno. */
  onSaved: () => void
  routeId: string
  /** Smjer u koji nastaju povratne vožnje (smjer grafikona). */
  direction: number | null
  /** Varijante tog smjera; po jednoj od njih nastaju vožnje. */
  variants: ScheduleVariant[]
  /** Varijante unaprijed odabrane (npr. nakon "Povratni smjer"). */
  initialTarget: string | null
  initialSource: string | null
  dayTypes: { id: string; label: string; color: string }[]
}

const defaults: MirrorSettings = { mode: 'turn', offset: 5 * minute, overrides: {} }

/** Okret i zaključani polasci pamte se u pregledniku, po paru varijanti. */
function storageKey(source: string, target: string) {
  return `terminus.mirror.${source}.${target}`
}

function loadSettings(source: string | null, target: string | null): MirrorSettings {
  if (!source || !target) {
    return defaults
  }
  try {
    const saved = localStorage.getItem(storageKey(source, target))
    return saved ? { ...defaults, ...(JSON.parse(saved) as Partial<MirrorSettings>) } : defaults
  } catch {
    return defaults
  }
}

function storeSettings(source: string, target: string, settings: MirrorSettings) {
  try {
    localStorage.setItem(storageKey(source, target), JSON.stringify(settings))
  } catch {
    // Bez spremišta se postavke samo ne pamte.
  }
}

/**
 * Povratne vožnje iz suprotnog smjera: svaki polazak dobiva povratak s
 * okretom na terminusu (ili pomakom od polaska). Okret je zajednički, a
 * pojedini polazak se može dotjerati i zaključati.
 */
export function MirrorDialog(props: MirrorDialogProps) {
  return (
    <Modal
      opened={props.opened}
      onClose={props.onClose}
      title="Preslikaj vožnje iz suprotnog smjera"
      size="xl"
    >
      {props.opened && <MirrorForm {...props} />}
    </Modal>
  )
}

function MirrorForm({
  onClose,
  onSaved,
  routeId,
  direction,
  variants,
  initialTarget,
  initialSource,
  dayTypes,
}: MirrorDialogProps) {
  const routeVariants = useRouteVariants(routeId)
  const sources = (routeVariants.data ?? []).filter(
    (v) => v.directionId !== direction && v.tripCount > 0,
  )

  const [targetId, setTargetId] = useState<string | null>(
    variants.find((v) => v.id === initialTarget)?.id ?? variants[0]?.id ?? null,
  )
  const [sourceId, setSourceId] = useState<string | null>(initialSource)
  // Bez zadane izvorne varijante uzima se prva sa vožnjama (glavna, ako ih ima).
  const source = sources.find((v) => v.id === sourceId) ?? sources[0] ?? null
  const target = variants.find((v) => v.id === targetId) ?? null

  // Svaki par varijanti ima svoje zapamćene postavke.
  const pairKey = source && target ? `${source.id}.${target.id}` : null
  const [stored, setStored] = useState<{ pair: string | null; settings: MirrorSettings }>({
    pair: null,
    settings: defaults,
  })
  const [sourceKey, targetKey] = [source?.id ?? null, target?.id ?? null]
  const settings = stored.pair === pairKey ? stored.settings : loadSettings(sourceKey, targetKey)
  const update = (next: MirrorSettings) => {
    setStored({ pair: pairKey, settings: next })
    if (source && target) {
      storeSettings(source.id, target.id, next)
    }
  }

  const [replace, setReplace] = useState(false)
  // Ručno uključeni ili isključeni polasci; ostali prema tome postoje li već.
  const [picked, setPicked] = useState<Record<string, boolean>>({})

  const sourceSchedule = useSchedule(routeId, source?.directionId ?? null, null, source !== null)
  const targetSchedule = useSchedule(routeId, direction, null)
  const save = useSaveSchedule(routeId)

  const sourceTrips = (sourceSchedule.data?.trips ?? [])
    .filter((t) => t.variantId === source?.id)
    .map(fromResponse)
  const targetTrips = targetSchedule.data?.trips ?? []
  // Kod zamjene vožnje odabrane varijante nestaju, pa se s njima ne uspoređuje.
  const replaced = replace ? targetTrips.filter((t) => t.variantId === target?.id) : []
  const kept = targetTrips.filter((t) => !(replace && t.variantId === target?.id)).map(fromResponse)

  const duration = target ? (variantOffsets(target).arrival.at(-1) ?? 0) : 0
  const rows = target ? mirrorRows(sourceTrips, settings, kept, duration) : []
  const included = (row: (typeof rows)[number]) =>
    !row.tooLate && (picked[row.key] ?? !row.existing)
  const chosen = rows.filter(included)

  const names = new Map(
    [...(sourceSchedule.data?.stops ?? []), ...(targetSchedule.data?.stops ?? [])].map((s) => [
      s.id,
      s.name,
    ]),
  )
  const sourceEnd = source?.stops.at(-1)?.stopId
  const targetStart = target?.stops[0]?.stopId
  // Okret ima smisla kad povratak kreće otprilike odande gdje je polazak stigao.
  const turnMismatch =
    settings.mode === 'turn' &&
    sourceEnd !== undefined &&
    targetStart !== undefined &&
    sourceEnd !== targetStart &&
    names.get(sourceEnd) !== names.get(targetStart)

  const dayType = new Map(dayTypes.map((d) => [d.id, d]))

  const setOverride = (key: string, minutes: number | null) => {
    const overrides = { ...settings.overrides }
    if (minutes === null) {
      delete overrides[key]
    } else {
      overrides[key] = minutes * minute
    }
    update({ ...settings, overrides })
  }

  const submit = () => {
    if (!target) {
      return
    }
    const trips = mirrorTrips(chosen, target)
    save.mutate(
      {
        directionId: direction,
        upserts: changes([], trips).upserts,
        deletes: replaced.map((t) => ({ id: t.id, updatedAt: t.updatedAt })),
      },
      {
        onSuccess: () => {
          notifications.show({
            color: 'green',
            message: `Napravljeno ${trips.length} povratnih polazaka${replaced.length > 0 ? `, obrisano ${replaced.length} starih vožnji` : ''}.`,
          })
          onSaved()
        },
      },
    )
  }

  if (routeVariants.isPending) {
    return <PageLoader />
  }
  if (routeVariants.isError) {
    return <QueryError error={routeVariants.error} onRetry={() => void routeVariants.refetch()} />
  }
  if (sources.length === 0 || variants.length === 0) {
    return (
      <Alert color="yellow" variant="light">
        {variants.length === 0
          ? 'Ovaj smjer nema varijantu po kojoj bi nastale vožnje. Napravi je na stranici linije.'
          : 'Suprotni smjer nema varijantu s vožnjama, pa se nema što preslikati.'}
      </Alert>
    )
  }

  return (
    <Stack gap="sm">
      <Group grow align="flex-end">
        <Select
          label="Iz varijante"
          data={sources.map((v) => ({ value: v.id, label: `${v.name} (${v.tripCount})` }))}
          value={source?.id ?? null}
          onChange={(value) => {
            setSourceId(value)
            setPicked({})
          }}
          allowDeselect={false}
        />
        <Select
          label="Nove vožnje po varijanti"
          data={variants.map((v) => ({ value: v.id, label: v.name }))}
          value={targetId}
          onChange={(value) => {
            setTargetId(value)
            setPicked({})
          }}
          allowDeselect={false}
        />
      </Group>

      <Group gap="sm" align="flex-end">
        <SegmentedControl
          value={settings.mode}
          onChange={(mode) => update({ ...settings, mode: mode as MirrorSettings['mode'] })}
          data={[
            { value: 'turn', label: 'Okret na terminusu' },
            { value: 'offset', label: 'Pomak od polaska' },
          ]}
        />
        <NumberInput
          w={130}
          label={settings.mode === 'turn' ? 'Okret (min)' : 'Pomak (min)'}
          min={settings.mode === 'turn' ? 0 : -24 * 60}
          max={24 * 60}
          value={settings.offset / minute}
          onChange={(value) =>
            typeof value === 'number' && update({ ...settings, offset: value * minute })
          }
        />
        <Text size="xs" c="dimmed" maw={320}>
          {settings.mode === 'turn'
            ? 'Povratak kreće kad polazak stigne na zadnju stanicu, plus okret.'
            : 'Povratak kreće toliko minuta nakon polaska u suprotnom smjeru.'}{' '}
          Zaključani polasci 🔒 zadržavaju svoj okret.
        </Text>
      </Group>

      {(target?.tripCount ?? 0) > 0 && (
        <Checkbox
          label={`Zamijeni postojeće vožnje varijante „${target!.name}” (${target!.tripCount})`}
          checked={replace}
          onChange={(event) => {
            setReplace(event.currentTarget.checked)
            setPicked({})
          }}
        />
      )}

      {turnMismatch && (
        <Alert color="yellow" variant="light" p="xs">
          <Text size="sm">
            Polasci stižu na „{names.get(sourceEnd!)}”, a povratak kreće s „
            {names.get(targetStart!)}”. Provjeri okret ili odaberi drugu varijantu.
          </Text>
        </Alert>
      )}

      {sourceSchedule.isError || targetSchedule.isError ? (
        <QueryError
          error={sourceSchedule.error ?? targetSchedule.error}
          onRetry={() => {
            void sourceSchedule.refetch()
            void targetSchedule.refetch()
          }}
        />
      ) : sourceSchedule.isPending || targetSchedule.isPending ? (
        <PageLoader />
      ) : (
        <ScrollArea.Autosize mah={420} type="auto">
          <Table verticalSpacing={4} stickyHeader>
            <Table.Thead>
              <Table.Tr>
                <Table.Th w={36} />
                <Table.Th>Tipovi dana</Table.Th>
                <Table.Th w={80}>Polazak</Table.Th>
                <Table.Th w={80}>Dolazak</Table.Th>
                <Table.Th w={150}>{settings.mode === 'turn' ? 'Okret' : 'Pomak'} (min)</Table.Th>
                <Table.Th w={90}>Povratak</Table.Th>
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {rows.map((row) => (
                <Table.Tr key={row.key} opacity={included(row) ? 1 : 0.55}>
                  <Table.Td>
                    <Checkbox
                      size="xs"
                      checked={included(row)}
                      disabled={row.tooLate}
                      onChange={(event) =>
                        setPicked({ ...picked, [row.key]: event.currentTarget.checked })
                      }
                      aria-label="Napravi povratak"
                    />
                  </Table.Td>
                  <Table.Td>
                    <Group gap={4}>
                      {row.dayTypes.map((id) => (
                        <Badge
                          key={id}
                          size="xs"
                          variant="light"
                          color={dayType.get(id)?.color ?? 'gray'}
                          tt="none"
                        >
                          {dayType.get(id)?.label.split(' · ')[0] ?? '?'}
                        </Badge>
                      ))}
                    </Group>
                  </Table.Td>
                  <Table.Td>{formatTime(row.start)}</Table.Td>
                  <Table.Td>{formatTime(row.end)}</Table.Td>
                  <Table.Td>
                    <Group gap={4} wrap="nowrap">
                      <NumberInput
                        size="xs"
                        w={80}
                        min={settings.mode === 'turn' ? 0 : -24 * 60}
                        max={24 * 60}
                        value={row.offset / minute}
                        onChange={(value) =>
                          typeof value === 'number' && setOverride(row.key, value)
                        }
                        fw={row.locked ? 600 : undefined}
                        aria-label="Okret polaska"
                      />
                      <Tooltip
                        label={
                          row.locked
                            ? 'Zaključano; klik vraća zajednički okret'
                            : 'Zaključaj ovaj okret'
                        }
                      >
                        <ActionIcon
                          size="sm"
                          variant={row.locked ? 'light' : 'subtle'}
                          color={row.locked ? 'blue' : 'gray'}
                          onClick={() =>
                            setOverride(row.key, row.locked ? null : row.offset / minute)
                          }
                          aria-label={row.locked ? 'Otključaj okret' : 'Zaključaj okret'}
                        >
                          {row.locked ? <IconLock size={14} /> : <IconLockOpen size={14} />}
                        </ActionIcon>
                      </Tooltip>
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Group gap={4} wrap="nowrap">
                      <Text size="sm" fw={500}>
                        {formatTime(row.departure)}
                      </Text>
                      {row.tooLate ? (
                        <Badge size="xs" color="red" variant="light">
                          iza 48:00
                        </Badge>
                      ) : (
                        row.existing && (
                          <Tooltip label="U tom vremenu već postoji vožnja">
                            <Badge size="xs" color="yellow" variant="light">
                              postoji
                            </Badge>
                          </Tooltip>
                        )
                      )}
                    </Group>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        </ScrollArea.Autosize>
      )}

      {save.isError && (
        <Alert color="red" variant="light" title="Spremanje nije uspjelo">
          <Text size="sm">{errorMessage(save.error)}</Text>
        </Alert>
      )}

      <Group justify="space-between">
        <Text size="sm" c="dimmed">
          {chosen.length} od {rows.length} polazaka
          {replaced.length > 0 ? ` · briše se ${replaced.length} starih vožnji` : ''}
        </Text>
        <Group gap="xs">
          <Button variant="default" onClick={onClose}>
            Odustani
          </Button>
          <Button
            onClick={submit}
            loading={save.isPending}
            disabled={chosen.length === 0 && replaced.length === 0}
          >
            Napravi {chosen.length} povrataka
          </Button>
        </Group>
      </Group>
    </Stack>
  )
}
