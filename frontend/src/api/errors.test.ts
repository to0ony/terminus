import { describe, expect, it } from 'vitest'
import { ApiError, errorMessage, unwrap } from './errors'

describe('ApiError', () => {
  it('uses the problem detail as the message', () => {
    const error = new ApiError(401, {
      title: 'Prijava nije uspjela.',
      detail: 'Neispravan e-mail.',
    })

    expect(error.message).toBe('Neispravan e-mail.')
  })

  it('falls back to the title, then to a message for the status', () => {
    expect(new ApiError(409, { title: 'Kod već postoji.' }).message).toBe('Kod već postoji.')
    expect(new ApiError(404).message).toBe('Traženi podatak ne postoji.')
    expect(new ApiError(503).message).toBe('Greška na poslužitelju. Pokušaj ponovno.')
  })

  it('camel-cases field error keys', () => {
    const error = new ApiError(400, {
      errors: { NewPassword: ['Prekratka.'], Email: ['Obavezno.'] },
    })

    expect(error.fieldErrors).toEqual({ newPassword: ['Prekratka.'], email: ['Obavezno.'] })
  })

  it('keeps the problem list of a 422', () => {
    const error = new ApiError(422, {
      detail: 'GTFS feed se ne može izvesti.',
      problems: ['Nije unesen nijedan prijevoznik.'],
    })

    expect(error.problems).toEqual(['Nije unesen nijedan prijevoznik.'])
    expect(new ApiError(404).problems).toEqual([])
  })
})

describe('unwrap', () => {
  it('returns data for a successful response', () => {
    const result = unwrap({ data: { id: 1 }, response: new Response(null, { status: 200 }) })

    expect(result).toEqual({ id: 1 })
  })

  it('throws ApiError with the problem body for an error response', () => {
    const call = () =>
      unwrap({
        error: { title: 'Previše neuspjelih prijava.' },
        response: new Response(null, { status: 429 }),
      })

    expect(call).toThrowError(ApiError)
    expect(call).toThrowError('Previše neuspjelih prijava.')
  })
})

describe('errorMessage', () => {
  it('explains a network failure', () => {
    expect(errorMessage(new TypeError('Failed to fetch'))).toMatch(/nije dostupan/)
  })

  it('hides unexpected error details', () => {
    expect(errorMessage(new Error('x is undefined'))).toBe('Došlo je do neočekivane greške.')
  })
})
