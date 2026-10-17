const numberFormat = new Intl.NumberFormat('hr-HR')

export function formatNumber(value: number): string {
  return numberFormat.format(value)
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`
  }
  if (bytes < 1024 * 1024) {
    return `${formatDecimal(bytes / 1024)} KB`
  }
  return `${formatDecimal(bytes / (1024 * 1024))} MB`
}

function formatDecimal(value: number): string {
  return value.toLocaleString('hr-HR', { maximumFractionDigits: 1 })
}

/**
 * GTFS vrijeme (HH:MM:SS, sati mogu biti iznad 24 za vožnje iza ponoći) kao
 * sat na satu: "25:10:00" → "01:10".
 */
export function formatGtfsTime(value: string): string {
  const [hours, minutes] = value.split(':')
  return `${String(Number(hours) % 24).padStart(2, '0')}:${minutes}`
}

/** Datum (YYYY-MM-DD) na hrvatski način: "2026-09-28" → "28. 9. 2026." */
export function formatDate(value: string): string {
  const [year, month, day] = value.split('-')
  return `${Number(day)}. ${Number(month)}. ${year}.`
}
