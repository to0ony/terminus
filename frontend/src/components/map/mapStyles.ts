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

/** Gdje se pamti odabrana podloga i koja je zadana. */
export interface MapStyleSetting {
  key: string
  defaultValue: MapStyle
}

/** Zajednička podloga svih karata dashboarda. */
export const sharedMapStyle: MapStyleSetting = { key: 'terminus-map-style', defaultValue: 'auto' }

/**
 * Odabrana podloga, zapamćena u pregledniku; zadano ista na svim kartama, a
 * karta sa svojom postavkom (npr. karta uživo) pamti svoju.
 */
export function useMapStyle(setting: MapStyleSetting = sharedMapStyle) {
  const { key, defaultValue } = setting
  return useLocalStorage<MapStyle>({
    key,
    defaultValue,
    deserialize: (value) => {
      try {
        const parsed: unknown = value === undefined ? defaultValue : JSON.parse(value)
        return isMapStyle(parsed) ? parsed : defaultValue
      } catch {
        return defaultValue
      }
    },
  })
}
