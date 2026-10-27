import { describe, expect, it } from 'vitest'
import { type DataColumn, isColumnVisible, moveIndex, nextSort, selectRange } from './tableModel'

describe('nextSort', () => {
  it('cycles ascending, descending and back to the default order', () => {
    const asc = nextSort({ key: null, descending: false }, 'Name')
    expect(asc).toEqual({ key: 'Name', descending: false })
    const desc = nextSort(asc, 'Name')
    expect(desc).toEqual({ key: 'Name', descending: true })
    expect(nextSort(desc, 'Name')).toEqual({ key: null, descending: false })
  })

  it('starts ascending on another column', () => {
    expect(nextSort({ key: 'Name', descending: true }, 'Code')).toEqual({
      key: 'Code',
      descending: false,
    })
  })
})

describe('isColumnVisible', () => {
  const column = (props: Partial<DataColumn<unknown>>): DataColumn<unknown> => ({
    id: 'code',
    header: 'Kod',
    cell: () => null,
    ...props,
  })

  it('uses the default until the user chooses', () => {
    expect(isColumnVisible(column({}), {})).toBe(true)
    expect(isColumnVisible(column({ hiddenByDefault: true }), {})).toBe(false)
    expect(isColumnVisible(column({ hiddenByDefault: true }), { code: true })).toBe(true)
    expect(isColumnVisible(column({}), { code: false })).toBe(false)
  })

  it('never hides an always visible column', () => {
    expect(isColumnVisible(column({ alwaysVisible: true }), { code: false })).toBe(true)
  })
})

describe('selectRange', () => {
  const ids = ['a', 'b', 'c', 'd', 'e']

  it('selects every row between the anchor and the clicked row', () => {
    expect([...selectRange(ids, new Set(['a']), 0, 3, true)].sort()).toEqual(['a', 'b', 'c', 'd'])
  })

  it('works upwards and can clear a range', () => {
    const selected = new Set(ids)
    expect([...selectRange(ids, selected, 4, 2, false)].sort()).toEqual(['a', 'b'])
  })
})

describe('moveIndex', () => {
  it('starts at the first or last row and stops at the ends', () => {
    expect(moveIndex(null, 1, 3)).toBe(0)
    expect(moveIndex(null, -1, 3)).toBe(2)
    expect(moveIndex(2, 1, 3)).toBe(2)
    expect(moveIndex(0, -1, 3)).toBe(0)
    expect(moveIndex(null, 1, 0)).toBeNull()
  })
})
