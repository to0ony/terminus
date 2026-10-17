/** Datoteka preuzeta s API-ja (blob) s nazivom iz Content-Disposition. */
export interface DownloadedFile {
  blob: Blob
  fileName: string
}

// Content-Disposition: attachment; filename=gtfs-2026-10-17.zip; filename*=UTF-8''…
export function fileNameOf(response: Response): string | null {
  const header = response.headers.get('Content-Disposition') ?? ''
  const encoded = /filename\*=UTF-8''([^;]+)/i.exec(header)
  if (encoded) {
    return decodeURIComponent(encoded[1])
  }
  return /filename="?([^";]+)"?/i.exec(header)?.[1] ?? null
}

/** Sprema datoteku kao preuzimanje preglednika. */
export function saveFile({ blob, fileName }: DownloadedFile) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  // Preglednik je već preuzeo sadržaj; URL više ne treba.
  setTimeout(() => URL.revokeObjectURL(url), 0)
}
