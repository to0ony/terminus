import type { MantineColorScheme } from '@mantine/core'
import { IconDeviceDesktop, IconMoon, IconSun } from '@tabler/icons-react'

/** Teme sučelja; izbor pamti Mantine u pregledniku. */
export const colorSchemes: {
  value: MantineColorScheme
  label: string
  icon: typeof IconSun
}[] = [
  { value: 'auto', label: 'Prema sustavu', icon: IconDeviceDesktop },
  { value: 'light', label: 'Svijetla', icon: IconSun },
  { value: 'dark', label: 'Tamna', icon: IconMoon },
]
