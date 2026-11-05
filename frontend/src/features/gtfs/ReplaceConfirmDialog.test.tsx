import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { render } from '@/test/render'
import { hasExistingData } from './queries'
import { ReplaceConfirmDialog } from './ReplaceConfirmDialog'

const existing = { agencies: 1, stops: 3800, routes: 154, trips: 12400, seasons: 2 }

describe('ReplaceConfirmDialog', () => {
  it('lists what will be deleted', () => {
    render(
      <ReplaceConfirmDialog opened existing={existing} onClose={vi.fn()} onConfirm={vi.fn()} />,
    )

    expect(screen.getByText('Sezone: 2')).toBeInTheDocument()
    expect(screen.getByText('Vožnje: 12.400')).toBeInTheDocument()
  })

  it('confirms only after the user acknowledges the loss', async () => {
    const onConfirm = vi.fn()
    render(
      <ReplaceConfirmDialog opened existing={existing} onClose={vi.fn()} onConfirm={onConfirm} />,
    )
    const replace = screen.getByRole('button', { name: 'Zamijeni podatke' })

    expect(replace).toBeDisabled()

    await userEvent.click(screen.getByRole('checkbox'))
    await userEvent.click(replace)

    expect(onConfirm).toHaveBeenCalledOnce()
  })
})

describe('hasExistingData', () => {
  it('is false for an empty instance', () => {
    expect(hasExistingData({ agencies: 0, stops: 0, routes: 0, trips: 0, seasons: 0 })).toBe(false)
  })

  it('counts a season without trips as data', () => {
    expect(hasExistingData({ agencies: 0, stops: 0, routes: 0, trips: 0, seasons: 1 })).toBe(true)
  })
})
