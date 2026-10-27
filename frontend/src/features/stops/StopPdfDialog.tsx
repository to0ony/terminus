import {
  Alert,
  Anchor,
  Button,
  Checkbox,
  Group,
  Input,
  List,
  Modal,
  SegmentedControl,
  Stack,
  Switch,
  Text,
} from '@mantine/core'
import { DatePickerInput, type DatesRangeValue } from '@mantine/dates'
import { notifications } from '@mantine/notifications'
import { IconAlertCircle, IconCalendar, IconDownload } from '@tabler/icons-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { ApiError, errorMessage } from '@/api/errors'
import { useServiceOverview } from '@/features/dashboard/queries'
import { useSettings } from '@/features/settings/queries'
import { saveFile } from '@/lib/download'
import { type StopPdfRequest, useStopPdf } from './queries'

type Paper = NonNullable<StopPdfRequest['paper']>
type Language = NonNullable<StopPdfRequest['language']>

interface StopPdfDialogProps {
  /** Jedna stanica ili više njih (jedan PDF, svaka od nove stranice). */
  stops: { id: string; name: string }[]
  opened: boolean
  onClose: () => void
}

/**
 * PDF stanice ili odabranih stanica: vozni red, sheme linija koje na njoj
 * staju ili oboje. Razdoblje voznog reda je zadano od danas do zadnjeg dana s
 * voznim redom, pa se novi vozni red može izraditi i unaprijed.
 */
export function StopPdfDialog({ stops, opened, onClose }: StopPdfDialogProps) {
  const overview = useServiceOverview()
  const settings = useSettings()
  const pdf = useStopPdf()
  const many = stops.length > 1

  const [content, setContent] = useState<string[]>(['timetable'])
  const [range, setRange] = useState<DatesRangeValue<string> | null>(null)
  const [paper, setPaper] = useState<Paper>('A4')
  const [language, setLanguage] = useState<Language>('Hr')
  const [showMinutes, setShowMinutes] = useState(false)
  const [minutesFrom, setMinutesFrom] = useState<'start' | 'stop'>('start')

  const timetable = content.includes('timetable')
  const routes = content.includes('routes')
  const defaultRange: DatesRangeValue<string> = [
    overview.data?.date ?? null,
    overview.data?.lastServiceDate ?? null,
  ]
  const [from, to] = range ?? defaultRange

  const close = () => {
    pdf.reset()
    onClose()
  }

  const submit = () =>
    pdf.mutate(
      {
        ids: stops.map((s) => s.id),
        timetable,
        routes,
        from: timetable ? from : null,
        to: timetable ? to : null,
        paper,
        language,
        showMinutes: routes && showMinutes,
        minutesFromStop: routes && showMinutes && minutesFrom === 'stop',
      },
      {
        onSuccess: (file) => {
          saveFile(file)
          if (file.skipped > 0) {
            notifications.show({
              color: 'yellow',
              message: `${file.skipped} od ${stops.length} stanica nema ništa za ispis (polazaka ni linija), pa nisu u PDF-u.`,
            })
          }
          close()
        },
      },
    )

  const error = pdf.error instanceof ApiError ? pdf.error : null
  const rangeError = error?.fieldErrors.to?.[0] ?? error?.fieldErrors.from?.[0]
  const problems = error?.problems ?? []
  const names = stops
    .slice(0, 5)
    .map((s) => s.name)
    .join(', ')

  return (
    <Modal
      opened={opened}
      onClose={close}
      title={many ? `PDF stanica (${stops.length})` : `PDF · ${stops[0]?.name ?? ''}`}
      size="md"
    >
      <Stack gap="md">
        {many && (
          <Text size="sm" c="dimmed">
            Sve odabrane stanice u jednom PDF-u, svaka od nove stranice: {names}
            {stops.length > 5 ? ' …' : ''}. Stanice bez ičega za ispis se preskaču.
          </Text>
        )}

        <Checkbox.Group
          label="Što ispisati"
          value={content}
          onChange={setContent}
          error={content.length === 0 ? 'Odaberi barem jedno.' : undefined}
        >
          <Stack gap={8} mt="xs">
            <Checkbox
              value="timetable"
              label="Vozni red stanice"
              description="Polasci po linijama i smjerovima, u stupcima po danima u tjednu; praznici i odstupanja kao napomene."
            />
            <Checkbox
              value="routes"
              label="Sheme linija"
              description="Sheme svih linija koje staju na stanici, svaka na svojoj stranici, sa stanicom istaknutom."
            />
          </Stack>
        </Checkbox.Group>

        {timetable && (
          <DatePickerInput
            type="range"
            label="Razdoblje voznog reda"
            description="Zadano od danas do kraja voznog reda. Za novi vozni red odaberi dan od kojeg vrijedi."
            leftSection={<IconCalendar size={16} />}
            valueFormat="D. M. YYYY."
            value={[from, to]}
            onChange={setRange}
            allowSingleDateInRange
            error={rangeError}
          />
        )}

        <Input.Wrapper label="Format">
          <SegmentedControl
            fullWidth
            value={paper}
            onChange={(value) => setPaper(value as Paper)}
            data={[
              { value: 'A4', label: timetable ? 'A4 · dva stupca' : 'A4' },
              { value: 'A3', label: timetable ? 'A3 · tri stupca' : 'A3' },
            ]}
          />
        </Input.Wrapper>

        <Input.Wrapper label="Jezik">
          <SegmentedControl
            fullWidth
            value={language}
            onChange={(value) => setLanguage(value as Language)}
            data={[
              { value: 'Hr', label: 'Hrvatski' },
              { value: 'En', label: 'English' },
            ]}
          />
        </Input.Wrapper>

        {routes && (
          <Switch
            label="Minute vožnje uz stanice na shemama"
            description="Uz svaku stanicu minute vožnje, za oba smjera."
            checked={showMinutes}
            onChange={(event) => setShowMinutes(event.currentTarget.checked)}
          />
        )}

        {routes && showMinutes && (
          <Input.Wrapper
            label="Minute računati"
            description="Od stanice: koliko se vozi od nje do stanica iza nje, u svakom smjeru."
          >
            <SegmentedControl
              fullWidth
              value={minutesFrom}
              onChange={(value) => setMinutesFrom(value as 'start' | 'stop')}
              data={[
                { value: 'start', label: 'Od polazne stanice' },
                { value: 'stop', label: many ? 'Od svake stanice' : 'Od ove stanice' },
              ]}
            />
          </Input.Wrapper>
        )}

        {timetable && settings.data && !settings.data.publicUrl && (
          <Alert color="yellow" variant="light">
            Vozni red neće imati QR kod sa zaslonom polazaka dok se u{' '}
            <Anchor component={Link} to="/settings" size="sm">
              postavkama
            </Anchor>{' '}
            ne upiše javna adresa.
          </Alert>
        )}

        {pdf.isError && !rangeError && (
          <Alert color="red" variant="light" icon={<IconAlertCircle />} title="PDF nije izrađen">
            <Text size="sm">{errorMessage(pdf.error)}</Text>
            {problems.length > 0 && (
              <List size="sm" mt="xs" spacing={4}>
                {problems.map((problem) => (
                  <List.Item key={problem}>{problem}</List.Item>
                ))}
              </List>
            )}
          </Alert>
        )}

        <Group justify="flex-end">
          <Button variant="default" onClick={close}>
            Odustani
          </Button>
          <Button
            leftSection={<IconDownload size={16} />}
            loading={pdf.isPending}
            disabled={content.length === 0 || (timetable && (!from || !to))}
            onClick={submit}
          >
            Preuzmi PDF
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
