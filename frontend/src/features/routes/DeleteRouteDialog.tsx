import { Alert, Button, Group, Modal, Stack, Text, TextInput } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { errorMessage } from '@/api/errors'
import { formatNumber } from '@/lib/format'
import { type Route, useDeleteRoute } from './queries'

interface DeleteRouteDialogProps {
  route: Route
  opened: boolean
  onClose: () => void
  variantCount: number
  tripCount: number
}

/**
 * Brisanje linije s varijantama i vožnjama. Liniju s vožnjama treba potvrditi
 * upisom njezine oznake, jer se brisanje ne može poništiti.
 */
export function DeleteRouteDialog({ route, opened, onClose, ...counts }: DeleteRouteDialogProps) {
  return (
    <Modal opened={opened} onClose={onClose} title="Obriši liniju" size="md">
      {opened && <DeleteRouteForm route={route} onClose={onClose} {...counts} />}
    </Modal>
  )
}

function DeleteRouteForm({
  route,
  onClose,
  variantCount,
  tripCount,
}: Omit<DeleteRouteDialogProps, 'opened'>) {
  const navigate = useNavigate()
  const remove = useDeleteRoute()
  const [typed, setTyped] = useState('')

  const label = route.shortName ?? route.code
  const title = route.longName ? `${label} ${route.longName}` : label
  const confirmed = tripCount === 0 || typed.trim() === label

  const submit = () =>
    remove.mutate(route.id, {
      onSuccess: () => {
        notifications.show({ color: 'green', message: `Linija ${title} je obrisana.` })
        onClose()
        void navigate('/routes')
      },
    })

  return (
    <Stack gap="md">
      <Text size="sm">
        Linija <b>{title}</b> bit će obrisana zajedno s{' '}
        <b>
          {formatNumber(variantCount)}{' '}
          {withCount(variantCount, 'varijantom', 'varijante', 'varijanti')}
        </b>{' '}
        i{' '}
        <b>
          {formatNumber(tripCount)} {withCount(tripCount, 'vožnjom', 'vožnje', 'vožnji')}
        </b>{' '}
        u svim tipovima dana. Ovo se ne može poništiti.
      </Text>
      <Text size="sm" c="dimmed">
        Ako liniju samo privremeno ne voziš, označi je neaktivnom: ne ide u izvoz ni na zaslone, a
        podaci ostaju.
      </Text>
      {tripCount > 0 && (
        <TextInput
          label={`Za potvrdu upiši oznaku linije: ${label}`}
          value={typed}
          onChange={(event) => setTyped(event.currentTarget.value)}
          data-autofocus
        />
      )}
      {remove.isError && (
        <Alert color="red" variant="light">
          {errorMessage(remove.error)}
        </Alert>
      )}
      <Group justify="flex-end">
        <Button variant="default" onClick={onClose}>
          Odustani
        </Button>
        <Button color="red" onClick={submit} disabled={!confirmed} loading={remove.isPending}>
          Obriši liniju
        </Button>
      </Group>
    </Stack>
  )
}

/** Oblik imenice uz broj ("s 1 vožnjom", "s 3 vožnje", "s 12 vožnji"). */
function withCount(n: number, one: string, few: string, many: string) {
  const tens = n % 100
  if (tens >= 11 && tens <= 14) {
    return many
  }
  const last = n % 10
  return last === 1 ? one : last >= 2 && last <= 4 ? few : many
}
