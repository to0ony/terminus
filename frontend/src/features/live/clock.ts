/** Sekundi u danu. */
export const day = 24 * 60 * 60

const formatters = new Map<string, Intl.DateTimeFormat>()

function formatter(timeZone: string) {
  let format = formatters.get(timeZone)
  if (!format) {
    format = new Intl.DateTimeFormat('en-CA', {
      timeZone,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hourCycle: 'h23',
    })
    formatters.set(timeZone, format)
  }
  return format
}

/** Današnji datum (YYYY-MM-DD) i sekunde od ponoći u zadanoj zoni, s dijelovima sekunde. */
export function nowInZone(timeZone: string, now = new Date()): { date: string; seconds: number } {
  const parts = Object.fromEntries(
    formatter(timeZone)
      .formatToParts(now)
      .map((part) => [part.type, part.value]),
  )
  return {
    date: `${parts.year}-${parts.month}-${parts.day}`,
    seconds:
      Number(parts.hour) * 3600 +
      Number(parts.minute) * 60 +
      Number(parts.second) +
      now.getMilliseconds() / 1000,
  }
}

/** Sekunde od ponoći kao HH:MM:SS (ili HH:MM); iza ponoći ide dalje od 24. */
export function formatTime(seconds: number, withSeconds = true): string {
  const whole = Math.floor(seconds)
  const pad = (value: number) => String(value).padStart(2, '0')
  const hours = pad(Math.floor(whole / 3600))
  const minutes = pad(Math.floor((whole % 3600) / 60))
  return withSeconds ? `${hours}:${minutes}:${pad(whole % 60)}` : `${hours}:${minutes}`
}

/** Zona preglednika, dok se ne zna zona prijevoznika. */
export const browserTimeZone = Intl.DateTimeFormat().resolvedOptions().timeZone
