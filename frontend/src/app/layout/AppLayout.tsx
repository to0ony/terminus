import { AppShell, Burger, Group, NavLink, ScrollArea, Tooltip } from '@mantine/core'
import { useDisclosure, useLocalStorage, useMediaQuery } from '@mantine/hooks'
import { IconLayoutSidebarLeftCollapse, IconLayoutSidebarLeftExpand } from '@tabler/icons-react'
import { Link, Outlet, useLocation } from 'react-router'
import { Logo } from '@/components/Logo'
import { navigation } from './navigation'
import { UserMenu } from './UserMenu'

/**
 * Dashboard: zaglavlje, izbornik lijevo (na mobitelu ladica) i sadržaj stranice.
 * Na računalu se izbornik može smanjiti na ikone; izbor se pamti u pregledniku.
 */
export function AppLayout() {
  const [opened, { toggle, close }] = useDisclosure()
  const [minimized, setMinimized] = useLocalStorage({
    key: 'terminus-navbar-minimized',
    defaultValue: false,
  })
  const desktop = useMediaQuery('(min-width: 48em)')
  const { pathname } = useLocation()

  // Na mobitelu je izbornik ladica, uvijek pune širine.
  const compact = minimized && desktop === true

  return (
    <AppShell
      header={{ height: 56 }}
      navbar={{ width: compact ? 64 : 240, breakpoint: 'sm', collapsed: { mobile: !opened } }}
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
            <Tooltip key={item.to} label={item.label} position="right" disabled={!compact}>
              <NavLink
                component={Link}
                to={item.to}
                label={compact ? undefined : item.label}
                aria-label={item.label}
                leftSection={<item.icon size={18} stroke={1.6} />}
                active={isActive(pathname, item.to)}
                onClick={close}
                styles={compact ? compactStyles : undefined}
              />
            </Tooltip>
          ))}
        </AppShell.Section>
        <AppShell.Section visibleFrom="sm">
          <Tooltip label="Proširi izbornik" position="right" disabled={!compact}>
            <NavLink
              component="button"
              label={compact ? undefined : 'Smanji izbornik'}
              aria-label={compact ? 'Proširi izbornik' : 'Smanji izbornik'}
              leftSection={
                compact ? (
                  <IconLayoutSidebarLeftExpand size={18} stroke={1.6} />
                ) : (
                  <IconLayoutSidebarLeftCollapse size={18} stroke={1.6} />
                )
              }
              c="dimmed"
              styles={compact ? compactStyles : undefined}
              onClick={() => setMinimized((value) => !value)}
            />
          </Tooltip>
        </AppShell.Section>
      </AppShell.Navbar>

      <AppShell.Main>
        <Outlet />
      </AppShell.Main>
    </AppShell>
  )
}

/** Smanjeni izbornik: samo ikona, centrirana. */
const compactStyles = {
  root: { justifyContent: 'center' },
  section: { marginInlineEnd: 0 },
  body: { display: 'none' },
}

function isActive(pathname: string, to: string) {
  return to === '/' ? pathname === '/' : pathname === to || pathname.startsWith(`${to}/`)
}
