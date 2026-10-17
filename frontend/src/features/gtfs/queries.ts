import { useMutation } from '@tanstack/react-query'
import { api } from '@/api/client'
import { unwrap } from '@/api/errors'
import type { components } from '@/api/schema'
import { type DownloadedFile, fileNameOf } from '@/lib/download'

export type GtfsImportReport = components['schemas']['GtfsImportReportResponse']
export type GtfsImportIssue = components['schemas']['GtfsImportIssueResponse']

/** Najveći ZIP koji backend prima (GtfsImportController.MaxUploadBytes). */
export const maxGtfsUploadBytes = 200 * 1024 * 1024

function fileBody(file: File) {
  const form = new FormData()
  form.append('file', file)
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
 * Uvoz feeda. Feed s greškama (422) nije greška zahtjeva: vraća se izvještaj
 * s `imported: false`, isto kao kod pregleda.
 */
export function useImportGtfs() {
  return useMutation({
    mutationFn: async (file: File): Promise<GtfsImportReport> => {
      const result = await api.POST('/api/gtfs/import', fileBody(file))
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
