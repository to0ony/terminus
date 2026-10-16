import { useParams } from 'react-router'

/**
 * Javni zaslon stanice (bez prijave). Namjerno bez Mantine komponenti, da se
 * brzo učitava i na slabijim zaslonima na stanicama.
 */
export function BoardPage() {
  const { stopCode } = useParams()

  return (
    <main style={{ fontFamily: 'system-ui, sans-serif', padding: '2rem' }}>
      <h1>Polasci</h1>
      <p>Stanica {stopCode}. Zaslon je u izradi.</p>
    </main>
  )
}
