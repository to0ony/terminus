import { describe, expect, it } from 'vitest'
import { nextStopCode, suggestStopCode } from './stopCode'

describe('suggestStopCode', () => {
  it('turns name and platform into a slug without diacritics', () => {
    expect(suggestStopCode('Trg bana J. Jelačića', 'A')).toBe('trg-bana-j-jelacica-a')
    expect(suggestStopCode('Đure Đakovića')).toBe('dure-dakovica')
    expect(suggestStopCode('  Šestinski dol  ', null)).toBe('sestinski-dol')
  })
})

describe('nextStopCode', () => {
  it('adds a counter after the first attempt', () => {
    expect(nextStopCode('glavni', 0)).toBe('glavni')
    expect(nextStopCode('glavni', 1)).toBe('glavni-2')
  })
})
