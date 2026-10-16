import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { render } from '@/test/render'
import { BlankSetupForm } from './BlankSetupForm'

describe('BlankSetupForm', () => {
  it('requires a name and a web address', async () => {
    render(<BlankSetupForm onCreated={vi.fn()} onCancel={vi.fn()} />)

    await userEvent.click(screen.getByRole('button', { name: 'Stvori prijevoznika' }))

    expect(await screen.findByText('Upiši naziv prijevoznika.')).toBeInTheDocument()
    expect(screen.getByText('Upiši web adresu prijevoznika.')).toBeInTheDocument()
  })

  it('rejects addresses without http or https', async () => {
    render(<BlankSetupForm onCreated={vi.fn()} onCancel={vi.fn()} />)

    await userEvent.type(screen.getByRole('textbox', { name: /^Naziv/ }), 'GPP')
    await userEvent.type(screen.getByRole('textbox', { name: /^Web adresa\s*\*?$/ }), 'gpp.hr')
    await userEvent.type(screen.getByRole('textbox', { name: 'E-mail' }), 'info@')
    await userEvent.click(screen.getByRole('button', { name: 'Stvori prijevoznika' }))

    expect(await screen.findByText('Upiši ispravnu web adresu (https://…).')).toBeInTheDocument()
    expect(screen.getByText('Neispravan e-mail.')).toBeInTheDocument()
  })
})
