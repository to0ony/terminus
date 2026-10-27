import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { render } from '@/test/render'
import { DataTable } from './DataTable'
import type { DataColumn, SortState } from './tableModel'

interface Row {
  id: string
  name: string
}

const rows: Row[] = [
  { id: 'a', name: 'Autobusni kolodvor' },
  { id: 'b', name: 'Bolnica' },
  { id: 'c', name: 'Centar' },
]

const columns: DataColumn<Row>[] = [
  { id: 'name', header: 'Naziv', sortKey: 'Name', cell: (row) => row.name },
]

function Table({ onSort }: { onSort: (sort: SortState) => void }) {
  const [selection, setSelection] = useState<Set<string>>(new Set())
  return (
    <>
      <DataTable
        rows={rows}
        columns={columns}
        getRowId={(row) => row.id}
        getRowHref={(row) => `/stops/${row.id}`}
        sort={{ key: null, descending: false }}
        onSortChange={onSort}
        selection={selection}
        onSelectionChange={setSelection}
      />
      <output>{[...selection].sort().join(',')}</output>
    </>
  )
}

describe('DataTable', () => {
  it('sorts by a column when its header is clicked', async () => {
    const onSort = vi.fn()
    render(<Table onSort={onSort} />)

    await userEvent.click(screen.getByRole('button', { name: /Naziv/ }))

    expect(onSort).toHaveBeenCalledWith({ key: 'Name', descending: false })
  })

  it('selects a range with Shift and the whole page with the header checkbox', async () => {
    const user = userEvent.setup()
    render(<Table onSort={vi.fn()} />)
    const [all, first, , third] = screen.getAllByRole('checkbox')

    await user.click(first)
    await user.keyboard('{Shift>}')
    await user.click(third)
    await user.keyboard('{/Shift}')
    expect(screen.getByRole('status')).toHaveTextContent('a,b,c')

    await user.click(all)
    expect(screen.getByRole('status')).toHaveTextContent('')
  })

  it('moves through rows and selects them from the keyboard', async () => {
    render(<Table onSort={vi.fn()} />)

    await userEvent.keyboard('{ArrowDown}{ArrowDown}x')

    expect(screen.getByRole('status')).toHaveTextContent('b')
    await userEvent.keyboard('{Escape}')
    expect(screen.getByRole('status')).toHaveTextContent('')
  })
})
