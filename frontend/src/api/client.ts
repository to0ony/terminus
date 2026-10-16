import createClient, { type Middleware } from 'openapi-fetch'
import { authKeys } from '@/features/auth/keys'
import { queryClient } from './queryClient'
import type { paths } from './schema'

// Tipovi dolaze iz OpenAPI opisa backenda (`npm run api:generate`).
// API je na istom originu (u razvoju preko Vite proxyja), pa se kolačić za
// prijavu šalje sam.
export const api = createClient<paths>({ baseUrl: '/' })

// Istekla prijava (401 na bilo kojem zahtjevu osim same prijave): trenutni
// korisnik postaje null, a RequireAuth preusmjerava na /login.
const sessionExpired: Middleware = {
  onResponse({ request, response }) {
    if (response.status === 401 && !new URL(request.url).pathname.startsWith('/api/auth/login')) {
      queryClient.setQueryData(authKeys.me, null)
    }
  },
}

api.use(sessionExpired)
