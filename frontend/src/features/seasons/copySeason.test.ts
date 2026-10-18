import { describe, expect, it } from 'vitest'
import { copyName, nextYear } from './copySeason'

describe('nextYear', () => {
  it('keeps the day and month', () => {
    expect(nextYear('2026-09-01')).toBe('2027-09-01')
    expect(nextYear('2027-06-14')).toBe('2028-06-14')
  })

  it('moves 29 February to 1 March', () => {
    expect(nextYear('2028-02-29')).toBe('2029-03-01')
  })
})

describe('copyName', () => {
  it('moves the years in the name', () => {
    expect(copyName('Zimski 2026/27')).toBe('Zimski 2027/28')
    expect(copyName('Ljeto 2027')).toBe('Ljeto 2028')
    expect(copyName('Zima 1999/00')).toBe('Zima 2000/01')
  })

  it('marks a name without years as a copy', () => {
    expect(copyName('Školska godina')).toBe('Školska godina (kopija)')
  })
})
