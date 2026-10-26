import {
  type Icon,
  IconBus,
  IconCar,
  IconFerry,
  IconMountain,
  IconPlane,
  IconShip,
  IconTrain,
} from '@tabler/icons-react'

// Osnovne GTFS vrste prijevoza (routes.route_type) i nazivi za prikaz.
const basicTypes: Record<number, string> = {
  0: 'Tramvaj',
  1: 'Metro',
  2: 'Vlak',
  3: 'Autobus',
  4: 'Brod',
  5: 'Žičani tramvaj',
  6: 'Žičara',
  7: 'Uspinjača',
  11: 'Trolejbus',
  12: 'Monorail',
}

// Proširene vrste (100–1799) po skupinama od stotinu.
const extendedGroups: Record<number, string> = {
  100: 'Vlak',
  200: 'Međugradski autobus',
  400: 'Gradska željeznica',
  700: 'Autobus',
  800: 'Trolejbus',
  900: 'Tramvaj',
  1000: 'Brod',
  1100: 'Zračni prijevoz',
  1200: 'Trajekt',
  1300: 'Žičara',
  1400: 'Uspinjača',
  1500: 'Taksi',
  1700: 'Ostalo',
}

/** Vrste za filtar popisa linija. */
export const routeTypeOptions = Object.entries(basicTypes).map(([value, label]) => ({
  value,
  label,
}))

export function routeTypeLabel(routeType: number): string {
  return (
    basicTypes[routeType] ??
    extendedGroups[Math.floor(routeType / 100) * 100] ??
    `Vrsta ${routeType}`
  )
}

// Ikona vrste prijevoza; osnovne vrste i skupine proširenih (po stotinu).
const basicIcons: Record<number, Icon> = {
  0: IconTrain,
  1: IconTrain,
  2: IconTrain,
  3: IconBus,
  4: IconShip,
  5: IconTrain,
  6: IconMountain,
  7: IconMountain,
  11: IconBus,
  12: IconTrain,
}

const extendedIcons: Record<number, Icon> = {
  100: IconTrain,
  200: IconBus,
  400: IconTrain,
  700: IconBus,
  800: IconBus,
  900: IconTrain,
  1000: IconShip,
  1100: IconPlane,
  1200: IconFerry,
  1300: IconMountain,
  1400: IconMountain,
  1500: IconCar,
}

/** Ikona vrste prijevoza (autobus, vlak, brod…); nepoznata vrsta je autobus. */
export function routeTypeIcon(routeType: number): Icon {
  return basicIcons[routeType] ?? extendedIcons[Math.floor(routeType / 100) * 100] ?? IconBus
}
