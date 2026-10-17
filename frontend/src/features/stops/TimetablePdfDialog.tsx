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
import { type Stop, type TimetablePdfParams, useTimetablePdf } from './queries'

type Paper = NonNullable<TimetablePdfParams['Paper']>
type Language = NonNullable<TimetablePdfParams['Language']>

interface TimetablePdfDialogProps {
  stop: Stop
  opened: boolean
  onClose: () => void
}

/**
 * Izbor razdoblja, formata i jezika za PDF vozni red stanice. Razdoblje je
 * zadano od danas do zadnjeg dana s voznim redom, pa se novi vozni red može
 * izraditi i unaprijed, od dana kad počinje vrijediti.
 */
export function TimetablePdfDialog({ stop, opened, onClose }: TimetablePdfDialogProps) {
  const overview = useServiceOverview()
  const settings = useSettings()
  const pdf = useTimetablePdf(stop.id)

  const [range, setRange] = useState<DatesRangeValue<string> | null>(null)
  const [paper, setPaper] = useState<Paper>('A4')
  const [language, setLanguage] = useState<Language>('Hr')

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
      { From: from ?? undefined, To: to ?? undefined, Paper: paper, Language: language },
      {
        onSuccess: (file) => {
          saveFile(file)
          close()
        },
      },
    )

  const error = pdf.error instanceof ApiError ? pdf.error : null
  const rangeError = error?.fieldErrors.to?.[0] ?? error?.fieldErrors.from?.[0]
  const problems = error?.problems ?? []

  return (
    <Modal opened={opened} onClose={close} title="PDF vozni red" size="md">
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          Polasci sa stanice {stop.name} po linijama i smjerovima, u stupcima po danima u tjednu.
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
