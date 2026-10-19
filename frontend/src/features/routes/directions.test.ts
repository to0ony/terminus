import { describe, expect, it } from 'vitest'
import { directionOptions } from './directions'
import type { RoutePattern } from './queries'

function pattern(
  directionId: number | null,
  headsign: string | null,
  lastStop = 'Kraj',
  tripCount = 1,
  stopCount = 2,
) {
  return {
    directionId,
    headsign,
    shapeId: null,
    tripCount,
    stops: [
      ...Array.from({ length: stopCount - 1 }, () => ({ name: 'Usput' })),
      { name: lastStop },
    ],
  } as RoutePattern
}

describe('directionOptions', () => {
  it('labels each direction by its target', () => {
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

  it('names a direction by the target most trips go to', () => {
    const options = directionOptions([
      pattern(0, null, 'Centar', 1),
      pattern(0, null, 'Potočari II', 2),
      pattern(0, null, 'Potočari II', 2),
      pattern(0, null, 'Grbavica škola', 3),
      pattern(1, null, 'M. Polje'),
    ])

    expect(options[0]).toEqual({ value: '0', label: '→ Potočari II' })
  })

  it('without trips names a direction by its longest sequence', () => {
    const options = directionOptions([
      pattern(0, null, 'Centar', 0, 5),
      pattern(0, null, 'Potočari II', 0, 9),
      pattern(1, null, 'M. Polje', 0),
    ])

    expect(options[0]).toEqual({ value: '0', label: '→ Potočari II' })
  })

  it('offers no choice without two directions', () => {
    expect(directionOptions([pattern(null, 'A'), pattern(null, 'B')])).toEqual([])
    expect(directionOptions([pattern(0, 'A')])).toEqual([])
  })
})
