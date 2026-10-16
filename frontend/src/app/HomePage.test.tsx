import { screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { render } from '@/test/render'
import { HomePage } from './HomePage'

describe('HomePage', () => {
  it('shows the logo', () => {
    render(<HomePage />)

    expect(screen.getByAltText('Terminus')).toBeInTheDocument()
  })
})
