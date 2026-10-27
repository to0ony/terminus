import { CloseButton, Group, Paper, Portal, Text, Transition } from '@mantine/core'
import type { ReactNode } from 'react'
import { formatNumber } from '@/lib/format'
import classes from './BulkBar.module.css'

interface BulkBarProps {
  count: number
  /** Npr. „stanica” / „stanice”; broj ide ispred. */
  noun: (count: number) => string
  onClear: () => void
  children: ReactNode
}

/** Traka grupnih akcija na dnu zaslona dok je nešto odabrano. */
export function BulkBar({ count, noun, onClear, children }: BulkBarProps) {
  return (
    <Portal>
      <Transition mounted={count > 0} transition="slide-up" duration={150}>
        {(styles) => (
          // Pomak animacije je na Paperu, a centriranje na omotaču, da se transformacije ne sudare.
          <div className={classes.bar}>
            <Paper shadow="lg" withBorder p="xs" pl="md" style={styles}>
              <Group gap="sm" wrap="nowrap">
                <Text size="sm" fw={600} style={{ whiteSpace: 'nowrap' }}>
                  {formatNumber(count)} {noun(count)}
                </Text>
                <Group gap={6} wrap="wrap">
                  {children}
                </Group>
                <CloseButton aria-label="Poništi odabir" onClick={onClear} />
              </Group>
            </Paper>
          </div>
        )}
      </Transition>
    </Portal>
  )
}
