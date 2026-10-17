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
import { Controller, useForm } from 'react-hook-form'
import { z } from 'zod'
import { ApiError, errorMessage } from '@/api/errors'
import { isHttpUrl } from '@/lib/url'
import { useStartBlank } from './queries'

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

type BlankForm = z.infer<typeof schema>
type FieldName = keyof BlankForm

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

interface BlankSetupFormProps {
  onCreated: () => void
  onCancel: () => void
}

/** Početak od nule: podaci prvog prijevoznika (GTFS agency). */
export function BlankSetupForm({ onCreated, onCancel }: BlankSetupFormProps) {
  const startBlank = useStartBlank()
  const [advanced, { toggle: toggleAdvanced }] = useDisclosure(false)

  const {
    register,
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<BlankForm>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
      url: '',
      email: '',
      phone: '',
      fareUrl: '',
      code: '',
      timezone: 'Europe/Zagreb',
      lang: 'hr',
    },
  })

  const onSubmit = handleSubmit((values) =>
    startBlank.mutate(
      {
        name: values.name,
        url: values.url,
        email: values.email || null,
        phone: values.phone || null,
        fareUrl: values.fareUrl || null,
        code: values.code || null,
        timezone: values.timezone,
        lang: values.lang,
      },
      {
        onSuccess: onCreated,
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

  const showAdvanced = advanced || Boolean(errors.timezone || errors.lang || errors.code)

  const formError =
    startBlank.error && !(startBlank.error instanceof ApiError && startBlank.error.status === 400)
      ? errorMessage(startBlank.error)
      : null

  return (
    <form onSubmit={onSubmit} noValidate>
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
              description="Ako se ne upiše, izvodi se iz naziva."
              error={errors.code?.message}
              {...register('code')}
            />
          </Stack>
        </Collapse>

        <Group justify="flex-end" mt="sm">
          <Button variant="default" onClick={onCancel} disabled={startBlank.isPending}>
            Natrag
          </Button>
          <Button type="submit" loading={startBlank.isPending}>
            Stvori prijevoznika
          </Button>
        </Group>
      </Stack>
    </form>
  )
}
