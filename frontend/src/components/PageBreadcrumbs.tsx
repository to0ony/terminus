import { Anchor, Breadcrumbs, Text } from '@mantine/core'
import { IconChevronRight } from '@tabler/icons-react'
import { Link } from 'react-router'

export interface Crumb {
  label: string
  /** Bez adrese je trenutna stranica (zadnja stavka). */
  to?: string
}

/** Put do stranice iznad naslova, npr. Linije › 4A › Grafikon voznog reda. */
export function PageBreadcrumbs({ items }: { items: Crumb[] }) {
  return (
    <Breadcrumbs
      separator={<IconChevronRight size={14} stroke={1.6} />}
      separatorMargin={6}
      styles={{ root: { flexWrap: 'wrap', rowGap: 4 } }}
    >
      {items.map((item) =>
        item.to ? (
          <Anchor key={item.label} component={Link} to={item.to} size="sm">
            {item.label}
          </Anchor>
        ) : (
          <Text key={item.label} size="sm" c="dimmed" aria-current="page" truncate maw={320}>
            {item.label}
          </Text>
        ),
      )}
    </Breadcrumbs>
  )
}
