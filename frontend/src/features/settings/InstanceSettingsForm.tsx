import {
  ActionIcon,
  Alert,
  Box,
  Button,
  ColorInput,
  Code,
  CopyButton,
  Group,
  Image,
  SimpleGrid,
  Stack,
  Switch,
  Text,
  TextInput,
  Title,
  Tooltip,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { zodResolver } from '@hookform/resolvers/zod'
import { IconCheck, IconCopy, IconExternalLink } from '@tabler/icons-react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { z } from 'zod'
import { ApiError, errorMessage } from '@/api/errors'
import { useAgencies } from '@/features/agencies/queries'
import { contrastRatio, toHex } from '@/lib/color'
import { isHttpUrl } from '@/lib/url'
import { type Settings, useSaveSettings } from './queries'

const color = z
  .string()
  .trim()
  .refine((value) => value === '' || toHex(value) !== null, 'Upiši boju kao #RRGGBB.')

const schema = z.object({
  displayName: z.string().trim().max(100, 'Naziv smije imati najviše 100 znakova.'),
  primaryColor: color,
  primaryTextColor: color,
  publicUrl: z
    .string()
    .trim()
    .refine((value) => value === '' || isHttpUrl(value), 'Upiši ispravnu adresu (https://…).'),
  publicGtfsEnabled: z.boolean(),
  publicMapEnabled: z.boolean(),
})

type SettingsForm = z.infer<typeof schema>
type FieldName = keyof SettingsForm

const swatches = ['#1565C0', '#2E7D32', '#C62828', '#6A1B9A', '#EF6C00', '#00838F', '#37474F']
const defaultPrimary = '1565C0'
const defaultText = 'FFFFFF'
const publicGtfsPath = '/api/public/gtfs'
const publicMapPath = '/karta'

/** Naziv i boje javnih stranica, javna adresa i objava GTFS-a; sprema se jednim PUT-om. */
export function InstanceSettingsForm({ settings }: { settings: Settings }) {
  const save = useSaveSettings()
  const agencies = useAgencies()

  const {
    register,
    control,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isDirty },
  } = useForm<SettingsForm>({
    resolver: zodResolver(schema),
    defaultValues: toForm(settings),
  })

  const values = useWatch({ control })

  const onSubmit = handleSubmit((form) =>
    save.mutate(
      {
        displayName: form.displayName || null,
        primaryColor: toHex(form.primaryColor),
        primaryTextColor: toHex(form.primaryTextColor),
        publicUrl: form.publicUrl || null,
        publicGtfsEnabled: form.publicGtfsEnabled,
        publicMapEnabled: form.publicMapEnabled,
      },
      {
        onSuccess: (saved) => {
          reset(toForm(saved))
          notifications.show({ color: 'green', message: 'Postavke su spremljene.' })
        },
        onError: (error) => {
          if (error instanceof ApiError && error.status === 400) {
            for (const [field, messages] of Object.entries(error.fieldErrors)) {
              if (field in schema.shape) {
                setError(field as FieldName, { message: messages[0] })
              }
            }
          }
        },
      },
    ),
  )

  const formError =
    save.error && !(save.error instanceof ApiError && save.error.status === 400)
      ? errorMessage(save.error)
      : null

  const primary = toHex(values.primaryColor ?? '') ?? defaultPrimary
  const text = toHex(values.primaryTextColor ?? '') ?? defaultText
  const contrast = contrastRatio(primary, text)
  const fallbackName = agencies.data?.[0]?.name
  const shownName = values.displayName?.trim() || fallbackName || 'Terminus'
  const baseUrl = (values.publicUrl?.trim() || window.location.origin).replace(/\/+$/, '')
  const gtfsUrl = `${baseUrl}${publicGtfsPath}`
  const mapUrl = `${baseUrl}${publicMapPath}`

  return (
    <form onSubmit={onSubmit} noValidate>
      <Stack gap="lg">
        {formError && (
          <Alert color="red" variant="light">
            {formError}
          </Alert>
        )}

        <Stack gap="md">
          <div>
            <Title order={3}>Javne stranice</Title>
            <Text size="sm" c="dimmed">
              Naziv i boje na zaslonima stanica i u PDF voznim redovima.
            </Text>
          </div>

          <TextInput
            label="Naziv"
            description="Bez njega se koristi naziv prijevoznika."
            placeholder={fallbackName}
            error={errors.displayName?.message}
            {...register('displayName')}
          />

          <SimpleGrid cols={{ base: 1, xs: 2 }}>
            <Controller
              control={control}
              name="primaryColor"
              render={({ field }) => (
                <ColorInput
                  label="Glavna boja"
                  placeholder={`#${defaultPrimary}`}
                  format="hex"
                  swatches={swatches}
                  error={errors.primaryColor?.message}
                  {...field}
                />
              )}
            />
            <Controller
              control={control}
              name="primaryTextColor"
              render={({ field }) => (
                <ColorInput
                  label="Boja teksta na glavnoj boji"
                  placeholder={`#${defaultText}`}
                  format="hex"
                  swatches={['#FFFFFF', '#000000']}
                  error={errors.primaryTextColor?.message}
                  {...field}
                />
              )}
            />
          </SimpleGrid>

          <Box
            p="md"
            style={{
              background: `#${primary}`,
              color: `#${text}`,
              borderRadius: 'var(--mantine-radius-md)',
            }}
          >
            <Group gap="sm" wrap="nowrap">
              {settings.logoUrl && <Image src={settings.logoUrl} alt="" h={32} w="auto" />}
              <div>
                <Text fw={700} size="lg" c="inherit">
                  {shownName}
                </Text>
                <Text size="sm" c="inherit" opacity={0.85}>
                  Polasci sa stanice · pregled
                </Text>
              </div>
            </Group>
          </Box>
          {contrast < 4.5 && (
            <Text size="sm" c="orange">
              Kontrast teksta i pozadine je slab ({contrast.toFixed(1).replace('.', ',')}:1). Za
              čitljivost treba barem 4,5:1.
            </Text>
          )}
        </Stack>

        <Stack gap="md">
          <div>
            <Title order={3}>Javni pristup</Title>
            <Text size="sm" c="dimmed">
              Javna adresa se koristi u QR kodovima na PDF voznim redovima.
            </Text>
          </div>

          <TextInput
            label="Javna adresa"
            placeholder="https://voznired.primjer.hr"
            error={errors.publicUrl?.message}
            {...register('publicUrl')}
          />

          <Controller
            control={control}
            name="publicGtfsEnabled"
            render={({ field: { value, onChange, ...field } }) => (
              <Switch
                label="Objavi GTFS feed"
                description="Feed je dostupan bez prijave, npr. za Google Maps i aplikacije."
                checked={value}
                onChange={(event) => onChange(event.currentTarget.checked)}
                {...field}
              />
            )}
          />
          {values.publicGtfsEnabled && (
            <Group gap="xs" wrap="nowrap">
              <Code style={{ overflowWrap: 'anywhere' }}>{gtfsUrl}</Code>
              <CopyButton value={gtfsUrl}>
                {({ copied, copy }) => (
                  <Tooltip label={copied ? 'Kopirano' : 'Kopiraj'}>
                    <ActionIcon variant="subtle" onClick={copy} aria-label="Kopiraj adresu feeda">
                      {copied ? <IconCheck size={16} /> : <IconCopy size={16} />}
                    </ActionIcon>
                  </Tooltip>
                )}
              </CopyButton>
            </Group>
          )}

          <Controller
            control={control}
            name="publicMapEnabled"
            render={({ field: { value, onChange, ...field } }) => (
              <Switch
                label="Objavi kartu uživo"
                description="Karta sa stanicama, polascima i vozilima prema voznom redu, bez prijave (bez simulacije)."
                checked={value}
                onChange={(event) => onChange(event.currentTarget.checked)}
                {...field}
              />
            )}
          />
          {values.publicMapEnabled && (
            <Group gap="xs" wrap="nowrap">
              <Code style={{ overflowWrap: 'anywhere' }}>{mapUrl}</Code>
              <CopyButton value={mapUrl}>
                {({ copied, copy }) => (
                  <Tooltip label={copied ? 'Kopirano' : 'Kopiraj'}>
                    <ActionIcon variant="subtle" onClick={copy} aria-label="Kopiraj adresu karte">
                      {copied ? <IconCheck size={16} /> : <IconCopy size={16} />}
                    </ActionIcon>
                  </Tooltip>
                )}
              </CopyButton>
              <Tooltip label="Otvori">
                <ActionIcon
                  variant="subtle"
                  component="a"
                  href={publicMapPath}
                  target="_blank"
                  aria-label="Otvori javnu kartu"
                >
                  <IconExternalLink size={16} />
                </ActionIcon>
              </Tooltip>
            </Group>
          )}
        </Stack>

        <Group justify="flex-end">
          <Button variant="default" onClick={() => reset()} disabled={!isDirty || save.isPending}>
            Odustani
          </Button>
          <Button type="submit" loading={save.isPending} disabled={!isDirty}>
            Spremi
          </Button>
        </Group>
      </Stack>
    </form>
  )
}

function toForm(settings: Settings): SettingsForm {
  return {
    displayName: settings.displayName ?? '',
    primaryColor: settings.primaryColor ? `#${settings.primaryColor}` : '',
    primaryTextColor: settings.primaryTextColor ? `#${settings.primaryTextColor}` : '',
    publicUrl: settings.publicUrl ?? '',
    publicGtfsEnabled: settings.publicGtfsEnabled,
    publicMapEnabled: settings.publicMapEnabled,
  }
}
