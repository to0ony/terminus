import { Navigate, Outlet } from 'react-router'
import { PageLoader } from '@/components/PageLoader'
import { QueryError } from '@/components/QueryError'
import { useSetupStatus } from './queries'

/**
 * Dashboard tek nakon čarobnjaka prvog pokretanja; do tada instanca nema
 * prijevoznika, pa sve rute vode na /setup.
 */
export function RequireSetup() {
  const { data: status, error, refetch } = useSetupStatus()

  if (error) {
    return <QueryError error={error} onRetry={() => refetch()} />
  }

  if (!status) {
    return <PageLoader />
  }

  if (!status.completed) {
    return <Navigate to="/setup" replace />
  }

  return <Outlet />
}
