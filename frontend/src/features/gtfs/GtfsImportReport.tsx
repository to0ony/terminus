import { Accordion, Alert, Code, List, Paper, SimpleGrid, Stack, Text } from '@mantine/core'
import { IconAlertCircle, IconAlertTriangle, IconCircleCheck } from '@tabler/icons-react'
import { formatNumber } from '@/lib/format'
import type { GtfsImportIssue, GtfsImportReport as Report } from './queries'

/**
 * Izvještaj pregleda ili uvoza: osnova koja se uvozi (novo i ažurirano),
 * greške, upozorenja i preskočene datoteke.
 */
export function GtfsImportReport({
  report,
  selectedVariants,
}: {
  report: Report
  /** Broj varijanti koje je korisnik odabrao (u pregledu); bez njega zadani izbor. */
  selectedVariants?: number
}) {
  const { counts, changes, errors, warnings, skippedFiles } = report

  const stats = [
    {
      label: 'Prijevoznici',
      value: counts.agencies,
      added: changes.newAgencies,
      updated: changes.updatedAgencies,
    },
    {
      label: 'Stanice',
      value: counts.stops,
      added: changes.newStops,
      updated: changes.updatedStops,
    },
    {
      label: 'Linije',
      value: counts.routes,
      added: changes.newRoutes,
      updated: changes.updatedRoutes,
    },
    {
      label: 'Trase',
      value: counts.shapes,
      added: changes.newShapes,
      updated: changes.updatedShapes,
    },
    {
      label: 'Varijante linija',
      value: selectedVariants ?? report.variants.filter((v) => v.included).length,
      added: null,
      updated: null,
    },
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
        <>
          <SimpleGrid cols={{ base: 2, xs: 3, sm: 5 }} spacing="sm">
            {stats.map((stat) => (
              <Paper key={stat.label} withBorder p="sm">
                <Text size="xs" c="dimmed">
                  {stat.label}
                </Text>
                <Text fw={700} size="lg">
                  {formatNumber(stat.value)}
                </Text>
                {stat.added !== null && stat.updated !== null && stat.updated > 0 && (
                  <Text size="xs" c="dimmed">
                    {formatNumber(stat.added)} novih · {formatNumber(stat.updated)} ažurira se
                  </Text>
                )}
              </Paper>
            ))}
          </SimpleGrid>
          <Text size="sm" c="dimmed">
            Kalendari i vožnje se ne uvoze, nego ih radiš u aplikaciji.
            {counts.trips > 0 &&
              ` ${formatNumber(counts.trips)} vožnji iz feeda služi samo za prepoznavanje varijanti i vremena vožnje.`}
          </Text>
        </>
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
    return (
      <Alert color="green" variant="light" icon={<IconCircleCheck />} title="Osnova je uvezena" />
    )
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
