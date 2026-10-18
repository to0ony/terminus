/**
 * Prijedlog GTFS stop_id iz naziva i perona: mala slova bez dijakritike,
 * riječi spojene crticom (npr. "Glavni kolodvor", "A" → "glavni-kolodvor-a").
 */
export function suggestStopCode(name: string, platformCode?: string | null): string {
  const text = [name, platformCode].filter(Boolean).join(' ')
  return text
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 60)
}

/** Sljedeći kandidat kad je kod zauzet: "x", "x-2", "x-3", … */
export function nextStopCode(code: string, attempt: number): string {
  return attempt === 0 ? code : `${code}-${attempt + 1}`
}
