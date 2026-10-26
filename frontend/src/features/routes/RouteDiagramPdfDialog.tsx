import {
  Alert,
  Button,
  Group,
  Input,
  List,
  Modal,
  SegmentedControl,
  Stack,
  Switch,
  Text,
} from '@mantine/core'
import { IconAlertCircle, IconDownload } from '@tabler/icons-react'
import { useState } from 'react'
import { ApiError, errorMessage } from '@/api/errors'
import { saveFile } from '@/lib/download'
import type { RouteDiagramPdfMutation, RouteDiagramPdfParams } from './queries'

type Paper = NonNullable<RouteDiagramPdfParams['Paper']>
type Language = NonNullable<RouteDiagramPdfParams['Language']>

interface RouteDiagramPdfDialogProps {
  title: string
  description: string
  /** Preuzimanje PDF-a: sheme jedne linije ili svih linija sa stanice. */
  pdf: RouteDiagramPdfMutation
  /** PDF sa stanice: minute se mogu računati i od te stanice. */
  fromStop?: boolean
  opened: boolean
  onClose: () => void
}

/** Izbor formata, jezika i minuta za PDF shemu linije (ili više njih). */
export function RouteDiagramPdfDialog({
  title,
  description,
  pdf,
  fromStop = false,
  opened,
  onClose,
}: RouteDiagramPdfDialogProps) {
  const [paper, setPaper] = useState<Paper>('A4')
  const [language, setLanguage] = useState<Language>('Hr')
  const [showMinutes, setShowMinutes] = useState(false)
  const [minutesFrom, setMinutesFrom] = useState<'start' | 'stop'>('start')

  const close = () => {
    pdf.reset()
    onClose()
  }

  const submit = () =>
    pdf.mutate(
      {
        Paper: paper,
        Language: language,
        ShowMinutes: showMinutes,
        MinutesFromStop: fromStop && minutesFrom === 'stop',
      },
      {
        onSuccess: (file) => {
          saveFile(file)
          close()
        },
      },
    )

  const problems = pdf.error instanceof ApiError ? pdf.error.problems : []

  return (
    <Modal opened={opened} onClose={close} title={title} size="md">
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          {description}
        </Text>

        <Input.Wrapper label="Format">
          <SegmentedControl
            fullWidth
            value={paper}
            onChange={(value) => setPaper(value as Paper)}
            data={[
              { value: 'A4', label: 'A4' },
              { value: 'A3', label: 'A3' },
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

        <Switch
          label="Minute vožnje uz stanice"
          description="Uz svaku stanicu minute od polazne stanice, za oba smjera."
          checked={showMinutes}
          onChange={(event) => setShowMinutes(event.currentTarget.checked)}
        />

        {fromStop && showMinutes && (
          <Input.Wrapper
            label="Minute računati"
            description="Od ove stanice: koliko se vozi od nje do stanica iza nje, u svakom smjeru."
          >
            <SegmentedControl
              fullWidth
              value={minutesFrom}
              onChange={(value) => setMinutesFrom(value as 'start' | 'stop')}
              data={[
                { value: 'start', label: 'Od polazne stanice' },
                { value: 'stop', label: 'Od ove stanice' },
              ]}
            />
          </Input.Wrapper>
        )}

        {pdf.isError && (
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
          <Button leftSection={<IconDownload size={16} />} loading={pdf.isPending} onClick={submit}>
            Preuzmi PDF
          </Button>
        </Group>
      </Stack>
    </Modal>
  )
}
