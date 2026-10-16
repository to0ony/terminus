import { MantineProvider } from '@mantine/core'
import { render as testingLibraryRender } from '@testing-library/react'
import type { ReactNode } from 'react'
import { theme } from '@/app/theme'

// Render s Mantine providerom; komponente bez njega ne rade.
export function render(ui: ReactNode) {
  return testingLibraryRender(ui, {
    wrapper: ({ children }) => <MantineProvider theme={theme}>{children}</MantineProvider>,
  })
}
