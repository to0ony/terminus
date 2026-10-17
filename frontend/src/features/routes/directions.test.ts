import { describe, expect, it } from 'vitest'
import { directionOptions } from './directions'
import type { RoutePattern } from './queries'

function pattern(directionId: number | null, headsign: string | null, lastStop = 'Kraj') {
  return {
    directionId,
    headsign,
    shapeId: null,
    tripCount: 1,
    stops: [{ name: 'Početak' }, { name: lastStop }],
  } as RoutePattern
}

describe('directionOptions', () => {
  it('labels each direction by its main pattern', () => {
    const options = directionOptions([
      pattern(0, 'Borongaj'),
      pattern(0, 'Dubec'),
      pattern(1, null, 'Črnomerec'),
    ])

    expect(options).toEqual([
      { value: '0', label: '→ Borongaj' },
      { value: '1', label: '→ Črnomerec' },
    ])
  })

  it('offers no choice without two directions', () => {
    expect(directionOptions([pattern(null, 'A'), pattern(null, 'B')])).toEqual([])
    expect(directionOptions([pattern(0, 'A')])).toEqual([])
  })
})
