import { ActionIcon, Menu, Text, Tooltip } from '@mantine/core'
import { IconCheck, IconStack2 } from '@tabler/icons-react'
import { mapStyles, useMapStyle } from './mapStyles'

/** Gumb na karti za izbor podloge. */
export function MapStylePicker() {
  const [style, setStyle] = useMapStyle()

  return (
    <Menu position="top-start" width={230} withinPortal>
      <Menu.Target>
        <Tooltip label="Podloga karte" openDelay={300}>
          <ActionIcon variant="default" size="lg" aria-label="Podloga karte">
            <IconStack2 size={18} />
          </ActionIcon>
        </Tooltip>
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Label>Podloga karte</Menu.Label>
        {mapStyles.map((option) => (
          <Menu.Item
            key={option.value}
            onClick={() => setStyle(option.value)}
            rightSection={option.value === style ? <IconCheck size={16} /> : null}
          >
            <Text size="sm">{option.label}</Text>
            <Text size="xs" c="dimmed">
              {option.description}
            </Text>
          </Menu.Item>
        ))}
      </Menu.Dropdown>
    </Menu>
  )
}
