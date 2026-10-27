import { createTheme, Table } from '@mantine/core'
import type { Density } from '@/features/appearance/density'

/**
 * Tema dashboarda. Gustoća mijenja zadani razmak redaka tablica; tablice sa
 * svojim razmakom (npr. vozni red) ga zadržavaju.
 */
export function appTheme(density: Density) {
  return createTheme({
    primaryColor: 'blue',
    defaultRadius: 'md',
    components: {
      Table: Table.extend({
        defaultProps:
          density === 'compact'
            ? { verticalSpacing: 3, horizontalSpacing: 'xs' }
            : { verticalSpacing: 'sm', horizontalSpacing: 'sm' },
      }),
    },
  })
}
