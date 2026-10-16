import {
  queryOptions,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import { authKeys } from './keys'

export type CurrentUser = components['schemas']['CurrentUserResponse']
export type LoginRequest = components['schemas']['LoginRequest']

/** Prijavljeni administrator ili null ako prijave nema (401). */
export const currentUserQuery = queryOptions({
  queryKey: authKeys.me,
  queryFn: async (): Promise<CurrentUser | null> => {
    const result = await api.GET('/api/auth/me')
    return result.response.status === 401 ? null : unwrap(result)
  },
  staleTime: Infinity,
})

export function useCurrentUser() {
  return useQuery(currentUserQuery)
}

function removeNonAuthQueries(queryClient: QueryClient) {
  queryClient.removeQueries({ predicate: (query) => query.queryKey[0] !== authKeys.me[0] })
}

export function useLogin() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: LoginRequest) =>
      unwrap(await api.POST('/api/auth/login', { body: request })),
    onSuccess: (user) => {
      // Podaci prethodne prijave (npr. drugog administratora) ne smiju ostati.
      removeNonAuthQueries(queryClient)
      queryClient.setQueryData(authKeys.me, user)
    },
    meta: { handlesErrors: true },
  })
}

export function useLogout() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async () => unwrap(await api.POST('/api/auth/logout')),
    onSuccess: () => {
      // Ne clear(): RequireAuth prati upit za korisnika, pa on mora ostati u
      // cacheu (s null) da bi preusmjerio na /login. Ostali podaci se brišu.
      queryClient.setQueryData(authKeys.me, null)
      removeNonAuthQueries(queryClient)
    },
  })
}
