import {
  Alert,
  Button,
  Center,
  FileButton,
  Group,
  Image,
  Paper,
  Stack,
  Text,
  Title,
} from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconPhoto, IconTrash, IconUpload } from '@tabler/icons-react'
import { useState } from 'react'
import { ApiError, errorMessage } from '@/api/errors'
import { formatBytes } from '@/lib/format'
import { maxLogoBytes, type Settings, useDeleteLogo, useUploadLogo } from './queries'

const accept = 'image/png,image/jpeg,image/webp,image/svg+xml'

/** Logo za javne stranice i PDF; sprema se odmah, neovisno o ostalim postavkama. */
export function LogoSettings({ settings }: { settings: Settings }) {
  const upload = useUploadLogo()
  const remove = useDeleteLogo()
  const [tooLarge, setTooLarge] = useState(false)

  const selectFile = (file: File | null) => {
    if (!file) {
      return
    }
    setTooLarge(file.size > maxLogoBytes)
    if (file.size <= maxLogoBytes) {
      upload.mutate(file, {
        onSuccess: () => notifications.show({ color: 'green', message: 'Logo je spremljen.' }),
      })
    }
  }

  const uploadError =
    upload.error instanceof ApiError
      ? (upload.error.fieldErrors.logo?.[0] ??
        upload.error.fieldErrors.file?.[0] ??
        upload.error.message)
      : upload.error && errorMessage(upload.error)

  return (
    <Stack gap="md">
      <div>
        <Title order={3}>Logo</Title>
        <Text size="sm" c="dimmed">
          PNG, JPEG, WebP ili SVG, najviše {formatBytes(maxLogoBytes)}. Najbolje izgleda vodoravni
          logo na prozirnoj pozadini.
        </Text>
      </div>

      <Group gap="lg" align="center">
        <Paper
          withBorder
          w={200}
          h={96}
          p="sm"
          // Siva podloga da se vidi i bijeli logo.
          bg="var(--mantine-color-default-hover)"
        >
          <Center h="100%">
            {settings.logoUrl ? (
              <Image src={settings.logoUrl} alt="Logo" mah={72} maw={176} fit="contain" />
            ) : (
              <IconPhoto size={36} stroke={1.2} color="var(--mantine-color-dimmed)" />
            )}
          </Center>
        </Paper>
        <Group gap="sm">
          <FileButton onChange={selectFile} accept={accept}>
            {(props) => (
              <Button
                {...props}
                variant="default"
                leftSection={<IconUpload size={16} />}
                loading={upload.isPending}
              >
                {settings.logoUrl ? 'Zamijeni' : 'Učitaj logo'}
              </Button>
            )}
          </FileButton>
          {settings.logoUrl && (
            <Button
              variant="subtle"
              color="red"
              leftSection={<IconTrash size={16} />}
              loading={remove.isPending}
              onClick={() => remove.mutate()}
            >
              Ukloni
            </Button>
          )}
        </Group>
      </Group>

      {(tooLarge || uploadError) && (
        <Alert color="red" variant="light">
          {tooLarge ? `Logo smije imati najviše ${formatBytes(maxLogoBytes)}.` : uploadError}
        </Alert>
      )}
    </Stack>
  )
}
