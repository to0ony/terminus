import { describe, expect, it } from 'vitest'
import { distance, duration, suggestTravelTime } from './variantMath'

describe('duration', () => {
  it('adds running times and inner dwells', () => {
    const stops = [
      { travelTime: 0, dwellTime: 60 },
      { travelTime: 120, dwellTime: 30 },
      { travelTime: 180, dwellTime: 60 },
    ]
    expect(duration({ stops })).toBe(330)
  })
})

describe('distance', () => {
  it('measures about 1.1 km per 0.01° of latitude', () => {
    expect(distance([45.8, 15.97], [45.81, 15.97])).toBeCloseTo(1112, -1)
  })
})

describe('suggestTravelTime', () => {
  it('rounds to half a minute and never goes below a minute', () => {
    // 1 km autobusom: 1,3 km pri 20 km/h je 234 s, zaokruženo 240 s.
    expect(suggestTravelTime(1000, 3)).toBe(240)
    expect(suggestTravelTime(50, 3)).toBe(60)
    // Tramvaj je sporiji: 1,3 km pri 16 km/h je 292,5 s.
    expect(suggestTravelTime(1000, 0)).toBe(300)
  })
})
