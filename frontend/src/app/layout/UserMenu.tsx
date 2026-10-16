import { Avatar, Group, Menu, Text, UnstyledButton } from '@mantine/core'
import { IconChevronDown, IconLogout, IconSettings } from '@tabler/icons-react'
import { Link } from 'react-router'
import { useCurrentUser, useLogout } from '@/features/auth/queries'

/** Prijavljeni administrator: postavke i odjava. */
export function UserMenu() {
  const { data: user } = useCurrentUser()
  const logout = useLogout()

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
