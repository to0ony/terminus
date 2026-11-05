import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { render } from '@/test/render'
import { GtfsImportReport } from './GtfsImportReport'
import type { GtfsImportReport as Report } from './queries'

const counts: Report['counts'] = {
  agencies: 1,
  stops: 3800,
  routes: 154,
  calendars: 10,
  calendarDates: 72,
  trips: 98915,
  stopTimes: 1697138,
  shapes: 561,
  shapePoints: 82147,
  fares: 0,
  fareRules: 0,
  feedInfo: true,
  tripsFromFrequencies: 0,
  interpolatedStopTimes: 12,
}

const report = (overrides: Partial<Report> = {}): Report => ({
  imported: false,
  canImport: true,
  counts,
  existing: { agencies: 0, stops: 0, routes: 0, trips: 0, seasons: 0 },
  expired: { calendars: 5, trips: 47892, stopTimes: 824980 },
  variants: [
    {
      key: 'a',
      routeCode: '6',
      routeName: '6',
      directionId: 0,
      name: 'Črnomerec → Sopot',
      headsign: 'Sopot',
      stopCount: 23,
      tripCount: 584,
      expiredTripCount: 100,
      duration: 2340,
      share: 0.66,
      main: true,
      included: true,
    },
  ],
  errors: [],
  warnings: [],
  skippedFiles: [],
  ...overrides,
})

describe('GtfsImportReport', () => {
  it('shows what will be imported', () => {
    render(<GtfsImportReport report={report()} />)

    expect(screen.getByText('Feed se može uvesti')).toBeInTheDocument()
    expect(screen.getByText('3.800')).toBeInTheDocument()
    expect(screen.getByText('98.915')).toBeInTheDocument()
    expect(screen.getByText('10')).toBeInTheDocument()
  })

  it('leaves expired trips out of the numbers when they are skipped', () => {
    render(<GtfsImportReport report={report()} skipExpired />)

    expect(screen.getByText('51.023')).toBeInTheDocument()
    expect(screen.getByText('5')).toBeInTheDocument()
  })

  it('lists errors with counts and examples and hides the counts', () => {
    render(
      <GtfsImportReport
        report={report({
          canImport: false,
          errors: [
            {
              file: 'stops.txt',
              message: 'Dvostruki stop_id.',
              count: 2,
              examples: ["redak 3: stop_id 'A'"],
            },
          ],
        })}
      />,
    )

    expect(screen.getByText(/feed se ne može uvesti/)).toBeInTheDocument()
    expect(screen.getByText('stops.txt')).toBeInTheDocument()
    expect(screen.getByText('(2×)')).toBeInTheDocument()
    expect(screen.getByText("npr. redak 3: stop_id 'A'")).toBeInTheDocument()
    expect(screen.queryByText('Stanice')).not.toBeInTheDocument()
  })

  it('collapses warnings and skipped files', () => {
    render(
      <GtfsImportReport
        report={report({
          warnings: [{ file: 'shapes.txt', message: 'Trasa preskočena.', count: 9, examples: [] }],
          skippedFiles: [{ file: 'transfers.txt', reason: 'Presjedanja nisu podržana.' }],
        })}
      />,
    )

    expect(screen.getByText('Upozorenja (1)')).toBeInTheDocument()
    expect(screen.getByText('Datoteke koje se ne uvoze (1)')).toBeInTheDocument()
  })
})
