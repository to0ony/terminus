import { describe, expect, it } from 'vitest'
import { routeTypeLabel } from './routeTypes'

describe('routeTypeLabel', () => {
  it('names basic and extended GTFS route types', () => {
    expect(routeTypeLabel(3)).toBe('Autobus')
    expect(routeTypeLabel(0)).toBe('Tramvaj')
    expect(routeTypeLabel(704)).toBe('Autobus')
    expect(routeTypeLabel(900)).toBe('Tramvaj')
  })

  it('falls back to the number for unknown types', () => {
    expect(routeTypeLabel(1600)).toBe('Vrsta 1600')
  })
})
