import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'

export type Settings = components['schemas']['SettingsResponse']
export type SettingsRequest = components['schemas']['SettingsRequest']
export type ChangePasswordRequest = components['schemas']['ChangePasswordRequest']

/** Najveći logo koji backend prima (SettingsService.MaxLogoBytes). */
export const maxLogoBytes = 512 * 1024

export const settingsKeys = {
  all: ['settings'] as const,
}

export function useSettings() {
  return useQuery({
    queryKey: settingsKeys.all,
    queryFn: async () => unwrap(await api.GET('/api/settings')),
  })
}

export function useSaveSettings() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (request: SettingsRequest) =>
      unwrap(await api.PUT('/api/settings', { body: request })),
    onSuccess: (settings) => queryClient.setQueryData(settingsKeys.all, settings),
    meta: { handlesErrors: true },
  })
}

export function useUploadLogo() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async (file: File) => {
      const form = new FormData()
      form.append('file', file)
      return unwrap(await api.PUT('/api/settings/logo', { body: {}, bodySerializer: () => form }))
    },
    onSuccess: (settings) => queryClient.setQueryData(settingsKeys.all, settings),
    meta: { handlesErrors: true },
  })
}

export function useDeleteLogo() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: async () => unwrap(await api.DELETE('/api/settings/logo')),
    onSuccess: () =>
      queryClient.setQueryData<Settings>(settingsKeys.all, (settings) =>
        settings ? { ...settings, logoUrl: null } : settings,
      ),
  })
}

/** Promjena vlastite lozinke; ostale prijave tog administratora prestaju vrijediti. */
export function useChangePassword() {
  return useMutation({
    mutationFn: async (request: ChangePasswordRequest) =>
      unwrap(await api.PUT('/api/auth/password', { body: request })),
    meta: { handlesErrors: true },
  })
}
