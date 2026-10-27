import { useSearchParams } from 'react-router'
import type { SortState } from './tableModel'

export const pageSizes = [25, 50, 100]

/**
 * Stanje tablice u adresi: filtri (po nazivu), `sort` i `desc`, `page` i
 * `size`. Promjena filtra ili redoslijeda vraća na prvu stranicu, pa se prikaz
 * može podijeliti poveznicom i preživi osvježavanje.
 */
export function useTableParams(defaultPageSize = 25) {
  const [searchParams, setSearchParams] = useSearchParams()

  const update = (change: (params: URLSearchParams) => void, keepPage = false) =>
    setSearchParams(
      (params) => {
        change(params)
        if (!keepPage) {
          params.delete('page')
        }
        return params
      },
      { replace: true },
    )

  const size = Number(searchParams.get('size'))
  const pageSize = pageSizes.includes(size) ? size : defaultPageSize
  const sort: SortState = {
    key: searchParams.get('sort'),
    descending: searchParams.get('desc') === '1',
  }

  return {
    get: (key: string) => searchParams.get(key),
    setFilter: (key: string, value: string | null) =>
      update((params) => (value ? params.set(key, value) : params.delete(key))),
    /** Briše sve filtre; redoslijed i veličina stranice ostaju. */
    clearFilters: (keys: string[]) => update((params) => keys.forEach((key) => params.delete(key))),
    sort,
    setSort: ({ key, descending }: SortState) =>
      update((params) => {
        if (key) {
          params.set('sort', key)
        } else {
          params.delete('sort')
        }
        if (descending) {
          params.set('desc', '1')
        } else {
          params.delete('desc')
        }
      }),
    page: Number(searchParams.get('page')) || 1,
    setPage: (page: number) =>
      update(
        (params) => (page > 1 ? params.set('page', String(page)) : params.delete('page')),
        true,
      ),
    pageSize,
    setPageSize: (value: number) =>
      update((params) =>
        value === defaultPageSize ? params.delete('size') : params.set('size', String(value)),
      ),
  }
}
