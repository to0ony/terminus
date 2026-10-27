import { Paper, Stack, Title } from '@mantine/core'
import { FormSkeleton } from '@/components/Skeletons'
import { QueryError } from '@/components/QueryError'
import { AppearanceSettings } from '@/features/appearance/AppearanceSettings'
import { InstanceSettingsForm } from './InstanceSettingsForm'
import { LogoSettings } from './LogoSettings'
import { PasswordForm } from './PasswordForm'
import { useSettings } from './queries'

/** Postavke instance (izgled javnih stranica, javni pristup, logo), izgled sučelja i lozinka. */
export function SettingsPage() {
  const settings = useSettings()

  return (
    <Stack gap="lg" maw={760}>
      <Title order={2}>Postavke</Title>

      {settings.isPending ? (
        <FormSkeleton />
      ) : settings.isError ? (
        <QueryError error={settings.error} onRetry={() => void settings.refetch()} />
      ) : (
        <>
          <Paper withBorder p="lg">
            <InstanceSettingsForm settings={settings.data} />
          </Paper>
          <Paper withBorder p="lg">
            <LogoSettings settings={settings.data} />
          </Paper>
        </>
      )}

      <Paper withBorder p="lg">
        <AppearanceSettings />
      </Paper>

      <Paper withBorder p="lg">
        <PasswordForm />
      </Paper>
    </Stack>
  )
}
