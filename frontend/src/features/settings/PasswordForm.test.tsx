import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { render } from '@/test/render'
import { PasswordForm } from './PasswordForm'

describe('PasswordForm', () => {
  it('checks the length and that both new passwords match', async () => {
    render(<PasswordForm />)

    await userEvent.type(screen.getByLabelText('Trenutna lozinka'), 'stara-lozinka')
    await userEvent.type(screen.getByLabelText('Nova lozinka'), 'kratka')
    await userEvent.type(screen.getByLabelText('Ponovi novu lozinku'), 'druga')
    await userEvent.click(screen.getByRole('button', { name: 'Promijeni lozinku' }))

    expect(await screen.findByText('Nova lozinka mora imati barem 10 znakova.')).toBeInTheDocument()
    expect(screen.getByText('Lozinke se ne podudaraju.')).toBeInTheDocument()
  })
})
