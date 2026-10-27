import { Button, Checkbox, Menu, Stack } from '@mantine/core'
import { IconColumns3 } from '@tabler/icons-react'
import type { ColumnsState } from './useColumns'

/** Izbornik „Stupci”: kvačice za prikaz stupaca i povratak na zadano. */
export function ColumnsMenu<T>({ columns }: { columns: ColumnsState<T> }) {
  return (
    <Menu position="bottom-end" closeOnItemClick={false} width={240}>
      <Menu.Target>
        <Button variant="default" leftSection={<IconColumns3 size={16} />}>
          Stupci
        </Button>
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Label>Prikaži stupce</Menu.Label>
        <Stack gap={8} px="sm" py={6}>
          {columns.all.map((column) => (
            <Checkbox
              key={column.id}
              size="sm"
              label={column.header}
              checked={columns.isVisible(column)}
              disabled={column.alwaysVisible}
              onChange={(event) => columns.setVisible(column.id, event.currentTarget.checked)}
            />
          ))}
        </Stack>
        <Menu.Divider />
        <Menu.Item onClick={columns.reset}>Zadani stupci</Menu.Item>
      </Menu.Dropdown>
    </Menu>
  )
}
