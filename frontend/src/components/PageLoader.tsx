import { Center, Loader } from '@mantine/core'

/** Učitavanje cijele stranice (provjera prijave, lazy rute). */
export function PageLoader() {
  return (
    <Center h="100%" mih={200} py="xl">
      <Loader />
    </Center>
  )
}
