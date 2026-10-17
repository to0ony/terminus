import { describe, expect, it } from 'vitest'
import { fileNameOf } from './download'

function response(disposition: string | null) {
  return new Response(null, disposition ? { headers: { 'Content-Disposition': disposition } } : {})
}

describe('fileNameOf', () => {
  it('prefers the encoded file name', () => {
    expect(
      fileNameOf(
        response(
          "attachment; filename=vozni-red-106.pdf; filename*=UTF-8''vozni-red-%C5%BEitnjak.pdf",
        ),
      ),
    ).toBe('vozni-red-žitnjak.pdf')
  })

  it('falls back to the plain file name', () => {
    expect(fileNameOf(response('attachment; filename="gtfs.zip"'))).toBe('gtfs.zip')
  })

  it('returns null without the header', () => {
    expect(fileNameOf(response(null))).toBeNull()
  })
})
