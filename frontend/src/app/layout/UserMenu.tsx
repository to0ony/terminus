import { Avatar, Group, Menu, Text, UnstyledButton, useMantineColorScheme } from '@mantine/core'
import { IconCheck, IconChevronDown, IconLogout, IconSettings } from '@tabler/icons-react'
import { Link } from 'react-router'
import { colorSchemes } from '@/features/appearance/colorSchemes'
import { useCurrentUser, useLogout } from '@/features/auth/queries'

/** Prijavljeni administrator: tema, postavke i odjava. */
export function UserMenu() {
  const { data: user } = useCurrentUser()
  const logout = useLogout()
  const { colorScheme, setColorScheme } = useMantineColorScheme()

  if (!user) {
    return null
  }

  return (
    <Menu position="bottom-end" width={220}>
      <Menu.Target>
        <UnstyledButton aria-label="Korisnički izbornik">
          <Group gap="xs" wrap="nowrap">
            <Avatar size="sm" radius="xl" color="initials" name={user.email} />
            <Text size="sm" visibleFrom="xs" truncate maw={200}>
              {user.email}
            </Text>
            <IconChevronDown size={14} />
          </Group>
        </UnstyledButton>
      </Menu.Target>
      <Menu.Dropdown>
        <Menu.Label>Tema</Menu.Label>
        {colorSchemes.map(({ value, label, icon: Icon }) => (
          <Menu.Item
            key={value}
            leftSection={<Icon size={16} />}
            rightSection={value === colorScheme ? <IconCheck size={16} /> : null}
            onClick={() => setColorScheme(value)}
          >
            {label}
          </Menu.Item>
        ))}
        <Menu.Divider />
        <Menu.Item component={Link} to="/settings" leftSection={<IconSettings size={16} />}>
          Postavke
        </Menu.Item>
        <Menu.Divider />
        <Menu.Item
          color="red"
          leftSection={<IconLogout size={16} />}
          onClick={() => logout.mutate()}
          disabled={logout.isPending}
        >
          Odjava
        </Menu.Item>
      </Menu.Dropdown>
    </Menu>
  )
}
