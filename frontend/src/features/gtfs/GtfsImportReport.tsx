import {
  Accordion,
  Alert,
  Badge,
  Code,
  Group,
  List,
  Paper,
  SimpleGrid,
  Stack,
  Text,
} from '@mantine/core'
import { IconAlertCircle, IconAlertTriangle, IconCircleCheck } from '@tabler/icons-react'
import { formatNumber } from '@/lib/format'
import type { GtfsImportIssue, GtfsImportReport as Report } from './queries'

/** Izvještaj pregleda ili uvoza: što se uvozi, greške, upozorenja i preskočene datoteke. */
export function GtfsImportReport({ report }: { report: Report }) {
  const { counts, errors, warnings, skippedFiles } = report

  const stats = [
    { label: 'Prijevoznici', value: counts.agencies },
    { label: 'Stanice', value: counts.stops },
    { label: 'Linije', value: counts.routes },
    { label: 'Kalendari', value: counts.calendars },
    { label: 'Vožnje', value: counts.trips },
    { label: 'Vremena polazaka', value: counts.stopTimes },
    { label: 'Trase', value: counts.shapes },
    { label: 'Cijene', value: counts.fares },
  ]

  return (
    <Stack gap="md">
      <Summary report={report} />

      {errors.length > 0 && (
        <Alert
          color="red"
          variant="light"
          icon={<IconAlertCircle />}
          title={`Greške (${errors.length}): feed se ne može uvesti`}
        >
          <IssueList issues={errors} />
        </Alert>
      )}

      {report.canImport && (
        <SimpleGrid cols={{ base: 2, xs: 4 }} spacing="sm">
          {stats.map((stat) => (
            <Paper key={stat.label} withBorder p="sm">
              <Text size="xs" c="dimmed">
                {stat.label}
              </Text>
              <Text fw={700} size="lg">
                {formatNumber(stat.value)}
              </Text>
            </Paper>
          ))}
        </SimpleGrid>
      )}

      {(counts.tripsFromFrequencies > 0 || counts.interpolatedStopTimes > 0) && (
        <Group gap="xs">
          {counts.tripsFromFrequencies > 0 && (
            <Badge variant="light">
              {formatNumber(counts.tripsFromFrequencies)} vožnji iz frequencies.txt
            </Badge>
          )}
          {counts.interpolatedStopTimes > 0 && (
            <Badge variant="light">
              {formatNumber(counts.interpolatedStopTimes)} izračunatih vremena
            </Badge>
          )}
        </Group>
      )}

      {(warnings.length > 0 || skippedFiles.length > 0) && (
        <Accordion variant="separated" multiple>
          {warnings.length > 0 && (
            <Accordion.Item value="warnings">
              <Accordion.Control icon={<IconAlertTriangle size={18} color="orange" />}>
                Upozorenja ({warnings.length})
              </Accordion.Control>
              <Accordion.Panel>
                <Text size="sm" c="dimmed" mb="xs">
                  Ti zapisi su preskočeni ili ispravljeni; ostatak feeda se može uvesti.
                </Text>
                <IssueList issues={warnings} />
              </Accordion.Panel>
            </Accordion.Item>
          )}
          {skippedFiles.length > 0 && (
            <Accordion.Item value="skipped">
              <Accordion.Control>
                Datoteke koje se ne uvoze ({skippedFiles.length})
              </Accordion.Control>
              <Accordion.Panel>
                <List size="sm" spacing={4}>
                  {skippedFiles.map((file) => (
                    <List.Item key={file.file}>
                      <Code>{file.file}</Code> {file.reason}
                    </List.Item>
                  ))}
                </List>
              </Accordion.Panel>
            </Accordion.Item>
          )}
        </Accordion>
      )}
    </Stack>
  )
}

function Summary({ report }: { report: Report }) {
  if (report.imported) {
    return <Alert color="green" variant="light" icon={<IconCircleCheck />} title="Feed je uvezen" />
  }
  if (!report.canImport) {
    return null
  }
  return (
    <Alert
      color={report.warnings.length > 0 ? 'yellow' : 'green'}
      variant="light"
      icon={<IconCircleCheck />}
      title="Feed se može uvesti"
    >
      {report.warnings.length > 0
        ? 'Neki zapisi će biti preskočeni ili ispravljeni; pogledaj upozorenja.'
        : 'Provjera nije našla problema.'}
    </Alert>
  )
}

function IssueList({ issues }: { issues: GtfsImportIssue[] }) {
  return (
    <Stack gap="xs">
      {issues.map((issue, index) => (
        <div key={index}>
          <Text size="sm">
            {issue.file && <Code mr={6}>{issue.file}</Code>}
            {issue.message}
            {issue.count > 1 && (
              <Text span c="dimmed" size="sm">
                {' '}
                ({formatNumber(issue.count)}×)
              </Text>
            )}
          </Text>
          {issue.examples.length > 0 && (
            <Text size="xs" c="dimmed">
              npr. {issue.examples.join('; ')}
            </Text>
          )}
        </div>
      ))}
    </Stack>
  )
}
