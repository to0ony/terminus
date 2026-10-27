import {
  Box,
  Checkbox,
  Group,
  Kbd,
  Paper,
  Stack,
  Table,
  Text,
  UnstyledButton,
  VisuallyHidden,
} from '@mantine/core'
import { useMediaQuery } from '@mantine/hooks'
import { IconArrowDown, IconArrowUp, IconSelector } from '@tabler/icons-react'
import { type MouseEvent, type ReactNode, useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router'
import { isCtrl, isTyping } from '@/lib/keyboard'
import { type DataColumn, moveIndex, nextSort, selectRange, type SortState } from './tableModel'
import classes from './DataTable.module.css'

interface DataTableProps<T> {
  rows: T[]
  /** Vidljivi stupci, redom (iz useColumns). */
  columns: DataColumn<T>[]
  getRowId: (row: T) => string
  /** Adresa retka: klik i Enter je otvaraju, Ctrl+klik u novoj kartici. */
  getRowHref: (row: T) => string
  sort: SortState
  onSortChange: (sort: SortState) => void
  /** Odabrani reci; bez toga tablica nema kvačica. */
  selection?: ReadonlySet<string>
  onSelectionChange?: (selection: Set<string>) => void
  /** Stari podaci dok stižu novi (druga stranica, filtar). */
  stale?: boolean
  onRowHover?: (row: T | null) => void
  /** Visina tablice; zaglavlje ostaje vidljivo dok se pomiče. */
  maxHeight?: string
  /** Tipka `/`: fokus na pretragu stranice. */
  onSearchKey?: () => void
  /** Tipke ← →: prethodna i sljedeća stranica. */
  onPageKey?: (step: 1 | -1) => void
}

/**
 * Tablica popisa (linije, stanice): slaganje klikom na zaglavlje, odabir
 * kvačicama (Shift za raspon), upravljanje tipkovnicom i kartice na mobitelu.
 * Podaci, stranice i slaganje su na serveru; tablica samo prikazuje stranicu.
 */
export function DataTable<T>(props: DataTableProps<T>) {
  const { rows, columns, getRowId, getRowHref, selection, onSelectionChange } = props
  const navigate = useNavigate()
  const mobile = useMediaQuery('(max-width: 48em)')
  const ids = rows.map(getRowId)
  // Aktivni redak i sidro raspona vrijede samo za reke za koje su postavljeni;
  // nova stranica ili filtar ih poništava.
  const rowsKey = ids.join()
  const [activeState, setActiveState] = useState<{ rowsKey: string; index: number | null }>({
    rowsKey,
    index: null,
  })
  const active = activeState.rowsKey === rowsKey ? activeState.index : null
  const setActive = (index: number | null) => setActiveState({ rowsKey, index })
  const anchorRef = useRef<{ rowsKey: string; index: number } | null>(null)

  const selectable = selection !== undefined && onSelectionChange !== undefined
  const selectedOnPage = selectable ? ids.filter((id) => selection.has(id)).length : 0

  const toggle = (index: number, shift: boolean) => {
    if (!selectable) {
      return
    }
    const id = ids[index]
    const select = !selection.has(id)
    const anchor = anchorRef.current?.rowsKey === rowsKey ? anchorRef.current.index : null
    if (shift && anchor !== null) {
      onSelectionChange(selectRange(ids, selection, anchor, index, select))
    } else {
      const next = new Set(selection)
      if (select) {
        next.add(id)
      } else {
        next.delete(id)
      }
      onSelectionChange(next)
    }
    anchorRef.current = { rowsKey, index }
  }

  const togglePage = () => {
    if (!selectable) {
      return
    }
    const next = new Set(selection)
    const all = selectedOnPage === ids.length
    for (const id of ids) {
      if (all) {
        next.delete(id)
      } else {
        next.add(id)
      }
    }
    onSelectionChange(next)
  }

  const open = (row: T, newTab: boolean) => {
    const href = getRowHref(row)
    if (newTab) {
      window.open(href, '_blank', 'noopener')
    } else {
      void navigate(href)
    }
  }

  const onRowClick = (event: MouseEvent, row: T, index: number) => {
    // Poveznice, gumbi i kvačice u retku rade svoje.
    if ((event.target as HTMLElement).closest('a, button, input, label')) {
      return
    }
    if (event.shiftKey && selectable) {
      toggle(index, true)
      return
    }
    setActive(index)
    open(row, event.ctrlKey || event.metaKey)
  }

  const moveTo = (index: number | null) => {
    setActive(index)
    if (index !== null) {
      document.querySelector(`[data-row-index="${index}"]`)?.scrollIntoView({ block: 'nearest' })
    }
  }

  // Tipkovnica: ↑↓ redak, Enter otvara, X ili razmak odabire, ← → stranica.
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      // Dijalozi i izbornici imaju svoje tipke.
      if (
        isTyping(event.target) ||
        document.querySelector('[role="dialog"], [role="menu"], [role="listbox"]')
      ) {
        return
      }
      const plain = !event.ctrlKey && !event.metaKey && !event.altKey
      if (
        event.key === 'ArrowDown' ||
        event.key === 'ArrowUp' ||
        event.key === 'j' ||
        event.key === 'k'
      ) {
        if (!plain) {
          return
        }
        const step = event.key === 'ArrowDown' || event.key === 'j' ? 1 : -1
        const next = moveIndex(active, step, rows.length)
        if (event.shiftKey && selectable && next !== null && !selection.has(ids[next])) {
          if (active !== null && !selection.has(ids[active])) {
            toggle(active, false)
          }
          onSelectionChange(new Set(selection).add(ids[next]))
        }
        moveTo(next)
      } else if ((event.key === 'Enter' || event.key === 'o') && plain && active !== null) {
        open(rows[active], false)
      } else if (
        (event.key === 'x' || event.key === ' ') &&
        plain &&
        active !== null &&
        selectable
      ) {
        toggle(active, false)
      } else if (isCtrl(event, 'a') && selectable) {
        togglePage()
      } else if (event.key === 'Escape' && (selectedOnPage > 0 || active !== null)) {
        if (selectable && selection.size > 0) {
          onSelectionChange(new Set())
        } else {
          setActive(null)
        }
      } else if (event.key === '/' && plain && props.onSearchKey) {
        props.onSearchKey()
      } else if (
        (event.key === 'ArrowLeft' || event.key === 'ArrowRight') &&
        plain &&
        props.onPageKey
      ) {
        props.onPageKey(event.key === 'ArrowLeft' ? -1 : 1)
      } else {
        return
      }
      event.preventDefault()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  })

  const rowCheckbox = (row: T, index: number) => (
    <Checkbox
      size="sm"
      aria-label="Odaberi"
      checked={selection?.has(getRowId(row)) ?? false}
      onChange={() => undefined}
      onClick={(event) => {
        event.stopPropagation()
        toggle(index, event.shiftKey)
      }}
    />
  )

  if (mobile) {
    return (
      <Stack gap="xs" style={{ opacity: props.stale ? 0.6 : 1 }}>
        {rows.map((row, index) => (
          <MobileCard
            key={ids[index]}
            row={row}
            columns={columns}
            checkbox={selectable ? rowCheckbox(row, index) : null}
            selected={selection?.has(ids[index]) ?? false}
            onOpen={(event) => onRowClick(event, row, index)}
          />
        ))}
      </Stack>
    )
  }

  return (
    <Stack gap={6}>
      <Paper withBorder className={classes.frame}>
        <Box className={classes.scroll} mah={props.maxHeight ?? 'calc(100dvh - 280px)'}>
          <Table
            stickyHeader
            highlightOnHover
            className={classes.table}
            style={{ opacity: props.stale ? 0.6 : 1 }}
          >
            <Table.Thead>
              <Table.Tr>
                {selectable && (
                  <Table.Th w={40}>
                    <Checkbox
                      size="sm"
                      aria-label="Odaberi sve na stranici"
                      checked={ids.length > 0 && selectedOnPage === ids.length}
                      indeterminate={selectedOnPage > 0 && selectedOnPage < ids.length}
                      onChange={togglePage}
                    />
                  </Table.Th>
                )}
                {columns.map((column) => (
                  <Table.Th key={column.id} w={column.width} ta={column.align}>
                    {column.sortKey ? (
                      <SortHeader
                        column={column}
                        sort={props.sort}
                        onClick={() => props.onSortChange(nextSort(props.sort, column.sortKey!))}
                      />
                    ) : (
                      column.header
                    )}
                  </Table.Th>
                ))}
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {rows.map((row, index) => {
                const selected = selection?.has(ids[index]) ?? false
                return (
                  <Table.Tr
                    key={ids[index]}
                    data-row-index={index}
                    data-active={index === active || undefined}
                    data-selected={selected || undefined}
                    className={classes.row}
                    onClick={(event) => onRowClick(event, row, index)}
                    onAuxClick={(event) => {
                      // Srednja tipka miša: nova kartica.
                      if (event.button === 1) {
                        open(row, true)
                      }
                    }}
                    onMouseEnter={() => props.onRowHover?.(row)}
                    onMouseLeave={() => props.onRowHover?.(null)}
                  >
                    {selectable && <Table.Td>{rowCheckbox(row, index)}</Table.Td>}
                    {columns.map((column) => (
                      <Table.Td key={column.id} ta={column.align}>
                        {column.cell(row)}
                      </Table.Td>
                    ))}
                  </Table.Tr>
                )
              })}
            </Table.Tbody>
          </Table>
        </Box>
      </Paper>
      <KeyboardHints selectable={selectable} />
    </Stack>
  )
}

