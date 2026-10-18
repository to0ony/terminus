import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type Variant = components['schemas']['VariantResponse']
export type VariantRequest = components['schemas']['VariantRequest']

export const variantKeys = {
  route: (routeId: string) => ['routes', routeId, 'variants'] as const,
  detail: (id: string) => ['variants', id] as const,
}

/** Varijante linije po smjeru, glavna prva. */
export function useRouteVariants(routeId: string) {
  return useQuery({
    queryKey: variantKeys.route(routeId),
    queryFn: async () =>
      unwrap(await api.GET('/api/routes/{routeId}/variants', { params: { path: { routeId } } })),
  })
}

export function useVariant(id: string, enabled = true) {
  return useQuery({
    queryKey: variantKeys.detail(id),
    queryFn: async () => unwrap(await api.GET('/api/variants/{id}', { params: { path: { id } } })),
    enabled,
  })
}

/** Trasa sa svim točkama (za varijantu koja je ima). */
export function useShape(id: string | null) {
  return useQuery({
    queryKey: ['shapes', id],
    queryFn: async () =>
      unwrap(await api.GET('/api/shapes/{id}', { params: { path: { id: id! } } })),
    enabled: id !== null,
    staleTime: Infinity,
  })
}

/** Nova trasa iz točaka [dužina, širina]; Id ide u varijantu. */
export async function createShape(code: string, points: [number, number][]) {
  return unwrap(
    await api.POST('/api/shapes', {
      body: {
        code,
        points: points.map(([longitude, latitude]) => ({ latitude, longitude })),
      },
    }),
  )
}

/** Nakon izmjene varijante osvježava se sve o liniji (grafikon, popis varijanti). */
function useVariantMutation<T, R>(routeId: string, request: (body: T) => Promise<R>) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: request,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['routes', routeId] }),
    meta: { handlesErrors: true },
  })
}

export function useCreateVariant(routeId: string) {
  return useVariantMutation(routeId, async (body: VariantRequest) =>
    unwrap(
      await api.POST('/api/routes/{routeId}/variants', { params: { path: { routeId } }, body }),
    ),
  )
}

export function useUpdateVariant(routeId: string, id: string) {
  return useVariantMutation(routeId, async (body: VariantRequest) =>
    unwrap(await api.PUT('/api/variants/{id}', { params: { path: { id } }, body })),
  )
}

export function useDeleteVariant(routeId: string) {
  return useVariantMutation(routeId, async (id: string) =>
    unwrap(await api.DELETE('/api/variants/{id}', { params: { path: { id } } })),
  )
}

/** Varijante iz postojećih vožnji linije (samo za liniju bez varijanti). */
export function useDeriveVariants(routeId: string) {
  return useVariantMutation(routeId, async () =>
    unwrap(
      await api.POST('/api/routes/{routeId}/variants/derive', { params: { path: { routeId } } }),
    ),
  )
}

/**
 * Smjerovi linije za odabir, prema glavnoj varijanti smjera: natpis smjera, a
 * bez njega zadnja stanica.
 */
export function variantDirections(variants: Variant[]): { value: string; label: string }[] {
  const main = new Map<number, Variant>()
  for (const variant of variants) {
    if (variant.directionId !== null && !main.has(variant.directionId)) {
      main.set(variant.directionId, variant)
    }
  }
  return [...main.entries()]
    .sort(([a], [b]) => a - b)
    .map(([direction, variant]) => ({
      value: String(direction),
      label: `→ ${variant.headsign ?? variant.stops.at(-1)?.name ?? `Smjer ${direction}`}`,
    }))
}
