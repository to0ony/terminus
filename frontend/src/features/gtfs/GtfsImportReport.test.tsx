import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { render } from '@/test/render'
import { GtfsImportReport } from './GtfsImportReport'
import type { GtfsImportReport as Report } from './queries'

const counts: Report['counts'] = {
  agencies: 1,
  stops: 3800,
  routes: 154,
  calendars: 7,
  calendarDates: 95,
  trips: 66661,
  stopTimes: 1131537,
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
  existing: { agencies: 0, stops: 0, routes: 0, trips: 0 },
  changes: {
    newAgencies: 1,
    updatedAgencies: 0,
    newStops: 3700,
    updatedStops: 100,
    newRoutes: 154,
    updatedRoutes: 0,
    newShapes: 561,
    updatedShapes: 0,
  },
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
    expect(screen.getByText('3.700 novih · 100 ažurira se')).toBeInTheDocument()
    expect(
      screen.getByText(/66.661 vožnji iz feeda služi samo za prepoznavanje/),
    ).toBeInTheDocument()
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
