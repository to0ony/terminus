import { Button, Group, Text } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { errorMessage } from '@/api/errors'

/** Koliko dugo obavijest nudi poništavanje. */
export const undoDelay = 6000

interface UndoNoticeOptions {
  message: string
  onUndo: () => void
}

/** Obavijest s gumbom „Poništi”; nakon klika nestaje. */
export function showUndo({ message, onUndo }: UndoNoticeOptions) {
  const id = notifications.show({
    autoClose: undoDelay,
    message: (
      <Group justify="space-between" gap="sm" wrap="nowrap">
        <Text size="sm">{message}</Text>
        <Button
          size="compact-sm"
          variant="subtle"
          onClick={() => {
            notifications.hide(id)
            onUndo()
          }}
        >
          Poništi
        </Button>
      </Group>
    ),
  })
  return id
}

interface DeferredDeleteOptions {
  message: string
  /** Optimistično uklanja stavku iz prikaza (cache upita). */
  apply: () => void
  /** Vraća prikaz kao prije apply (poništavanje ili greška). */
  revert: () => void
  /** Stvarno brisanje na serveru. */
  commit: () => Promise<unknown>
  errorTitle: string
}

// Brisanja koja čekaju; pri zatvaranju stranice šalju se odmah.
const pending = new Set<() => void>()

if (typeof window !== 'undefined') {
  window.addEventListener('pagehide', () => pending.forEach((run) => run()))
}

/**
 * Brisanje s poništavanjem: stavka odmah nestaje iz prikaza, a server je briše
 * tek kad istekne vrijeme za „Poništi”. Ako server odbije, stavka se vraća.
 */
export function deferDelete({ message, apply, revert, commit, errorTitle }: DeferredDeleteOptions) {
  apply()

  const run = () => {
    clearTimeout(timer)
    pending.delete(run)
    commit().catch((error: unknown) => {
      revert()
      notifications.show({ color: 'red', title: errorTitle, message: errorMessage(error) })
    })
  }
  const timer = setTimeout(run, undoDelay)
  pending.add(run)

  showUndo({
    message,
    onUndo: () => {
      clearTimeout(timer)
      pending.delete(run)
      revert()
    },
  })
}
