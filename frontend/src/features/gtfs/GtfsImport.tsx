import {
  Alert,
  Anchor,
  Button,
  Group,
  Loader,
  Paper,
  Stack,
  Stepper,
  Switch,
  Text,
} from '@mantine/core'
import { Dropzone } from '@mantine/dropzone'
import { useDisclosure, useInterval } from '@mantine/hooks'
import {
  IconAlertCircle,
  IconAlertTriangle,
  IconFileZip,
  IconInfoCircle,
  IconUpload,
  IconX,
} from '@tabler/icons-react'
import { useState } from 'react'
import { Link } from 'react-router'
import { errorMessage } from '@/api/errors'
import { useSetupStatus } from '@/features/setup/queries'
import { formatBytes, formatNumber } from '@/lib/format'
import { GtfsImportReport } from './GtfsImportReport'
import { ReplaceConfirmDialog } from './ReplaceConfirmDialog'
import { VariantSelection } from './VariantSelection'
import {
  hasExistingData,
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

/**
 * Uvoz GTFS feeda koji zamjenjuje sve podatke voznog reda: odabir datoteke,
 * pregled s izvještajem, odabir što se uvozi, potvrda i uvoz.
 */
export function GtfsImport({ onImported, onCancel }: GtfsImportProps) {
  const [file, setFile] = useState<File | null>(null)
  const [rejection, setRejection] = useState<string | null>(null)
  const preview = usePreviewGtfs()
  const importFeed = useImportGtfs()
  const [skipExpired, setSkipExpired] = useState(true)
  const [confirmOpened, confirm] = useDisclosure(false)
  const status = useSetupStatus()
  // null: korisnik još nije mijenjao odabir, pa vrijede zadane varijante iz pregleda.
  const [chosen, setChosen] = useState<Set<string> | null>(null)
  const selection =
    chosen ?? new Set(preview.data?.variants.filter((v) => v.included).map((v) => v.key) ?? [])

  const report = importFeed.data ?? preview.data
  const existing = report?.existing ?? status.data?.counts
  // Varijante koje imaju samo istekle vožnje ne uvoze se ako se te vožnje izostavljaju.
  const variants = (report?.variants ?? []).filter(
    (v) => !skipExpired || v.tripCount > v.expiredTripCount,
  )
  const selectedVariants = variants.filter((v) => selection.has(v.key)).length
  // Nakon uspješnog uvoza svi su koraci završeni.
  const step = importFeed.data?.imported
    ? 3
    : importFeed.isPending || importFeed.data
      ? 2
      : file
        ? 1
        : 0

  const selectFile = (selected: File) => {
    setRejection(null)
    setFile(selected)
    setChosen(null)
    importFeed.reset()
    preview.mutate(selected)
  }

  const reset = () => {
    setFile(null)
    setChosen(null)
    preview.reset()
    importFeed.reset()
  }

  const confirmImport = () => {
    if (file) {
      importFeed.mutate(
        { file, variants: [...selection], skipExpired },
        { onSuccess: (result) => result.imported && onImported(result) },
      )
    }
  }

  const startImport = () => {
    if (existing && hasExistingData(existing)) {
      confirm.open()
    } else {
      confirmImport()
    }
  }

  return (
    <Stack gap="lg">
      <Stepper active={step} size="sm" allowNextStepsSelect={false}>
        <Stepper.Step label="Datoteka" description="GTFS ZIP" />
        <Stepper.Step label="Pregled" description="Provjera feeda" />
        <Stepper.Step label="Uvoz" description="Upis podataka" />
      </Stepper>

      {existing && hasExistingData(existing) && !report?.imported && <ReplaceWarning />}

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
              {report.canImport && !report.imported && report.expired.calendars > 0 && (
                <Switch
                  checked={skipExpired}
                  onChange={(event) => setSkipExpired(event.currentTarget.checked)}
                  label="Izostavi kalendare i vožnje koji više ne voze"
                  description={`${formatNumber(report.expired.calendars)} kalendara i ${formatNumber(report.expired.trips)} vožnji nemaju nijedan dan vožnje od danas nadalje.`}
                />
              )}
              <GtfsImportReport
                report={report}
                selectedVariants={report.imported ? undefined : selectedVariants}
                skipExpired={!report.imported && skipExpired}
              />
              {report.canImport && !report.imported && variants.length > 0 && (
                <VariantSelection variants={variants} selected={selection} onChange={setChosen} />
              )}
              {report.imported && <NextSteps />}
            </>
          )}

          {!importFeed.isPending && !report?.imported && (
            <Group justify="flex-end">
              <Button variant="default" onClick={reset} disabled={preview.isPending}>
                Druga datoteka
              </Button>
              <Button
                onClick={startImport}
                disabled={!preview.data?.canImport || importFeed.isError}
              >
                Uvezi
              </Button>
            </Group>
          )}
        </>
      )}

      {existing && (
        <ReplaceConfirmDialog
          opened={confirmOpened}
          existing={existing}
          onClose={confirm.close}
          onConfirm={confirmImport}
        />
      )}
    </Stack>
  )
}

/** Uvoz briše sve postojeće podatke; prije potvrde se još ništa ne mijenja. */
function ReplaceWarning() {
  return (
    <Alert
      color="red"
      variant="light"
      icon={<IconAlertTriangle />}
      title="Uvoz zamjenjuje sve postojeće podatke"
    >
      Trenutni vozni red, sezone, linije i stanice bit će obrisani i zamijenjeni novim feedom.
      Provjera feeda ne mijenja ništa; brisanje se događa tek nakon potvrde.
    </Alert>
  )
}

function NextSteps() {
  return (
    <Alert color="blue" variant="light" icon={<IconInfoCircle />} title="Sljedeći koraci">
      <Text size="sm">
        Vožnje možeš urediti u grafikonu na stranici linije. Kalendari iz feeda nisu u sezoni; vidiš
        ih na stranici{' '}
        <Anchor component={Link} to="/calendars" size="sm">
          Kalendari
        </Anchor>
        .
      </Text>
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
