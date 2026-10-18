import { AgencyForm } from '@/features/agencies/AgencyForm'
import { useStartBlank } from './queries'

interface BlankSetupFormProps {
  onCreated: () => void
  onCancel: () => void
}

/** Početak od nule: podaci prvog prijevoznika (GTFS agency). */
export function BlankSetupForm({ onCreated, onCancel }: BlankSetupFormProps) {
  const startBlank = useStartBlank()

  return (
    <AgencyForm
      submitLabel="Stvori prijevoznika"
      cancelLabel="Natrag"
      onSubmit={async (body) => {
        await startBlank.mutateAsync(body)
        onCreated()
      }}
      onCancel={onCancel}
    />
  )
}
