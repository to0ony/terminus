import { Switch } from '@mantine/core'
import { formatNumber } from '@/lib/format'
import type { GtfsImportReport } from './queries'

interface SeasonOptionProps {
  season: NonNullable<GtfsImportReport['season']>
  checked: boolean
  onChange: (checked: boolean) => void
}

/** Pretvaranje kalendara feeda u sezonu; onemogućeno ako ih sezona ne može prikazati. */
export function SeasonOption({ season, checked, onChange }: SeasonOptionProps) {
  return (
    <Switch
      checked={checked && !season.problem}
      disabled={season.problem !== null}
      onChange={(event) => onChange(event.currentTarget.checked)}
      label="Napravi sezonu voznog reda"
      description={
        season.problem
          ? `Nije moguće, pa kalendari ostaju izvan sezona: ${season.problem}`
          : `${season.name}. Tipova dana: ${formatNumber(season.dayTypes.length)}, dana koji odstupaju: ${formatNumber(season.exceptions)}.`
      }
    />
  )
}
