import {
  Center,
  Input,
  Radio,
  SegmentedControl,
  SimpleGrid,
  Stack,
  Text,
  Title,
  useMantineColorScheme,
  type MantineColorScheme,
} from '@mantine/core'
import { mapStyles, type MapStyle, useMapStyle } from '@/components/map/mapStyles'
import { colorSchemes } from './colorSchemes'
import { type Density, densities, useDensity } from './density'

/** Tema sučelja, gustoća prikaza i podloga karte; vrijede samo za ovaj preglednik. */
export function AppearanceSettings() {
  const { colorScheme, setColorScheme } = useMantineColorScheme()
  const [mapStyle, setMapStyle] = useMapStyle()
  const [density, setDensity] = useDensity()

  return (
    <Stack gap="md">
      <div>
        <Title order={3}>Izgled sučelja</Title>
        <Text size="sm" c="dimmed">
          Pamti se u ovom pregledniku i ne mijenja javne stranice ni PDF.
        </Text>
      </div>

      <Input.Wrapper label="Tema">
        <SegmentedControl
          fullWidth
          value={colorScheme}
          onChange={(value) => setColorScheme(value as MantineColorScheme)}
          data={colorSchemes.map(({ value, label, icon: Icon }) => ({
            value,
            label: (
              <Center style={{ gap: 8 }}>
                <Icon size={16} />
                <span>{label}</span>
              </Center>
            ),
          }))}
        />
      </Input.Wrapper>

      <Input.Wrapper
        label="Gustoća prikaza"
        description="Zbijeno smanjuje razmak redaka tablica, pa ih na zaslon stane više."
      >
        <SegmentedControl
          fullWidth
          mt={4}
          value={density}
          onChange={(value) => setDensity(value as Density)}
          data={densities.map(({ value, label, icon: Icon }) => ({
            value,
            label: (
              <Center style={{ gap: 8 }}>
                <Icon size={16} />
                <span>{label}</span>
              </Center>
            ),
          }))}
        />
      </Input.Wrapper>

      <Radio.Group
        label="Podloga karte"
        description="Može se promijeniti i gumbom u donjem lijevom kutu karte."
        value={mapStyle}
        onChange={(value) => setMapStyle(value as MapStyle)}
      >
        <SimpleGrid cols={{ base: 1, xs: 2 }} mt="xs" spacing="sm">
          {mapStyles.map((option) => (
            <Radio
              key={option.value}
              value={option.value}
              label={option.label}
              description={option.description}
            />
          ))}
        </SimpleGrid>
      </Radio.Group>
    </Stack>
  )
}
