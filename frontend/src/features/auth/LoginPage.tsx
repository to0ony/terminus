import {
  Alert,
  Button,
  Center,
  Checkbox,
  Paper,
  PasswordInput,
  Stack,
  TextInput,
  Title,
} from '@mantine/core'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { Navigate, useLocation, useNavigate, type Location } from 'react-router'
import { z } from 'zod'
import { ApiError, errorMessage } from '@/api/errors'
import { Logo } from '@/components/Logo'
import { PageLoader } from '@/components/PageLoader'
import { useCurrentUser, useLogin } from './queries'

const schema = z.object({
  email: z
    .string()
    .trim()
    .min(1, { error: 'Upiši e-mail.', abort: true })
    .refine((value) => z.email().safeParse(value).success, 'Neispravan e-mail.'),
  password: z.string().min(1, 'Upiši lozinku.'),
  rememberMe: z.boolean(),
})

type LoginForm = z.infer<typeof schema>

export function LoginPage() {
  const { data: user, isPending } = useCurrentUser()
  const login = useLogin()
  const navigate = useNavigate()
  const location = useLocation()
  const from = (location.state as { from?: Location } | null)?.from?.pathname ?? '/'

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<LoginForm>({
    resolver: zodResolver(schema),
    // Bez "zapamti me" prijava traje do zatvaranja preglednika (sigurnije na dijeljenim računalima).
    defaultValues: { email: '', password: '', rememberMe: false },
  })

  if (isPending) {
    return <PageLoader />
  }

  if (user && !login.isPending) {
    return <Navigate to={from} replace />
  }

  const onSubmit = handleSubmit((values) =>
    login.mutate(values, {
      onSuccess: () => navigate(from, { replace: true }),
      onError: (error) => {
        if (error instanceof ApiError && error.status === 400) {
          for (const [field, messages] of Object.entries(error.fieldErrors)) {
            if (field === 'email' || field === 'password') {
              setError(field, { message: messages[0] })
            }
          }
        }
      },
    }),
  )

  const formError =
    login.error && !(login.error instanceof ApiError && login.error.status === 400)
      ? errorMessage(login.error)
      : null

  return (
    <Center mih="100vh" p="md">
      <Paper withBorder shadow="sm" p="xl" w="100%" maw={400}>
        <form onSubmit={onSubmit} noValidate>
          <Stack>
            <Stack align="center" gap="xs">
              <Logo height={28} />
              <Title order={2} size="h3">
                Prijava
              </Title>
            </Stack>

            {formError && (
              <Alert color="red" variant="light">
                {formError}
              </Alert>
            )}

            <TextInput
              label="E-mail"
              type="email"
              autoComplete="username"
              autoFocus
              error={errors.email?.message}
              {...register('email')}
            />
            <PasswordInput
              label="Lozinka"
              autoComplete="current-password"
              error={errors.password?.message}
              {...register('password')}
            />
            <Checkbox label="Zapamti me na ovom uređaju" {...register('rememberMe')} />

            <Button type="submit" loading={login.isPending} fullWidth>
              Prijavi se
            </Button>
          </Stack>
        </form>
      </Paper>
    </Center>
  )
}
