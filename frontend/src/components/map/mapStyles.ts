import { useLocalStorage } from '@mantine/hooks'

/** Podloge karte s OpenFreeMapa: vektorske karte iz OSM-a, bez ključa i bez ograničenja prometa. */
export const mapStyles = [
  { value: 'auto', label: 'Prema temi', description: 'Svijetla ili tamna, kao sučelje' },
  { value: 'positron', label: 'Svijetla', description: 'Positron, mirna siva' },
  { value: 'dark', label: 'Tamna', description: 'Dark, za tamnu temu' },
  { value: 'liberty', label: 'Detaljna', description: 'Liberty, zgrade i oznake' },
  { value: 'bright', label: 'Šarena', description: 'Bright, klasična karta' },
  { value: 'fiord', label: 'Tamnoplava', description: 'Fiord, tamna plava' },
] as const

export type MapStyle = (typeof mapStyles)[number]['value']

const isMapStyle = (value: unknown): value is MapStyle =>
  mapStyles.some((style) => style.value === value)

/** Adresa stila; "Prema temi" bira svijetlu ili tamnu podlogu. */
export function mapStyleUrl(style: MapStyle, colorScheme: 'light' | 'dark'): string {
  const name = style === 'auto' ? (colorScheme === 'dark' ? 'dark' : 'positron') : style
  return `https://tiles.openfreemap.org/styles/${name}`
}

/** Odabrana podloga, zapamćena u pregledniku i ista na svim kartama. */
export function useMapStyle() {
  return useLocalStorage<MapStyle>({
    key: 'terminus-map-style',
    defaultValue: 'auto',
    deserialize: (value) => {
      try {
        const parsed: unknown = value === undefined ? 'auto' : JSON.parse(value)
        return isMapStyle(parsed) ? parsed : 'auto'
      } catch {
        return 'auto'
      }
    },
  })
}