/** Poravnanje zaglavlja kao poravnanje stupca. */
const justify = { left: 'flex-start', center: 'center', right: 'flex-end' } as const

function SortHeader<T>({
  column,
  sort,
  onClick,
}: {
  column: DataColumn<T>
  sort: SortState
  onClick: () => void
}) {
  const sorted = sort.key === column.sortKey
  const Icon = !sorted ? IconSelector : sort.descending ? IconArrowDown : IconArrowUp
  return (
    <UnstyledButton
      onClick={onClick}
      className={classes.sort}
      data-sorted={sorted || undefined}
      aria-sort={sorted ? (sort.descending ? 'descending' : 'ascending') : 'none'}
    >
      <Group gap={4} wrap="nowrap" justify={justify[column.align ?? 'left']}>
        <span>{column.header}</span>
        <Icon size={14} stroke={1.8} />
        <VisuallyHidden>
          {sorted ? (sort.descending ? 'silazno' : 'uzlazno') : 'slaži po ovom stupcu'}
        </VisuallyHidden>
      </Group>
    </UnstyledButton>
  )
}

function MobileCard<T>({
  row,
  columns,
  checkbox,
  selected,
  onOpen,
}: {
  row: T
  columns: DataColumn<T>[]
  checkbox: ReactNode
  selected: boolean
  onOpen: (event: MouseEvent) => void
}) {
  const leading = columns.filter((c) => c.mobile === 'leading')
  const title = columns.filter((c) => c.mobile === 'title')
  const meta = columns.filter((c) => c.mobile === 'meta')
  return (
    <Paper
      withBorder
      p="sm"
      className={classes.card}
      data-selected={selected || undefined}
      onClick={onOpen}
    >
      <Group gap="sm" wrap="nowrap" align="flex-start">
        {checkbox}
        <Stack gap={4} style={{ minWidth: 0, flex: 1 }}>
          <Group gap="xs" wrap="nowrap">
            {leading.map((c) => (
              <Box key={c.id}>{c.cell(row)}</Box>
            ))}
            {title.map((c) => (
              <Box key={c.id} style={{ minWidth: 0 }}>
                {c.cell(row)}
              </Box>
            ))}
          </Group>
          {meta.length > 0 && (
            <Group gap="md" style={{ rowGap: 4 }}>
              {meta.map((c) => (
                <Group key={c.id} gap={6} wrap="nowrap">
                  <Text size="xs" c="dimmed">
                    {c.header}
                  </Text>
                  <Box fz="sm">{c.cell(row)}</Box>
                </Group>
              ))}
            </Group>
          )}
        </Stack>
      </Group>
    </Paper>
  )
}

function KeyboardHints({ selectable }: { selectable: boolean }) {
  return (
    <Text size="xs" c="dimmed">
      <Kbd size="xs">↑ ↓</Kbd> redak · <Kbd size="xs">Enter</Kbd> otvori ·{' '}
      {selectable && (
        <>
          <Kbd size="xs">X</Kbd> odaberi (<Kbd size="xs">Shift</Kbd> raspon) ·{' '}
          <Kbd size="xs">Ctrl+A</Kbd> cijela stranica · <Kbd size="xs">Esc</Kbd> poništi odabir
          ·{' '}
        </>
      )}
      <Kbd size="xs">← →</Kbd> stranica · <Kbd size="xs">/</Kbd> traži
    </Text>
  )
}
