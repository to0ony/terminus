import {
  Alert,
  Box,
  Button,
  ColorInput,
  Group,
  Modal,
  Radio,
  Select,
  SimpleGrid,
  Stack,
  Switch,
  Text,
  TextInput,
} from '@mantine/core'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { ApiError, errorMessage } from '@/api/errors'
import { useAgencies } from '@/features/agencies/queries'
import { toHex } from '@/lib/color'
import { type Route, type TimeMode, useRoutes, useSaveRoute } from './queries'
import { routeTypeOptions } from './routeTypes'

interface RouteDialogProps {
  /** Linija za izmjenu; bez nje nova linija. */
  route?: Route
  opened: boolean
  onClose: () => void
}

/** Nova linija ili izmjena osnovnih podataka linije (oznaka, naziv, vrsta, boje). */
export function RouteDialog({ route, opened, onClose }: RouteDialogProps) {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={route ? 'Uredi liniju' : 'Nova linija'}
      size="lg"
    >
      {opened && <RouteForm route={route} onClose={onClose} />}
    </Modal>
  )
}

function RouteForm({ route, onClose }: { route?: Route; onClose: () => void }) {
  const navigate = useNavigate()
  const agencies = useAgencies()
  const save = useSaveRoute(route?.id)

  const [shortName, setShortName] = useState(route?.shortName ?? '')
  const [longName, setLongName] = useState(route?.longName ?? '')
  // GTFS route_id: za novu liniju zadano kratka oznaka.
  const [code, setCode] = useState<string | null>(route?.code ?? null)
  const [agencyId, setAgencyId] = useState<string | null>(route?.agencyId ?? null)
  const [routeType, setRouteType] = useState(String(route?.routeType ?? 3))
  const [color, setColor] = useState(route?.color ? `#${route.color}` : '#1565C0')
  const [textColor, setTextColor] = useState(route?.textColor ? `#${route.textColor}` : '#FFFFFF')
  const [isActive, setIsActive] = useState(route?.isActive ?? true)
  // Nova linija zadano preuzima način koji ima većina postojećih linija.
  const routes = useRoutes({ PageSize: 100 })
  const approximateCount =
    routes.data?.items.filter((r) => r.timeMode === 'Approximate').length ?? 0
  const usual: TimeMode =
    approximateCount * 2 > (routes.data?.items.length ?? 0) ? 'Approximate' : 'Exact'
  const [timeMode, setTimeMode] = useState<TimeMode | null>(route?.timeMode ?? null)
  const shownTimeMode = timeMode ?? usual

  const agency = agencyId ?? agencies.data?.[0]?.id ?? null
  const shownCode = code ?? shortName.trim()
  const error = save.error instanceof ApiError ? save.error : null
  const fieldError = (key: string) => error?.fieldErrors[key]?.[0]

  const submit = () => {
    if (!agency) {
      return
    }
    save.mutate(
      {
        // Polja koja forma ne prikazuje ostaju kakva jesu (PUT zamjenjuje cijelu liniju).
        description: route?.description ?? null,
        url: route?.url ?? null,
        sortOrder: route?.sortOrder ?? null,
        continuousPickup: route?.continuousPickup ?? null,
        continuousDropOff: route?.continuousDropOff ?? null,
        networkId: route?.networkId ?? null,
        agencyId: agency,
        code: shownCode,
        shortName: shortName.trim() || null,
        longName: longName.trim() || null,
        routeType: Number(routeType),
        color: toHex(color),
        textColor: toHex(textColor),
        timeMode: shownTimeMode,
        isActive,
      },
      {
        onSuccess: (saved) => {
          onClose()
          if (!route) {
            void navigate(`/routes/${saved.id}`)
          }
        },
      },
    )
  }

  return (
    <Stack gap="md">
      <SimpleGrid cols={{ base: 1, xs: 2 }}>
        <TextInput
          label="Oznaka linije"
          description="Na vozilima i stanicama, npr. 6 ili 101"
          value={shortName}
          onChange={(event) => setShortName(event.currentTarget.value)}
          error={fieldError('shortName')}
          data-autofocus
        />
        <TextInput
          label="GTFS ID"
          description="Jedinstven; zadano oznaka linije"
          value={shownCode}
          onChange={(event) => setCode(event.currentTarget.value)}
          error={fieldError('code')}
        />
      </SimpleGrid>
      <TextInput
        label="Naziv"
        placeholder="npr. Črnomerec – Sopot"
        value={longName}
        onChange={(event) => setLongName(event.currentTarget.value)}
        error={fieldError('longName')}
      />
      <SimpleGrid cols={{ base: 1, xs: 2 }}>
        <Select
          label="Vrsta prijevoza"
          data={routeTypeOptions}
          value={routeType}
          onChange={(value) => value && setRouteType(value)}
          allowDeselect={false}
          error={fieldError('routeType')}
        />
        {(agencies.data?.length ?? 0) > 1 && (
          <Select
            label="Prijevoznik"
            data={agencies.data!.map((a) => ({ value: a.id, label: a.name }))}
            value={agency}
            onChange={(value) => setAgencyId(value === null ? null : String(value))}
            allowDeselect={false}
            error={fieldError('agencyId')}
          />
        )}
      </SimpleGrid>
      <SimpleGrid cols={{ base: 1, xs: 2 }}>
        <ColorInput
          label="Boja"
          format="hex"
          value={color}
          onChange={setColor}
          error={fieldError('color')}
        />
        <ColorInput
          label="Boja teksta"
          format="hex"
          swatches={['#FFFFFF', '#000000']}
          value={textColor}
          onChange={setTextColor}
          error={fieldError('textColor')}
        />
      </SimpleGrid>
      <Group gap="sm">
        <Box
          px={10}
          py={2}
          style={{ background: color, color: textColor, borderRadius: 4, fontWeight: 700 }}
        >
          {shortName || '?'}
        </Box>
        <Text size="sm" c="dimmed">
          Pregled oznake
        </Text>
      </Group>
      <Radio.Group
        label="Vremena po stanicama"
        value={shownTimeMode}
        onChange={(value) => setTimeMode(value as TimeMode)}
        error={fieldError('timeMode')}
      >
        <Stack gap="xs" mt={4}>
          <Radio
            value="Exact"
            label="Točna"
            description="Vremena dolaze iz varijante i mogu se dotjerati za svaku vožnju."
          />
          <Radio
            value="Approximate"
            label="Samo polasci (okvirno)"
            description="Zadaje se polazak i dolazak na terminus; vremena međustanica Terminus raspoređuje sam i označi kao okvirna."
          />
        </Stack>
      </Radio.Group>
      {route && shownTimeMode !== route.timeMode && (
        <Alert color="yellow" variant="light" p="xs">
          <Text size="sm">
            {shownTimeMode === 'Approximate'
              ? 'Postojeće vožnje zadržavaju svoja vremena, ali vremena međustanica postaju okvirna.'
              : 'Postojeće vožnje zadržavaju svoja vremena, a vremena međustanica postaju točna. Provjeri ih, jer su bila raspoređena okvirno.'}
          </Text>
        </Alert>
      )}
      <Switch
        label="Aktivna"
        description="Neaktivna linija ne ide u GTFS izvoz ni na zaslone."
        checked={isActive}
        onChange={(event) => setIsActive(event.currentTarget.checked)}
      />

      {save.isError && Object.keys(error?.fieldErrors ?? {}).length === 0 && (
        <Alert color="red" variant="light">
          {errorMessage(save.error)}
        </Alert>
      )}

      <Group justify="flex-end">
        <Button variant="default" onClick={onClose}>
          Odustani
        </Button>
        <Button onClick={submit} loading={save.isPending} disabled={!agency || !shownCode}>
          {route ? 'Spremi' : 'Napravi liniju'}
        </Button>
      </Group>
    </Stack>
  )
}
