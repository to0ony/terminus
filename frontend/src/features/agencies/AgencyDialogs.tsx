import { Alert, Button, Group, Modal, Select, Stack, Text } from '@mantine/core'
import { useState } from 'react'
import { errorMessage } from '@/api/errors'
import { formatNumber } from '@/lib/format'
import { AgencyForm } from './AgencyForm'
import { type Agency, useDeleteAgency, useSaveAgency } from './queries'

interface AgencyDialogProps {
  /** Prijevoznik za izmjenu; bez njega nov. */
  agency?: Agency
  /** Zona novog prijevoznika: ista kao kod postojećih. */
  defaultTimezone?: string
  opened: boolean
  onClose: () => void
}

/** Nov prijevoznik ili izmjena postojećeg. */
export function AgencyDialog({ agency, defaultTimezone, opened, onClose }: AgencyDialogProps) {
  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title={agency ? 'Uredi prijevoznika' : 'Novi prijevoznik'}
      size="lg"
    >
      {opened && (
        <AgencyFormBody agency={agency} defaultTimezone={defaultTimezone} onClose={onClose} />
      )}
    </Modal>
  )
}

function AgencyFormBody({
  agency,
  defaultTimezone,
  onClose,
}: {
  agency?: Agency
  defaultTimezone?: string
  onClose: () => void
}) {
  const save = useSaveAgency(agency?.id)

  return (
    <AgencyForm
      agency={agency}
      defaultTimezone={defaultTimezone}
      submitLabel={agency ? 'Spremi' : 'Dodaj prijevoznika'}
      onSubmit={async (body) => {
        await save.mutateAsync(body)
        onClose()
      }}
      onCancel={onClose}
    />
  )
}

interface DeleteAgencyDialogProps {
  agency: Agency | null
  /** Broj linija prijevoznika koji se briše. */
  routeCount: number
  agencies: Agency[]
  onClose: () => void
}

/**
 * Brisanje prijevoznika. Ako ima linija, preuzima ih drugi prijevoznik (npr. kad
 * linije u konzorciju prijeđu na drugu tvrtku), zajedno s cijenama karata.
 */
export function DeleteAgencyDialog({
  agency,
  routeCount,
  agencies,
  onClose,
}: DeleteAgencyDialogProps) {
  return (
    <Modal opened={agency !== null} onClose={onClose} title="Obriši prijevoznika">
      {agency && (
        <DeleteAgencyBody
          agency={agency}
          routeCount={routeCount}
          others={agencies.filter((a) => a.id !== agency.id)}
          onClose={onClose}
        />
      )}
    </Modal>
  )
}

function DeleteAgencyBody({
  agency,
  routeCount,
  others,
  onClose,
}: {
  agency: Agency
  routeCount: number
  others: Agency[]
  onClose: () => void
}) {
  const remove = useDeleteAgency()
  const [target, setTarget] = useState<string | null>(others.length === 1 ? others[0].id : null)
  const needsTarget = routeCount > 0

  const submit = () =>
    remove.mutate(
      { id: agency.id, moveRoutesTo: needsTarget ? (target ?? undefined) : undefined },
      { onSuccess: onClose },
    )

  return (
    <Stack gap="md">
      {needsTarget ? (
        <>
          <Text size="sm">
            „{agency.name}” vozi {formatNumber(routeCount)} {routeCount === 1 ? 'liniju' : 'linija'}
            . Odaberi prijevoznika koji ih preuzima; vožnje i vozni red ostaju kakvi jesu.
          </Text>
          <Select
            label="Linije preuzima"
            placeholder="Odaberi prijevoznika"
            data={others.map((a) => ({ value: a.id, label: a.name }))}
            value={target}
            onChange={(value) => setTarget(value === null ? null : String(value))}
            allowDeselect={false}
            data-autofocus
          />
        </>
      ) : (
        <Text size="sm">Prijevoznik „{agency.name}” bit će obrisan. Nema nijednu liniju.</Text>
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
        <Button
          color="red"
          onClick={submit}
          loading={remove.isPending}
          disabled={needsTarget && !target}
        >
          Obriši
        </Button>
      </Group>
    </Stack>
  )
}
