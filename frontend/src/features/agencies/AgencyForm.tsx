import {
  Alert,
  Anchor,
  Button,
  Collapse,
  Group,
  Select,
  SimpleGrid,
  Stack,
  TextInput,
} from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'
import { ApiError, errorMessage } from '@/api/errors'
import { isHttpUrl } from '@/lib/url'
import type { Agency } from './queries'

const optionalText = z.string().trim()

const url = (message: string) =>
  z
    .string()
    .trim()
    .refine((value) => value === '' || isHttpUrl(value), message)

const schema = z.object({
  name: z.string().trim().min(1, 'Upiši naziv prijevoznika.'),
  url: url('Upiši ispravnu web adresu (https://…).').refine(
    (value) => value !== '',
    'Upiši web adresu prijevoznika.',
  ),
  email: optionalText.refine(
    (value) => value === '' || z.email().safeParse(value).success,
    'Neispravan e-mail.',
  ),
  phone: optionalText,
  fareUrl: url('Upiši ispravnu web adresu (https://…).'),
  code: optionalText,
  timezone: z.string().min(1, 'Odaberi vremensku zonu.'),
  lang: z.string().min(1, 'Odaberi jezik.'),
})

type AgencyFormValues = z.infer<typeof schema>
type FieldName = keyof AgencyFormValues

/** Podaci prijevoznika (GTFS agency) koje forma šalje; prazna polja su null. */
export interface AgencyBody {
  name: string
  url: string
  email: string | null
  phone: string | null
  fareUrl: string | null
  /** Bez koda backend ga kod stvaranja izvodi iz naziva. */
  code: string | null
  timezone: string
  lang: string
}

const languages = [
  { value: 'hr', label: 'Hrvatski' },
  { value: 'en', label: 'Engleski' },
  { value: 'sl', label: 'Slovenski' },
  { value: 'bs', label: 'Bosanski' },
  { value: 'sr', label: 'Srpski' },
  { value: 'de', label: 'Njemački' },
  { value: 'it', label: 'Talijanski' },
  { value: 'hu', label: 'Mađarski' },
]

const timezones = Intl.supportedValuesOf('timeZone')

interface AgencyFormProps {
  /** Prijevoznik za izmjenu; bez njega nov. */
  agency?: Agency
  /** Zona za novog prijevoznika: svi prijevoznici instance moraju imati istu. */
  defaultTimezone?: string
  submitLabel: string
  cancelLabel?: string
  /** Sprema podatke; greške 400 forma prikazuje uz polja. */
  onSubmit: (body: AgencyBody) => Promise<unknown>
  onCancel: () => void
}

/** Forma prijevoznika: u čarobnjaku prvog pokretanja i na stranici Prijevoznici. */
export function AgencyForm({
  agency,
  defaultTimezone = 'Europe/Zagreb',
  submitLabel,
  cancelLabel = 'Odustani',
  onSubmit,
  onCancel,
}: AgencyFormProps) {
  const [advanced, { toggle: toggleAdvanced }] = useDisclosure(false)
  const [formError, setFormError] = useState<string | null>(null)

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<AgencyFormValues>({
    resolver: zodResolver(
      // Postojeći prijevoznik mora zadržati GTFS oznaku; novi je može dobiti iz naziva.
      agency ? schema.extend({ code: z.string().trim().min(1, 'Upiši GTFS oznaku.') }) : schema,
    ),
    defaultValues: {
      name: agency?.name ?? '',
      url: agency?.url ?? '',
      email: agency?.email ?? '',
      phone: agency?.phone ?? '',
      fareUrl: agency?.fareUrl ?? '',
      code: agency?.code ?? '',
      timezone: agency?.timezone ?? defaultTimezone,
      lang: agency?.lang ?? 'hr',
    },
  })

  const submit = handleSubmit(async (values) => {
    setFormError(null)
    try {
      await onSubmit({
        name: values.name,
        url: values.url,
        email: values.email || null,
        phone: values.phone || null,
        fareUrl: values.fareUrl || null,
        code: values.code || null,
        timezone: values.timezone,
        lang: values.lang,
      })
    } catch (error) {
      const fieldErrors = Object.entries(error instanceof ApiError ? error.fieldErrors : {}).filter(
        ([field]) => field in schema.shape,
      )
      for (const [field, messages] of fieldErrors) {
        setError(field as FieldName, { message: messages[0] })
      }
      if (fieldErrors.length === 0) {
        setFormError(errorMessage(error))
      }
    }
  })

  const showAdvanced = advanced || Boolean(errors.timezone || errors.lang || errors.code)

  return (
    <form onSubmit={submit} noValidate>
      <Stack>
        {formError && (
          <Alert color="red" variant="light">
            {formError}
          </Alert>
        )}

        <TextInput
          label="Naziv prijevoznika"
          placeholder="npr. Gradski prijevoz putnika"
          withAsterisk
          data-autofocus
          autoFocus
          error={errors.name?.message}
          {...register('name')}
        />
        <TextInput
          label="Web adresa"
          placeholder="https://"
          withAsterisk
          error={errors.url?.message}
          {...register('url')}
        />
        <SimpleGrid cols={{ base: 1, xs: 2 }}>
          <TextInput
            label="E-mail"
            type="email"
            error={errors.email?.message}
            {...register('email')}
          />
          <TextInput label="Telefon" error={errors.phone?.message} {...register('phone')} />
        </SimpleGrid>
        <TextInput
          label="Web adresa s cijenama karata"
          placeholder="https://"
          error={errors.fareUrl?.message}
          {...register('fareUrl')}
        />

        <Anchor component="button" type="button" size="sm" onClick={toggleAdvanced} w="fit-content">
          {showAdvanced ? 'Sakrij napredne postavke' : 'Napredne postavke'}
        </Anchor>
        <Collapse expanded={showAdvanced}>
          <Stack>
            <SimpleGrid cols={{ base: 1, xs: 2 }}>
              <Controller
                control={control}
                name="timezone"
                render={({ field }) => (
                  <Select
                    label="Vremenska zona"
                    description="Ista za sve prijevoznike"
                    data={timezones}
                    searchable
                    allowDeselect={false}
                    error={errors.timezone?.message}
                    {...field}
                  />
                )}
              />
              <Controller
                control={control}
                name="lang"
                render={({ field }) => (
                  <Select
                    label="Jezik"
                    description="Jezik naziva u feedu"
                    data={languages}
                    allowDeselect={false}
                    error={errors.lang?.message}
                    {...field}
                  />
                )}
              />
            </SimpleGrid>
            <TextInput
              label="GTFS oznaka (agency_id)"
              description={
                agency
                  ? 'Promijeni samo ako je nitko izvan Terminusa ne koristi.'
                  : 'Ako se ne upiše, izvodi se iz naziva.'
              }
              error={errors.code?.message}
              {...register('code')}
            />
          </Stack>
        </Collapse>

        <Group justify="flex-end" mt="sm">
          <Button variant="default" onClick={onCancel} disabled={isSubmitting}>
            {cancelLabel}
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {submitLabel}
          </Button>
        </Group>
      </Stack>
    </form>
  )
}
