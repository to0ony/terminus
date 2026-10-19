import { describe, expect, it } from 'vitest'
import { directionGroups, type Variant } from './queries'

const variant = (id: string, directionId: number | null, sortOrder: number) =>
  ({ id, directionId, sortOrder }) as Variant

const ids = (groups: ReturnType<typeof directionGroups>) =>
  groups.map((g) => [g.direction, g.variants.map((v) => v.id)])

describe('directionGroups', () => {
  it('groups by direction in sort order', () => {
    const groups = directionGroups([variant('b', 0, 1), variant('r', 1, 0), variant('a', 0, 0)])
    expect(ids(groups)).toEqual([
      [0, ['a', 'b']],
      [1, ['r']],
    ])
  })

  it('always shows both directions', () => {
    expect(ids(directionGroups([variant('a', 0, 0)]))).toEqual([
      [0, ['a']],
      [1, []],
    ])
  })

  it('puts variants without a direction last', () => {
    expect(ids(directionGroups([variant('x', null, 0), variant('a', 0, 0)]))).toEqual([
      [0, ['a']],
      [1, []],
      [null, ['x']],
    ])
  })
})
