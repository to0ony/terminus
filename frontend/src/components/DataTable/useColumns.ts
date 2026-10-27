import { useLocalStorage } from '@mantine/hooks'
import { type DataColumn, isColumnVisible } from './dataTable'

export interface ColumnsState<T> {
  all: DataColumn<T>[]
  visible: DataColumn<T>[]
  isVisible: (column: DataColumn<T>) => boolean
  setVisible: (id: string, visible: boolean) => void
  reset: () => void
}

/** Izbor stupaca tablice, zapamćen u pregledniku pod `terminus-columns-{tableKey}`. */
export function useColumns<T>(tableKey: string, columns: DataColumn<T>[]): ColumnsState<T> {
  const [overrides, setOverrides] = useLocalStorage<Record<string, boolean>>({
    key: `terminus-columns-${tableKey}`,
    defaultValue: {},
    getInitialValueInEffect: false,
    deserialize: (value) => {
      try {
        const parsed: unknown = value === undefined ? {} : JSON.parse(value)
        return parsed && typeof parsed === 'object' ? (parsed as Record<string, boolean>) : {}
      } catch {
        return {}
      }
    },
  })

  const isVisible = (column: DataColumn<T>) => isColumnVisible(column, overrides)

  return {
    all: columns,
    visible: columns.filter(isVisible),
    isVisible,
    setVisible: (id, visible) => setOverrides((current) => ({ ...current, [id]: visible })),
    reset: () => setOverrides({}),
  }
}
