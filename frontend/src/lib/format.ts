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
