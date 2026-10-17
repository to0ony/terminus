import { describe, expect, it } from 'vitest'
import { contrastRatio, toHex } from './color'

describe('toHex', () => {
  it('accepts hex with or without #, in any case', () => {
    expect(toHex('#1565c0')).toBe('1565C0')
    expect(toHex(' FFFFFF ')).toBe('FFFFFF')
  })

  it('rejects anything else', () => {
    expect(toHex('')).toBeNull()
    expect(toHex('#fff')).toBeNull()
    expect(toHex('blue')).toBeNull()
  })
})

describe('contrastRatio', () => {
  it('follows WCAG 2', () => {
    expect(contrastRatio('000000', 'FFFFFF')).toBeCloseTo(21)
    expect(contrastRatio('FFFFFF', 'FFFFFF')).toBeCloseTo(1)
    // Plava 1565C0 s bijelim tekstom prolazi 4,5.
    expect(contrastRatio('1565C0', 'FFFFFF')).toBeGreaterThan(4.5)
    expect(contrastRatio('FFEB3B', 'FFFFFF')).toBeLessThan(4.5)
  })
})
