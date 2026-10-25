import {
  Alert,
  Button,
  Group,
  Input,
  List,
  Modal,
  SegmentedControl,
  Stack,
  Text,
} from '@mantine/core'
import { IconAlertCircle, IconDownload } from '@tabler/icons-react'
import { useState } from 'react'
import { ApiError, errorMessage } from '@/api/errors'
import { saveFile } from '@/lib/download'
import { type Route, type RouteDiagramPdfParams, useRouteDiagramPdf } from './queries'

type Paper = NonNullable<RouteDiagramPdfParams['Paper']>
type Language = NonNullable<RouteDiagramPdfParams['Language']>

interface RouteDiagramPdfDialogProps {
  route: Route
  opened: boolean
  onClose: () => void
}

/** Izbor formata i jezika za PDF shemu linije (redoslijed stanica oba smjera). */
export function RouteDiagramPdfDialog({ route, opened, onClose }: RouteDiagramPdfDialogProps) {
  const pdf = useRouteDiagramPdf(route.id)

  const [paper, setPaper] = useState<Paper>('A4')
  const [language, setLanguage] = useState<Language>('Hr')

  const close = () => {
    pdf.reset()
    onClose()
  }

  const submit = () =>
    pdf.mutate(
      { Paper: paper, Language: language },
      {
        onSuccess: (file) => {
          saveFile(file)
          close()
        },
      },
    )

  const problems = pdf.error instanceof ApiError ? pdf.error.problems : []

  return (
    <Modal opened={opened} onClose={close} title="PDF shema linije" size="md">
      <Stack gap="md">
        <Text size="sm" c="dimmed">
          Stanice glavnih varijanti oba smjera redom kojim ih linija obilazi, s minutama vožnje od
          polazne stanice. Gdje smjerovi idu različitim stanicama, crta se razdvaja u dvije trake sa
          strelicama.
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
