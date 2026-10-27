import { Group, Pagination, Select, Text } from '@mantine/core'
import { formatNumber } from '@/lib/format'
import { pageSizes } from './useTableParams'

interface TableFooterProps {
  totalCount: number
  page: number
  pageSize: number
  onPage: (page: number) => void
  onPageSize: (size: number) => void
}

/** Ispod tablice: koji su reci prikazani, veličina stranice i stranice. */
export function TableFooter({ totalCount, page, pageSize, onPage, onPageSize }: TableFooterProps) {
  const pages = Math.max(1, Math.ceil(totalCount / pageSize))
  const first = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const last = Math.min(page * pageSize, totalCount)

  return (
    <Group justify="space-between" gap="sm">
      <Group gap="xs">
        <Text size="sm" c="dimmed">
          {formatNumber(first)}–{formatNumber(last)} od {formatNumber(totalCount)}
        </Text>
        <Select
          size="xs"
          w={110}
          aria-label="Redaka po stranici"
          data={pageSizes.map((size) => ({ value: String(size), label: `${size} / str.` }))}
          value={String(pageSize)}
          onChange={(value) => value && onPageSize(Number(value))}
          allowDeselect={false}
        />
      </Group>
      {pages > 1 && (
        <Pagination size="sm" total={pages} value={page} siblings={1} onChange={onPage} />
      )}
    </Group>
  )
}
