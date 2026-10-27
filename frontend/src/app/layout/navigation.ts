import {
  IconBuildingCommunity,
  IconCalendarEvent,
  IconFileZip,
  IconLayoutDashboard,
  IconMap2,
  IconMapPin,
  IconRoute,
  IconSettings,
  type Icon,
} from '@tabler/icons-react'

export interface NavigationItem {
  to: string
  label: string
  icon: Icon
}

/** Stavke izbornika dashboarda; rute su u router.tsx. */
export const navigation: NavigationItem[] = [
  { to: '/', label: 'Pregled', icon: IconLayoutDashboard },
  { to: '/live', label: 'Karta uživo', icon: IconMap2 },
  { to: '/routes', label: 'Linije', icon: IconRoute },
  { to: '/stops', label: 'Stanice', icon: IconMapPin },
  { to: '/calendars', label: 'Kalendari', icon: IconCalendarEvent },
  { to: '/agencies', label: 'Prijevoznici', icon: IconBuildingCommunity },
  { to: '/gtfs', label: 'GTFS uvoz i izvoz', icon: IconFileZip },
  { to: '/settings', label: 'Postavke', icon: IconSettings },
]
