import { useMutation, useQuery } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import { type DownloadedFile, fileNameOf } from '@/lib/download'

export type GtfsImportReport = components['schemas']['GtfsImportReportResponse']
export type GtfsImportIssue = components['schemas']['GtfsImportIssueResponse']
export type GtfsImportVariant = components['schemas']['GtfsImportVariantResponse']

/** Ima li instanca podataka koje bi uvoz obrisao. */
export function hasExistingData({
  agencies,
  stops,
  routes,
  trips,
  seasons,
}: GtfsImportReport['existing']) {
  return agencies + stops + routes + trips + seasons > 0
}

/** Najveći ZIP koji backend prima (GtfsImportController.MaxUploadBytes). */
export const maxGtfsUploadBytes = 200 * 1024 * 1024

function fileBody(
  file: File,
  options?: { variants: string[]; skipExpired: boolean; createSeason: boolean },
) {
  const form = new FormData()
  form.append('file', file)
  if (options) {
    form.append('variants', JSON.stringify(options.variants))
    form.append('skipExpired', String(options.skipExpired))
    form.append('createSeason', String(options.createSeason))
  }
  return { body: {}, bodySerializer: () => form }
}

/** Provjera feeda i izvještaj, bez upisa. */
export function usePreviewGtfs() {
  return useMutation({
    mutationFn: async (file: File) =>
      unwrap(await api.POST('/api/gtfs/import/preview', fileBody(file))),
    meta: { handlesErrors: true },
  })
}

/**
 * Uvoz feeda, koji zamjenjuje sve podatke voznog reda. Feed s greškama (422) nije
 * greška zahtjeva: vraća se izvještaj s `imported: false`, isto kao kod pregleda.
 */
export function useImportGtfs() {
  return useMutation({
    mutationFn: async ({
      file,
      variants,
      skipExpired,
      createSeason,
    }: {
      file: File
      /** Ključevi odabranih varijanti iz pregleda. */
      variants: string[]
      /** Izostavlja kalendare koji više ne voze i njihove vožnje. */
      skipExpired: boolean
      /** Pretvara kalendare u sezonu. */
      createSeason: boolean
    }): Promise<GtfsImportReport> => {
      const result = await api.POST(
        '/api/gtfs/import',
        fileBody(file, { variants, skipExpired, createSeason }),
      )
      if (result.response.status === 422) {
        return result.error as GtfsImportReport
      }
      return unwrap(result)
    },
    meta: { handlesErrors: true },
  })
}

/**
 * Izvoz feeda kao ZIP. Ako podaci nisu spremni, backend vraća 422, a
 * ApiError.problems kaže što treba ispraviti.
 */
export function useExportGtfs() {
  return useMutation({
    mutationFn: async (): Promise<DownloadedFile> => {
      const result = await api.GET('/api/gtfs', { parseAs: 'blob' })
      const blob = unwrap(result) as unknown as Blob
      return { blob, fileName: fileNameOf(result.response) ?? 'gtfs.zip' }
    },
    meta: { handlesErrors: true },
  })
}

/**
 * Provjere izvoza bez izrade ZIP-a: što bi blokiralo izvoz. Uvijek svježe,
 * jer ih mijenja gotovo svaka izmjena podataka.
 */
export function useGtfsCheck() {
  return useQuery({
    queryKey: ['gtfs', 'check'],
    queryFn: async () => unwrap(await api.GET('/api/gtfs/check')).problems,
    staleTime: 0,
  })
}
