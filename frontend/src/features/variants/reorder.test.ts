import { describe, expect, it } from 'vitest'
import { moveBefore } from './reorder'

describe('moveBefore', () => {
  const list = ['a', 'b', 'c', 'd']

  it('moves an item down, in front of a later one', () => {
    expect(moveBefore(list, 0, 3)).toEqual(['b', 'c', 'a', 'd'])
  })

  it('moves an item up, in front of an earlier one', () => {
    expect(moveBefore(list, 3, 1)).toEqual(['a', 'd', 'b', 'c'])
  })

  it('moves an item to the end', () => {
    expect(moveBefore(list, 1, 4)).toEqual(['a', 'c', 'd', 'b'])
  })

  it('leaves the list when dropped on itself', () => {
    expect(moveBefore(list, 2, 2)).toBe(list)
    expect(moveBefore(list, 2, 3)).toBe(list)
  })
})
