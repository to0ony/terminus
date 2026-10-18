const toDate = (value: string) => new Date(`${value}T00:00:00Z`)

/** Isti datum godinu kasnije (29. 2. postaje 1. 3.). */
export function nextYear(value: string): string {
  const date = toDate(value)
  date.setUTCFullYear(date.getUTCFullYear() + 1)
  return date.toISOString().slice(0, 10)
}

/**
 * Naziv kopije za sljedeću godinu: godine u nazivu se pomiču za jednu
 * ("Zimski 2026/27" → "Zimski 2027/28"); bez godina dodaje se "(kopija)".
 */
export function copyName(name: string): string {
  if (!/\d{4}/.test(name)) {
    return `${name} (kopija)`
  }
  return name
    .replace(
      /\b(\d{4})\/(\d{2})\b/g,
      (_, full: string, short: string) =>
        `${Number(full) + 1}/${String((Number(short) + 1) % 100).padStart(2, '0')}`,
    )
    .replace(/\b(\d{4})\b(?!\/)/g, (year: string) => String(Number(year) + 1))
}
