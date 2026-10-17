// GTFS stops.location_type.
const labels: Record<number, string> = {
  0: 'Stajalište',
  1: 'Kolodvor',
  2: 'Ulaz / izlaz',
  3: 'Čvor',
  4: 'Ukrcajno mjesto',
}

export function locationTypeLabel(locationType: number): string {
  return labels[locationType] ?? `Vrsta ${locationType}`
}

export const locationTypeOptions = Object.entries(labels).map(([value, label]) => ({
  value,
  label,
}))

export const wheelchairLabels: Record<number, string> = {
  0: 'Nema podataka',
  1: 'Pristupačno',
  2: 'Nije pristupačno',
}
