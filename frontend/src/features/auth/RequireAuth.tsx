import { Navigate, Outlet, useLocation } from 'react-router'
import { PageLoader } from '@/components/PageLoader'
import { useCurrentUser } from './queries'

/** Rute samo za prijavljene; ostale šalje na /login i pamti kamo su išle. */
export function RequireAuth() {
  const { data: user, isPending } = useCurrentUser()
  const location = useLocation()

  if (isPending) {
    return <PageLoader />
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return <Outlet />
}
