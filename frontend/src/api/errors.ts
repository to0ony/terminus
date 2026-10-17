/** ProblemDetails / ValidationProblemDetails kakve vraća backend. */
interface ProblemBody {
  title?: string | null
  detail?: string | null
  status?: number | null
  errors?: Record<string, string[]>
  /** Popis problema uz 422 (UnprocessableException). */
  problems?: string[]
}

/** Greška API-ja s porukom iz ProblemDetails (poruke backenda su na hrvatskom). */
export class ApiError extends Error {
  readonly status: number
  readonly title: string | null
  readonly detail: string | null
  /** Greške po poljima (400); ključevi su nazivi polja kao u zahtjevu. */
  readonly fieldErrors: Record<string, string[]>
  /** Što treba ispraviti (422), npr. zašto se GTFS ne može izvesti. */
  readonly problems: string[]

  constructor(status: number, body?: ProblemBody | null) {
    const title = body?.title ?? null
    const detail = body?.detail ?? null
    super(detail ?? title ?? defaultMessage(status))
    this.name = 'ApiError'
    this.status = status
    this.title = title
    this.detail = detail
    this.fieldErrors = normalizeKeys(body?.errors ?? {})
    this.problems = body?.problems ?? []
  }
}

/** Poruka za prikaz korisniku za bilo koju grešku. */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.message
  }
  if (error instanceof TypeError) {
    // fetch baca TypeError kad poslužitelj nije dostupan.
    return 'Poslužitelj nije dostupan. Provjeri vezu i pokušaj ponovno.'
  }
  return 'Došlo je do neočekivane greške.'
}

interface FetchResult<T> {
  data?: T
  error?: unknown
  response: Response
}

/**
 * Podaci iz odgovora openapi-fetch klijenta ili ApiError. Za TanStack Query:
 * `queryFn: async () => unwrap(await api.GET(...))`.
 */
export function unwrap<T>({ data, error, response }: FetchResult<T>): T {
  if (!response.ok) {
    throw new ApiError(response.status, isProblem(error) ? error : null)
  }
  return data as T
}

function isProblem(value: unknown): value is ProblemBody {
  return typeof value === 'object' && value !== null
}

// ASP.NET vraća nazive polja kako su u C# ("Email", "NewPassword"); forme
// koriste camelCase kao i JSON ("email", "newPassword").
function normalizeKeys(errors: Record<string, string[]>): Record<string, string[]> {
  return Object.fromEntries(
    Object.entries(errors).map(([key, messages]) => [
      key.length > 0 ? key[0].toLowerCase() + key.slice(1) : key,
      messages,
    ]),
  )
}

function defaultMessage(status: number): string {
  switch (status) {
    case 401:
      return 'Prijava je istekla. Prijavi se ponovno.'
    case 403:
      return 'Nemaš ovlasti za ovu radnju.'
    case 404:
      return 'Traženi podatak ne postoji.'
    case 429:
      return 'Previše zahtjeva. Pokušaj ponovno malo kasnije.'
    default:
      return status >= 500
        ? 'Greška na poslužitelju. Pokušaj ponovno.'
        : `Zahtjev nije uspio (${status}).`
  }
}
