import { Alert, Button, Group, PasswordInput, SimpleGrid, Stack, Text, Title } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { ApiError, errorMessage } from '@/api/errors'
import { useChangePassword } from './queries'

/** Najkraća lozinka koju backend prima (Identity RequiredLength). */
const minPasswordLength = 10

const schema = z
  .object({
    currentPassword: z.string().min(1, 'Upiši trenutnu lozinku.'),
    newPassword: z
      .string()
      .min(minPasswordLength, `Nova lozinka mora imati barem ${minPasswordLength} znakova.`),
    confirmPassword: z.string(),
  })
  .refine((form) => form.newPassword === form.confirmPassword, {
    path: ['confirmPassword'],
    message: 'Lozinke se ne podudaraju.',
  })

type PasswordForm = z.infer<typeof schema>

/** Promjena lozinke prijavljenog administratora. */
export function PasswordForm() {
  const changePassword = useChangePassword()
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm<PasswordForm>({
    resolver: zodResolver(schema),
    defaultValues: { currentPassword: '', newPassword: '', confirmPassword: '' },
  })

  const onSubmit = handleSubmit(({ currentPassword, newPassword }) =>
    changePassword.mutate(
      { currentPassword, newPassword },
      {
        onSuccess: () => {
          reset()
          notifications.show({
            color: 'green',
            message: 'Lozinka je promijenjena. Prijave u drugim preglednicima su odjavljene.',
          })
        },
        onError: (error) => {
          if (error instanceof ApiError && error.status === 400) {
            const { currentPassword: current, newPassword: next } = error.fieldErrors
            if (current) {
              setError('currentPassword', { message: 'Trenutna lozinka nije ispravna.' })
            }
            if (next) {
              setError('newPassword', { message: next[0] })
            }
          }
        },
      },
    ),
  )

  const formError =
    changePassword.error &&
    !(changePassword.error instanceof ApiError && changePassword.error.status === 400)
      ? errorMessage(changePassword.error)
      : null

  return (
    <form onSubmit={onSubmit} noValidate>
      <Stack gap="md">
        <div>
          <Title order={3}>Lozinka</Title>
          <Text size="sm" c="dimmed">
            Nakon promjene ostale prijave tvog računa (drugi preglednici) prestaju vrijediti.
          </Text>
        </div>

        {formError && (
          <Alert color="red" variant="light">
            {formError}
          </Alert>
        )}

        <PasswordInput
          label="Trenutna lozinka"
          autoComplete="current-password"
          error={errors.currentPassword?.message}
          maw={{ xs: 'calc(50% - var(--mantine-spacing-md) / 2)' }}
          {...register('currentPassword')}
        />
        <SimpleGrid cols={{ base: 1, xs: 2 }}>
          <PasswordInput
            label="Nova lozinka"
            autoComplete="new-password"
            error={errors.newPassword?.message}
            {...register('newPassword')}
          />
          <PasswordInput
            label="Ponovi novu lozinku"
            autoComplete="new-password"
            error={errors.confirmPassword?.message}
            {...register('confirmPassword')}
          />
        </SimpleGrid>

        <Group justify="flex-end">
          <Button type="submit" loading={changePassword.isPending}>
            Promijeni lozinku
          </Button>
        </Group>
      </Stack>
    </form>
  )
}
