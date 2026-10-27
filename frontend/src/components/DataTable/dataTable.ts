import type { ReactNode } from 'react'

/** Stupac tablice; vrijednosti dolaze iz retka, a slaganje radi server. */
export interface DataColumn<T> {
  id: string
  header: string
  cell: (row: T) => ReactNode
  /** Naziv stupca za sortBy na serveru; bez njega se po stupcu ne slaže. */
  sortKey?: string
  /** Skriven dok ga korisnik ne uključi u izborniku „Stupci”. */
  hiddenByDefault?: boolean
  /** Ne može se sakriti (npr. naziv). */
  alwaysVisible?: boolean
  align?: 'left' | 'right' | 'center'
  width?: number
  /**
   * Na mobitelu: `leading` ispred naslova (npr. oznaka linije), `title` kao
   * naslov kartice, `meta` u retku ispod; bez toga se na kartici ne vidi.
   */
  mobile?: 'leading' | 'title' | 'meta'
}

export interface SortState {
  key: string | null
  descending: boolean
}

/** Klik na zaglavlje: uzlazno, silazno, pa zadani redoslijed. */
export function nextSort(current: SortState, key: string): SortState {
  if (current.key !== key) {
    return { key, descending: false }
  }
  return current.descending ? { key: null, descending: false } : { key, descending: true }
}

/** Vidljivost stupca: izbor korisnika, a bez njega zadano za stupac. */
export function isColumnVisible<T>(column: DataColumn<T>, overrides: Record<string, boolean>) {
  return column.alwaysVisible === true || (overrides[column.id] ?? !column.hiddenByDefault)
}

/** Odabir rasponom (Shift+klik): svi od sidra do retka dobivaju stanje kliknutog. */
export function selectRange(
  ids: string[],
  selected: ReadonlySet<string>,
  anchor: number,
  index: number,
  select: boolean,
): Set<string> {
  const next = new Set(selected)
  const [from, to] = anchor < index ? [anchor, index] : [index, anchor]
  for (const id of ids.slice(from, to + 1)) {
    if (select) {
      next.add(id)
    } else {
      next.delete(id)
    }
  }
  return next
}

/** Sljedeći aktivni redak strelicom; bez aktivnog prvi (dolje) ili zadnji (gore). */
export function moveIndex(current: number | null, step: 1 | -1, count: number): number | null {
  if (count === 0) {
    return null
  }
  if (current === null) {
    return step === 1 ? 0 : count - 1
  }
  return Math.min(Math.max(current + step, 0), count - 1)
}
