import {
  Alert,
  Anchor,
  Button,
  Group,
  Input,
  List,
  Modal,
  SegmentedControl,
  Stack,
  Text,
} from '@mantine/core'
import { DatePickerInput, type DatesRangeValue } from '@mantine/dates'
import { IconAlertCircle, IconCalendar, IconDownload } from '@tabler/icons-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { ApiError, errorMessage } from '@/api/errors'
import { useServiceOverview } from '@/features/dashboard/queries'
import { useSettings } from '@/features/settings/queries'
import { saveFile } from '@/lib/download'
import { notifications } from '@mantine/notifications'
import { type TimetablePdfParams, useStopTimetablesPdf, useTimetablePdf } from './queries'

type Paper = NonNullable<TimetablePdfParams['Paper']>
type Language = NonNullable<TimetablePdfParams['Language']>

interface TimetablePdfDialogProps {
  /** Jedna stanica ili više njih (jedan PDF, svaka od nove stranice). */
  stops: { id: string; name: string }[]
  opened: boolean
  onClose: () => void
}

/**
 * Izbor razdoblja, formata i jezika za PDF vozni red stanice. Razdoblje je
 * zadano od danas do zadnjeg dana s voznim redom, pa se novi vozni red može
 * izraditi i unaprijed, od dana kad počinje vrijediti.
 */
export function TimetablePdfDialog({ stops, opened, onClose }: TimetablePdfDialogProps) {
  const overview = useServiceOverview()
  const settings = useSettings()
  const many = stops.length > 1
  const one = useTimetablePdf(stops[0]?.id ?? '')
  const several = useStopTimetablesPdf()
  const pdf = many ? several : one

  const [range, setRange] = useState<DatesRangeValue<string> | null>(null)
  const [paper, setPaper] = useState<Paper>('A4')
  const [language, setLanguage] = useState<Language>('Hr')

  const defaultRange: DatesRangeValue<string> = [
    overview.data?.date ?? null,
    overview.data?.lastServiceDate ?? null,
  ]
  const [from, to] = range ?? defaultRange

  const close = () => {
    one.reset()
    several.reset()
    onClose()
  }

  const submit = () => {
    const params = {
      From: from ?? undefined,
      To: to ?? undefined,
      Paper: paper,
      Language: language,
    }
    if (!many) {
      one.mutate(params, {
        onSuccess: (file) => {
          saveFile(file)
          close()
        },
      })
      return
    }
    several.mutate(
      { ...params, ids: stops.map((s) => s.id) },
      {
        onSuccess: (file) => {
          saveFile(file)
          if (file.skipped > 0) {
            notifications.show({
              color: 'yellow',
              message: `${file.skipped} od ${stops.length} stanica u tom razdoblju nema polazaka, pa nisu u PDF-u.`,
            })
          }
          close()
        },
      },
    )
  }

  const error = pdf.error instanceof ApiError ? pdf.error : null
  const rangeError = error?.fieldErrors.to?.[0] ?? error?.fieldErrors.from?.[0]
  const problems = error?.problems ?? []

  return (
    <Modal
      opened={opened}
      onClose={close}
      title={many ? `PDF vozni redovi (${stops.length} stanica)` : 'PDF vozni red'}
      size="md"
    >
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          {many
            ? `Vozni redovi odabranih stanica u jednom PDF-u, svaka od nove stranice: ${stops
                .slice(0, 5)
                .map((s) => s.name)
                .join(', ')}${stops.length > 5 ? ' …' : ''}. Stanice bez polazaka se preskaču.`
            : `Polasci sa stanice ${stops[0]?.name ?? ''} po linijama i smjerovima, u stupcima po danima u tjednu.`}{' '}
          Praznici i ostala odstupanja ispisuju se kao napomene.
        </Text>

        <DatePickerInput
          type="range"
          label="Razdoblje"
          description="Zadano od danas do kraja voznog reda. Za novi vozni red odaberi dan od kojeg vrijedi."
          leftSection={<IconCalendar size={16} />}
          valueFormat="D. M. YYYY."
          value={[from, to]}
          onChange={setRange}
          allowSingleDateInRange
          error={rangeError}
        />

        <Input.Wrapper label="Format">
          <SegmentedControl
            fullWidth
            value={paper}
            onChange={(value) => setPaper(value as Paper)}
            data={[
              { value: 'A4', label: 'A4 · dva stupca' },
              { value: 'A3', label: 'A3 · tri stupca' },
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

        {settings.data && !settings.data.publicUrl && (
          <Alert color="yellow" variant="light">
            PDF neće imati QR kod sa zaslonom polazaka dok se u{' '}
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
            disabled={!from || !to}
            onClick={submit}
          >
            Preuzmi PDF
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
