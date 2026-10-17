import type { BoardDeparture } from './queries'

/** Do koliko minuta se prikazuje "za X min", a iza toga sat polaska. */
export const minutesShownUntil = 10

/** Koliko se polazak još prikazuje nakon svog vremena ("stiže"). */
const graceMs = 30_000

/** Pomak zone u minutama iz ISO vremena: "…+02:00" → 120, "…Z" → 0. */
export function offsetMinutes(isoTime: string): number {
  const match = /([+-])(\d{2}):(\d{2})$/.exec(isoTime)
  if (!match) {
    return 0
  }
  const minutes = Number(match[2]) * 60 + Number(match[3])
  return match[1] === '-' ? -minutes : minutes
}

/** Sat (HH:MM) u zoni s pomakom offset, neovisno o zoni preglednika. */
export function clockText(nowMs: number, offset: number): string {
  const local = new Date(nowMs + offset * 60_000)
  const hours = String(local.getUTCHours()).padStart(2, '0')
  const minutes = String(local.getUTCMinutes()).padStart(2, '0')
  return `${hours}:${minutes}`
}

/**
 * Polasci koji još nisu otišli. Odgovor može biti iz cachea (do 20 s), pa se
 * odlazak provjerava i na zaslonu.
 */
export function upcoming(departures: BoardDeparture[], nowMs: number): BoardDeparture[] {
  return departures.filter((d) => Date.parse(d.departureTime) >= nowMs - graceMs)
}

/** Cijele minute do polaska, najmanje 0. */
export function minutesUntil(departureTime: string, nowMs: number): number {
  return Math.max(0, Math.floor((Date.parse(departureTime) - nowMs) / 60_000))
}

/**
 * "stiže", "za 4 min" ili sat polaska (HH:MM). Sat se čita iz teksta jer je
 * vrijeme već u zoni prijevoznika.
 */
export function whenText(departureTime: string, nowMs: number): string {
  const minutes = minutesUntil(departureTime, nowMs)
  if (minutes < 1) {
    return 'stiže'
  }
  if (minutes <= minutesShownUntil) {
    return `za ${minutes} min`
  }
  return departureTime.slice(11, 16)
}

/** ?limit= iz adrese; zadano 12, najviše 30 (granica backenda). */
export function parseLimit(value: string | null): number {
  const limit = Number(value)
  return Number.isInteger(limit) && limit >= 1 ? Math.min(limit, 30) : 12
}
