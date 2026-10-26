import { IconBus, IconFerry, IconShip, IconTrain } from '@tabler/icons-react'
import { describe, expect, it } from 'vitest'
import { routeTypeIcon, routeTypeLabel } from './routeTypes'

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

describe('routeTypeIcon', () => {
  it('picks an icon by basic and extended GTFS route types', () => {
    expect(routeTypeIcon(3)).toBe(IconBus)
    expect(routeTypeIcon(2)).toBe(IconTrain)
    expect(routeTypeIcon(4)).toBe(IconShip)
    expect(routeTypeIcon(1200)).toBe(IconFerry)
    expect(routeTypeIcon(109)).toBe(IconTrain)
  })

  it('falls back to a bus', () => {
    expect(routeTypeIcon(1600)).toBe(IconBus)
  })
})
