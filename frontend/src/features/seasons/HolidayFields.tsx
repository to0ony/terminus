import { Anchor, Select, SimpleGrid, Text } from '@mantine/core'
import type { HolidayRule } from './holidayRule'
import { useHolidayCountries } from './queries'

const regionNames = new Intl.DisplayNames(['hr'], { type: 'region' })

interface HolidayFieldsProps {
  country: string | null
  onCountryChange: (country: string | null) => void
  rule: HolidayRule
  onRuleChange: (rule: HolidayRule) => void
  /** Tipovi dana s ključem (Id ili indeks) i nazivom. */
  dayTypes: { key: string; name: string }[]
  countryError?: string
}

/** Država praznika (OpenHolidays) i kako se vozi na praznike. */
export function HolidayFields({
  country,
  onCountryChange,
  rule,
  onRuleChange,
  dayTypes,
  countryError,
}: HolidayFieldsProps) {
  const countries = useHolidayCountries()

  const countryOptions = [
    { value: '', label: 'Bez automatskih praznika' },
    ...(countries.data?.countries ?? [])
      .map((c) => ({ value: c.code, label: regionNames.of(c.code) ?? c.name }))
      .sort((a, b) => a.label.localeCompare(b.label, 'hr')),
  ]
  // Država sezone mora biti u popisu i dok se popis učitava ili ako API ne radi.
  if (country && !countryOptions.some((c) => c.value === country)) {
    countryOptions.push({ value: country, label: regionNames.of(country) ?? country })
  }

  return (
    <div>
      <SimpleGrid cols={{ base: 1, xs: 2 }}>
        <Select
          label="Praznici"
          description={
            countries.isError
              ? 'Popis država trenutno nije dostupan.'
              : 'Državni praznici se dodaju sami.'
          }
          data={countryOptions}
          value={country ?? ''}
          onChange={(value) => onCountryChange(value || null)}
          searchable
          allowDeselect={false}
          error={countryError}
        />
        <Select
          label="Na praznike vozi"
          description="Pojedini praznik možeš promijeniti u kalendaru."
          data={[
            ...dayTypes.map((t) => ({ value: `type:${t.key}`, label: `kao ${t.name}` })),
            { value: 'NoService', label: 'ne vozi' },
            { value: 'Regular', label: 'normalno (kao taj dan u tjednu)' },
          ]}
          value={rule}
          onChange={(value) => value && onRuleChange(value as HolidayRule)}
          allowDeselect={false}
          disabled={!country}
        />
      </SimpleGrid>
      <Text size="xs" c="dimmed" mt={6}>
        Podaci o praznicima:{' '}
        <Anchor href="https://openholidaysapi.org" target="_blank" size="xs">
          OpenHolidays API
        </Anchor>{' '}
        (ODbL)
      </Text>
    </div>
  )
}
