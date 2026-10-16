import { Alert, Button, Group, Loader, Paper, Stack, Stepper, Text } from '@mantine/core'
import { Dropzone } from '@mantine/dropzone'
import { useInterval } from '@mantine/hooks'
import { IconAlertCircle, IconFileZip, IconUpload, IconX } from '@tabler/icons-react'
import { useState } from 'react'
import { errorMessage } from '@/api/errors'
import { formatBytes, formatNumber } from '@/lib/format'
import { GtfsImportReport } from './GtfsImportReport'
import {
  maxGtfsUploadBytes,
  useImportGtfs,
  usePreviewGtfs,
  type GtfsImportReport as Report,
} from './queries'

interface GtfsImportProps {
  /** Poziva se nakon uspješnog uvoza. */
  onImported: (report: Report) => void
  onCancel?: () => void
}

/** Uvoz GTFS feeda: odabir datoteke, pregled s izvještajem, potvrda i uvoz. */
export function GtfsImport({ onImported, onCancel }: GtfsImportProps) {
  const [file, setFile] = useState<File | null>(null)
  const [rejection, setRejection] = useState<string | null>(null)
  const preview = usePreviewGtfs()
  const importFeed = useImportGtfs()

  const report = importFeed.data ?? preview.data
  const step = importFeed.isPending || importFeed.data ? 2 : file ? 1 : 0

  const selectFile = (selected: File) => {
    setRejection(null)
    setFile(selected)
    importFeed.reset()
    preview.mutate(selected)
  }

  const reset = () => {
    setFile(null)
    preview.reset()
    importFeed.reset()
  }

  const confirmImport = () => {
    if (file) {
      importFeed.mutate(file, {
        onSuccess: (result) => result.imported && onImported(result),
      })
    }
  }

  return (
    <Stack gap="lg">
      <Stepper active={step} size="sm" allowNextStepsSelect={false}>
        <Stepper.Step label="Datoteka" description="GTFS ZIP" />
        <Stepper.Step label="Pregled" description="Provjera feeda" />
        <Stepper.Step label="Uvoz" description="Upis podataka" />
      </Stepper>

      {!file && (
        <>
          <Dropzone
            onDrop={(files) => selectFile(files[0])}
            onReject={(rejections) =>
              setRejection(rejectionMessage(rejections[0]?.errors[0]?.code))
            }
            accept={{ 'application/zip': ['.zip'], 'application/x-zip-compressed': ['.zip'] }}
            maxSize={maxGtfsUploadBytes}
            maxFiles={1}
            multiple={false}
          >
            <Group justify="center" gap="lg" mih={140} style={{ pointerEvents: 'none' }}>
              <Dropzone.Accept>
                <IconUpload size={44} stroke={1.5} />
              </Dropzone.Accept>
              <Dropzone.Reject>
                <IconX size={44} stroke={1.5} />
              </Dropzone.Reject>
              <Dropzone.Idle>
                <IconFileZip size={44} stroke={1.5} />
              </Dropzone.Idle>
              <div>
                <Text size="lg">Povuci GTFS ZIP ovdje ili klikni za odabir</Text>
                <Text size="sm" c="dimmed">
                  Najviše {formatBytes(maxGtfsUploadBytes)}. Ništa se ne upisuje prije potvrde.
                </Text>
              </div>
            </Group>
          </Dropzone>
          {rejection && (
            <Alert color="red" variant="light" icon={<IconAlertCircle />}>
              {rejection}
            </Alert>
          )}
          {onCancel && (
            <Group justify="flex-end">
              <Button variant="default" onClick={onCancel}>
                Natrag
              </Button>
            </Group>
          )}
        </>
      )}

      {file && (
        <>
          <Paper withBorder p="sm">
            <Group gap="sm" wrap="nowrap">
              <IconFileZip size={24} stroke={1.5} />
              <div style={{ minWidth: 0 }}>
                <Text size="sm" fw={500} truncate>
                  {file.name}
                </Text>
                <Text size="xs" c="dimmed">
                  {formatBytes(file.size)}
                </Text>
              </div>
            </Group>
          </Paper>

          {preview.isPending && (
            <Progress
              label="Provjera feeda…"
              hint="Veći feedovi se provjeravaju nekoliko sekundi."
            />
          )}

          {preview.isError && (
            <Alert
              color="red"
              variant="light"
              icon={<IconAlertCircle />}
              title="Provjera nije uspjela"
            >
              {errorMessage(preview.error)}
            </Alert>
          )}

          {importFeed.isPending && (
            <Progress
              label="Uvoz u tijeku…"
              hint="Upis velikog feeda može potrajati i do minute. Ne zatvaraj stranicu."
            />
          )}

          {importFeed.isError && (
            <Alert color="red" variant="light" icon={<IconAlertCircle />} title="Uvoz nije uspio">
              {errorMessage(importFeed.error)}
            </Alert>
          )}

          {report && !importFeed.isPending && (
            <>
              {report.canImport && !report.imported && <ExistingDataWarning report={report} />}
              <GtfsImportReport report={report} />
            </>
          )}

          {!importFeed.isPending && !report?.imported && (
            <Group justify="flex-end">
              <Button variant="default" onClick={reset} disabled={preview.isPending}>
                Druga datoteka
              </Button>
              <Button
                onClick={confirmImport}
                disabled={!preview.data?.canImport || importFeed.isError}
              >
                Uvezi
              </Button>
            </Group>
          )}
        </>
      )}
    </Stack>
  )
}

function ExistingDataWarning({ report }: { report: Report }) {
  const { agencies, stops, routes, trips } = report.existing
  if (agencies + stops + routes + trips === 0) {
    return null
  }
  return (
    <Alert
      color="orange"
      variant="light"
      icon={<IconAlertCircle />}
      title="Uvoz zamjenjuje postojeći vozni red"
    >
      Obrisat će se {formatNumber(stops)} stanica, {formatNumber(routes)} linija i{' '}
      {formatNumber(trips)} vožnji. Postavke i administratori ostaju.
    </Alert>
  )
}

function Progress({ label, hint }: { label: string; hint: string }) {
  const seconds = useElapsedSeconds()
  return (
    <Paper withBorder p="lg">
      <Group gap="md" wrap="nowrap">
        <Loader size="sm" />
        <div>
          <Text fw={500}>
            {label}{' '}
            {seconds > 0 && (
              <Text span c="dimmed">
                {seconds} s
              </Text>
            )}
          </Text>
          <Text size="sm" c="dimmed">
            {hint}
          </Text>
        </div>
      </Group>
    </Paper>
  )
}

function useElapsedSeconds() {
  const [seconds, setSeconds] = useState(0)
  useInterval(() => setSeconds((value) => value + 1), 1000, { autoInvoke: true })
  return seconds
}

function rejectionMessage(code: string | undefined) {
  switch (code) {
    case 'file-too-large':
      return `Datoteka je veća od ${formatBytes(maxGtfsUploadBytes)}.`
    case 'file-invalid-type':
      return 'GTFS feed mora biti ZIP datoteka.'
    default:
      return 'Odaberi jednu ZIP datoteku.'
  }
}
