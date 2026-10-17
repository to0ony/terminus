/** Pravilo za praznike: "type:<ključ tipa dana>", "NoService" ili "Regular". */
export type HolidayRule = `type:${string}` | 'NoService' | 'Regular'

/** Pravilo u obliku koji traži API. */
export function holidayRuleToApi(rule: HolidayRule): {
  holidayMode: 'Regular' | 'RunsAs' | 'NoService'
  dayTypeKey: string | null
} {
  if (rule.startsWith('type:')) {
    return { holidayMode: 'RunsAs', dayTypeKey: rule.slice(5) }
  }
  return { holidayMode: rule as 'Regular' | 'NoService', dayTypeKey: null }
}
