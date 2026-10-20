import { Select } from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { IconSearch } from '@tabler/icons-react'
import { useState } from 'react'
import { type Stop, useStops } from '@/features/stops/queries'

interface StopSearchProps {
  onPick: (stop: Stop) => void
  onClose: () => void
}

/** Traženje stajališta po nazivu ili kodu; odabrano se predaje i polje se prazni. */
export function StopSearch({ onPick, onClose }: StopSearchProps) {
  const [value, setValue] = useState('')
  const [search] = useDebouncedValue(value.trim(), 250)
  // Varijanta obilazi stajališta i perone, ne kolodvore.
  const stops = useStops({ LocationType: 0, Search: search || undefined, PageSize: 20 })
  const items = search ? (stops.data?.items ?? []) : []

  return (
    <Select
      size="xs"
      autoFocus
      searchable
      placeholder="Traži stanicu po nazivu ili kodu"
      leftSection={<IconSearch size={14} />}
      data={items.map((s) => ({
        value: s.id,
        label: `${s.name}${s.platformCode ? ` · ${s.platformCode}` : ''} (${s.code})`,
      }))}
      // Rezultate filtrira backend.
      filter={({ options }) => options}
      value={null}
      searchValue={value}
      onSearchChange={setValue}
      onChange={(id) => {
        const stop = items.find((s) => s.id === id)
        if (stop) {
          onPick(stop)
          setValue('')
        }
      }}
      onKeyDown={(event) => event.key === 'Escape' && onClose()}
      nothingFoundMessage={
        !search ? 'Upiši naziv ili kod' : stops.isFetching ? 'Tražim…' : 'Nema stanica'
      }
      comboboxProps={{ withinPortal: true }}
    />
  )
}
