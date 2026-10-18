import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { render } from '@/test/render'
import { DeleteAgencyDialog } from './AgencyDialogs'
import type { Agency } from './queries'

const agency = (id: string, name: string): Agency => ({
  id,
  code: id,
  name,
  url: 'https://example.com',
  timezone: 'Europe/Zagreb',
  lang: 'hr',
  phone: null,
  fareUrl: null,
  email: null,
  createdAt: '2026-10-18T00:00:00Z',
  updatedAt: '2026-10-18T00:00:00Z',
})

const leaving = agency('a', 'Autoprijevoz A')
const agencies = [leaving, agency('b', 'Autoprijevoz B'), agency('c', 'Autoprijevoz C')]

describe('DeleteAgencyDialog', () => {
  it('asks who takes over the routes before deleting', async () => {
    render(
      <DeleteAgencyDialog agency={leaving} routeCount={4} agencies={agencies} onClose={vi.fn()} />,
    )

    expect(await screen.findByText(/vozi 4 linija/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Obriši' })).toBeDisabled()
  })

  it('offers the only other agency to take over the routes', async () => {
    render(
      <DeleteAgencyDialog
        agency={leaving}
        routeCount={1}
        agencies={agencies.slice(0, 2)}
        onClose={vi.fn()}
      />,
    )

    expect(await screen.findByText(/vozi 1 liniju/)).toBeInTheDocument()
    expect(screen.getByDisplayValue('Autoprijevoz B')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Obriši' })).toBeEnabled()
  })

  it('deletes an agency without routes right away', async () => {
    render(
      <DeleteAgencyDialog agency={leaving} routeCount={0} agencies={agencies} onClose={vi.fn()} />,
    )

    expect(await screen.findByText(/Nema nijednu liniju/)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Obriši' })).toBeEnabled()
  })
})
