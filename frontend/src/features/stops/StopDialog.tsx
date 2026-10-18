import {
  Alert,
  Button,
  Group,
  Modal,
  SegmentedControl,
  Select,
  SimpleGrid,
  Stack,
  Switch,
  Text,
  TextInput,
} from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { useState } from 'react'
import { ApiError, errorMessage } from '@/api/errors'
import { type Stop, type StopRequest, useSaveStop, useStops } from './queries'
import { nextStopCode, suggestStopCode } from './stopCode'

/** Početne vrijednosti nove stanice (npr. peron kolodvora preuzima naziv). */
export interface NewStop {
  latitude: number
  longitude: number
  name?: string
  parentStationId?: string | null
  locationType?: number
}

interface StopDialogProps {
  /** Stanica za izmjenu; bez nje nova stanica na `create`. */
  stop?: Stop
  create?: NewStop | null
  opened: boolean
  onClose: () => void
  onSaved?: (stop: Stop) => void
}

/** Nova stanica ili izmjena postojeće: naziv, peron, vrsta, kolodvor, pristupačnost. */
export function StopDialog({ stop, create, opened, onClose, onSaved }: StopDialogProps) {
  return (
    <Modal opened={opened} onClose={onClose} title={stop ? 'Uredi stanicu' : 'Nova stanica'}>
      {opened && (stop || create) && (
        <StopForm stop={stop} create={create ?? null} onClose={onClose} onSaved={onSaved} />
      )}
    </Modal>
  )
}

