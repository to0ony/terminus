import { AppShell, Burger, Group, NavLink, ScrollArea } from '@mantine/core'
import { useDisclosure } from '@mantine/hooks'
import { Link, Outlet, useLocation } from 'react-router'
import { Logo } from '@/components/Logo'
import { navigation } from './navigation'
import { UserMenu } from './UserMenu'

/** Dashboard: zaglavlje, izbornik lijevo (na mobitelu ladica) i sadržaj stranice. */
export function AppLayout() {
  const [opened, { toggle, close }] = useDisclosure()
  const { pathname } = useLocation()

  return (
    <AppShell
      header={{ height: 56 }}
      navbar={{ width: 240, breakpoint: 'sm', collapsed: { mobile: !opened } }}
      padding="lg"
    >
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between" wrap="nowrap">
          <Group gap="sm" wrap="nowrap">
            <Burger
              opened={opened}
              onClick={toggle}
              hiddenFrom="sm"
              size="sm"
              aria-label="Izbornik"
            />
            <Link to="/" onClick={close} style={{ display: 'flex' }}>
              <Logo height={18} />
            </Link>
          </Group>
          <UserMenu />
        </Group>
      </AppShell.Header>

      <AppShell.Navbar p="xs">
        <AppShell.Section grow component={ScrollArea}>
          {navigation.map((item) => (
            <NavLink
              key={item.to}
              component={Link}
              to={item.to}
              label={item.label}
              leftSection={<item.icon size={18} stroke={1.6} />}
              active={isActive(pathname, item.to)}
              onClick={close}
            />
          ))}
        </AppShell.Section>
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  )
}

function isActive(pathname: string, to: string) {
  return to === '/' ? pathname === '/' : pathname === to || pathname.startsWith(`${to}/`)
}
