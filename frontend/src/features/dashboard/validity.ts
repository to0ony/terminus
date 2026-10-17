import dayjs from 'dayjs'
import type { ServiceOverview } from './queries'

/** Ispod ovoliko dana do kraja voznog reda pregled upozorava. */
export const expiryWarningDays = 30

export type Validity =
  { state: 'expired' } | { state: 'expiring' | 'valid'; lastDate: string; daysLeft: number }

/** Koliko još vrijedi vozni red, računajući od dana pregleda (ne iz preglednika). */
export function validity(overview: ServiceOverview): Validity {
  if (!overview.lastServiceDate) {
    return { state: 'expired' }
  }
  const daysLeft = dayjs(overview.lastServiceDate).diff(dayjs(overview.date), 'day')
  return {
    state: daysLeft < expiryWarningDays ? 'expiring' : 'valid',
    lastDate: overview.lastServiceDate,
    daysLeft,
  }
}