function StopForm({
  stop,
  create,
  onClose,
  onSaved,
}: {
  stop?: Stop
  create: NewStop | null
  onClose: () => void
  onSaved?: (stop: Stop) => void
}) {
  const save = useSaveStop(stop?.id)
  const [name, setName] = useState(stop?.name ?? create?.name ?? '')
  const [platformCode, setPlatformCode] = useState(stop?.platformCode ?? '')
  const [publicCode, setPublicCode] = useState(stop?.publicCode ?? '')
  // Prazan kod nove stanice: predlaže se iz naziva i perona.
  const [code, setCode] = useState(stop?.code ?? '')
  const [locationType, setLocationType] = useState(
    String(stop?.locationType ?? create?.locationType ?? 0),
  )
  const [parentId, setParentId] = useState<string | null>(
    stop?.parentStationId ?? create?.parentStationId ?? null,
  )
  const [wheelchair, setWheelchair] = useState(String(stop?.wheelchairBoarding ?? 0))
  const [isActive, setIsActive] = useState(stop?.isActive ?? true)
  const [saving, setSaving] = useState(false)

  // Kolodvori za odabir nadređene stanice, traže se po nazivu.
  const [stationSearch, setStationSearch] = useState('')
  const [search] = useDebouncedValue(stationSearch, 250)
  const stations = useStops({ LocationType: 1, Search: search || undefined, PageSize: 20 })
  const stationOptions = [
    ...(stations.data?.items ?? [])
      .filter((s) => s.id !== stop?.id)
      .map((s) => ({ value: s.id, label: s.name })),
  ]
  if (parentId && !stationOptions.some((o) => o.value === parentId)) {
    stationOptions.unshift({ value: parentId, label: 'Trenutni kolodvor' })
  }

  // Samo stajalište i kolodvor uređuju se ovdje; ostale vrste (ulazi, čvorovi) ostaju kakve jesu.
  const simpleType = Number(locationType) <= 1
  const autoCode = !stop && !code.trim()
  const shownCode = code.trim() || suggestStopCode(name, platformCode)

  const error = save.error instanceof ApiError ? save.error : null
  const fieldError = (key: string) => error?.fieldErrors[key]?.[0]

  const submit = async () => {
    const base: StopRequest = {
      code: shownCode,
      publicCode: publicCode.trim() || null,
      name: name.trim(),
      ttsName: stop?.ttsName ?? null,
      description: stop?.description ?? null,
      zoneId: stop?.zoneId ?? null,
      url: stop?.url ?? null,
      timezone: stop?.timezone ?? null,
      latitude: stop ? stop.latitude : round(create!.latitude),
      longitude: stop ? stop.longitude : round(create!.longitude),
      locationType: Number(locationType),
      // Kolodvor ne može imati nadređeni kolodvor.
      parentStationId: locationType === '1' ? null : parentId,
      platformCode: platformCode.trim() || null,
      wheelchairBoarding: Number(wheelchair),
      isActive,
    }
    setSaving(true)
    try {
      // Predloženi kod može biti zauzet (isti naziv drugdje), pa se dodaje broj.
      for (let attempt = 0; ; attempt++) {
        try {
          const saved = await save.mutateAsync({
            ...base,
            code: autoCode ? nextStopCode(shownCode, attempt) : shownCode,
          })
          onSaved?.(saved)
          onClose()
          return
        } catch (e) {
          if (!(autoCode && e instanceof ApiError && e.status === 409 && attempt < 20)) {
            return
          }
        }
      }
    } finally {
      setSaving(false)
    }
  }

  return (
    <Stack gap="sm">
      <SimpleGrid cols={2}>
        <TextInput
          label="Naziv"
          value={name}
          onChange={(event) => setName(event.currentTarget.value)}
          error={fieldError('name')}
          data-autofocus
          style={{ gridColumn: 'span 2' }}
        />
        <TextInput
          label="Peron"
          placeholder="npr. A ili 1"
          value={platformCode}
          onChange={(event) => setPlatformCode(event.currentTarget.value)}
          disabled={locationType === '1'}
        />
        <TextInput
          label="Oznaka za putnike"
          placeholder="npr. broj na stupu"
          value={publicCode}
          onChange={(event) => setPublicCode(event.currentTarget.value)}
        />
      </SimpleGrid>

      {simpleType && (
        <Stack gap={4}>
          <Text size="sm" fw={500}>
            Vrsta
          </Text>
          <SegmentedControl
            data={[
              { value: '0', label: 'Stajalište' },
              { value: '1', label: 'Kolodvor (s peronima)' },
            ]}
            value={locationType}
            onChange={(value) => setLocationType(String(value))}
          />
          {fieldError('locationType') && (
            <Text size="xs" c="red">
              {fieldError('locationType')}
            </Text>
          )}
        </Stack>
      )}
      {locationType !== '1' && (
        <Select
          label="Kolodvor kojem pripada"
          placeholder="Samostalno stajalište"
          description="Peroni istog kolodvora dijele zaslon i vozni red kolodvora."
          data={stationOptions}
          value={parentId}
          onChange={(value) => setParentId(value === null ? null : String(value))}
          searchable
          clearable
          searchValue={stationSearch}
          onSearchChange={setStationSearch}
          nothingFoundMessage="Nema kolodvora"
          error={fieldError('parentStationId')}
        />
      )}

      <Stack gap={4}>
        <Text size="sm" fw={500}>
          Pristupačnost za invalidska kolica
        </Text>
        <SegmentedControl
          data={[
            { value: '0', label: 'Nepoznato' },
            { value: '1', label: 'Da' },
            { value: '2', label: 'Ne' },
          ]}
          value={wheelchair}
          onChange={(value) => setWheelchair(String(value))}
        />
      </Stack>

      <TextInput
        label="GTFS ID"
        description={
          stop
            ? 'Mijenjaj samo ako znaš zašto: koriste ga zasloni i izvoz.'
            : 'Zadano iz naziva i perona'
        }
        placeholder={shownCode}
        value={code}
        onChange={(event) => setCode(event.currentTarget.value)}
        error={fieldError('code')}
      />
      {stop && (
        <Switch
          label="Aktivna"
          description="Oznaka za tebe: neaktivna stanica je siva na karti, a podaci ostaju."
          checked={isActive}
          onChange={(event) => setIsActive(event.currentTarget.checked)}
        />
      )}

      {save.isError && Object.keys(error?.fieldErrors ?? {}).length === 0 && (
        <Alert color="red" variant="light">
          {errorMessage(save.error)}
        </Alert>
      )}

      <Group justify="flex-end">
        <Button variant="default" onClick={onClose}>
          Odustani
        </Button>
        <Button
          onClick={() => void submit()}
          loading={saving}
          disabled={!name.trim() || !shownCode}
        >
          {stop ? 'Spremi' : 'Dodaj stanicu'}
        </Button>
      </Group>
    </Stack>
  )
}

/** Šest decimala je oko 10 cm. */
function round(value: number) {
  return Math.round(value * 1e6) / 1e6
}
