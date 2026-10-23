import { fireEvent, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { render } from '@/test/render'
import { DeleteRouteDialog } from './DeleteRouteDialog'
import type { Route } from './queries'

const route: Route = {
  id: 'r1',
  agencyId: 'a1',
  code: 'L6',
  shortName: '6',
  longName: 'Črnomerec – Sopot',
  description: null,
  routeType: 3,
  url: null,
  color: null,
  textColor: null,
  sortOrder: null,
  continuousPickup: null,
  continuousDropOff: null,
  networkId: null,
  timeMode: 'Exact',
  isActive: true,
  createdAt: '2026-10-18T00:00:00Z',
  updatedAt: '2026-10-18T00:00:00Z',
}

describe('DeleteRouteDialog', () => {
  it('names what goes with the route and waits for its label', async () => {
    render(
      <DeleteRouteDialog route={route} opened onClose={vi.fn()} variantCount={4} tripCount={312} />,
    )

    expect(await screen.findByText(/4 varijante/)).toBeInTheDocument()
    expect(screen.getByText(/312 vožnji/)).toBeInTheDocument()
    const button = screen.getByRole('button', { name: 'Obriši liniju' })
    expect(button).toBeDisabled()

    fireEvent.change(screen.getByLabelText(/upiši oznaku linije/), { target: { value: '6' } })
    expect(button).toBeEnabled()
  })

  it('deletes a route without trips after one click', async () => {
    render(
      <DeleteRouteDialog route={route} opened onClose={vi.fn()} variantCount={1} tripCount={0} />,
    )

    expect(await screen.findByText(/1 varijantom/)).toBeInTheDocument()
    expect(screen.queryByLabelText(/upiši oznaku linije/)).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Obriši liniju' })).toBeEnabled()
  })
})
