import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { render } from '@/test/render'
import { authKeys } from './keys'
import { LoginPage } from './LoginPage'

describe('LoginPage', () => {
  it('validates the form before sending it', async () => {
    render(<LoginPage />, { route: '/login', queryData: [[authKeys.me, null]] })

    await userEvent.click(screen.getByRole('button', { name: 'Prijavi se' }))

    expect(await screen.findByText('Upiši e-mail.')).toBeInTheDocument()
    expect(screen.getByText('Upiši lozinku.')).toBeInTheDocument()
  })

  it('rejects an invalid e-mail', async () => {
    render(<LoginPage />, { route: '/login', queryData: [[authKeys.me, null]] })

    await userEvent.type(screen.getByLabelText('E-mail'), 'admin')
    await userEvent.type(screen.getByLabelText('Lozinka'), 'lozinka1234')
    await userEvent.click(screen.getByRole('button', { name: 'Prijavi se' }))

    expect(await screen.findByText('Neispravan e-mail.')).toBeInTheDocument()
  })
})
