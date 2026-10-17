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
