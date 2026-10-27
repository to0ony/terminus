import { createTheme, Table } from '@mantine/core'
import type { Density } from '@/features/appearance/density'

/**
 * Tema dashboarda. Gustoća zadaje visinu redaka tablica po Material Designu:
 * udobno 52 px, zbijeno 36 px (redak teksta 14 px je visok oko 22 px).
 * Tablice sa svojim razmakom (vozni red, editori) ga zadržavaju.
 */
export function appTheme(density: Density) {
  return createTheme({
    primaryColor: 'blue',
    defaultRadius: 'md',
    components: {
      Table: Table.extend({
        defaultProps:
          density === 'compact'
            ? { verticalSpacing: 7, horizontalSpacing: 'sm' }
            : { verticalSpacing: 15, horizontalSpacing: 'md' },
      }),
    },
  })
}
