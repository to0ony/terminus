import { useLocalStorage } from '@mantine/hooks'
import { IconBaselineDensityMedium, IconBaselineDensitySmall } from '@tabler/icons-react'

export type Density = 'comfortable' | 'compact'

/** Gustoća prikaza tablica; izbor se pamti u pregledniku. */
export const densities: { value: Density; label: string; icon: typeof IconBaselineDensitySmall }[] =
  [
    { value: 'comfortable', label: 'Udobno', icon: IconBaselineDensityMedium },
    { value: 'compact', label: 'Zbijeno', icon: IconBaselineDensitySmall },
  ]

const isDensity = (value: unknown): value is Density => densities.some((d) => d.value === value)

export function useDensity() {
  return useLocalStorage<Density>({
    key: 'terminus-density',
    defaultValue: 'comfortable',
    // Tema se gradi iz gustoće, pa se čita odmah, bez treptaja pri učitavanju.
    getInitialValueInEffect: false,
    deserialize: (value) => {
      try {
        const parsed: unknown = value === undefined ? 'comfortable' : JSON.parse(value)
        return isDensity(parsed) ? parsed : 'comfortable'
      } catch {
        return 'comfortable'
      }
    },
  })
}
