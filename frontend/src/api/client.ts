import createClient from 'openapi-fetch'
import type { paths } from './schema'

// Tipovi dolaze iz OpenAPI opisa backenda (`npm run api:generate`).
// API je na istom originu (u razvoju preko Vite proxyja), pa se kolačić za
// prijavu šalje sam.
export const api = createClient<paths>({ baseUrl: '/' })
