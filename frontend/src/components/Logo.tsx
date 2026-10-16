import { Image } from '@mantine/core'

/** Natpis Terminus; u tamnoj temi bijela inačica. */
export function Logo({ height }: { height: number }) {
  return (
    <>
      <Image src="/logo.png" alt="Terminus" h={height} w="auto" darkHidden />
      <Image src="/logo-white.png" alt="Terminus" h={height} w="auto" lightHidden />
    </>
  )
}
