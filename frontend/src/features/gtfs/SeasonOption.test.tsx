import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { render } from '@/test/render'
import { SeasonOption } from './SeasonOption'

const season = {
  problem: null,
  name: 'Vozni red 5. 11. 2026. – 31. 12. 2026.',
  startDate: '2026-11-05',
  endDate: '2026-12-31',
  dayTypes: ['Radni dan', 'Subota', 'Nedjelja'],
  exceptions: 6,
}

describe('SeasonOption', () => {
  it('describes the season that would be created', () => {
    render(<SeasonOption season={season} checked onChange={vi.fn()} />)

    expect(screen.getByRole('switch')).toBeChecked()
    expect(screen.getByText(/Tipova dana: 3, dana koji odstupaju: 6/)).toBeInTheDocument()
  })

  it('is disabled and explains why when a season is impossible', () => {
    render(
      <SeasonOption
        season={{
          ...season,
          problem: '1. 12. 2026. vozi više kalendara odjednom.',
          name: null,
          dayTypes: [],
        }}
        checked
        onChange={vi.fn()}
      />,
    )

    expect(screen.getByRole('switch')).toBeDisabled()
    expect(screen.getByRole('switch')).not.toBeChecked()
    expect(screen.getByText(/vozi više kalendara odjednom/)).toBeInTheDocument()
  })
})
