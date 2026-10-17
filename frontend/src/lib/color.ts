/** "#1565c0" ili "1565C0" → "1565C0"; null za prazno ili neispravno. */
export function toHex(value: string): string | null {
  const hex = value.trim().replace(/^#/, '').toUpperCase()
  return /^[0-9A-F]{6}$/.test(hex) ? hex : null
}

/**
 * Omjer kontrasta dviju boja (hex bez #) prema WCAG 2: od 1 (isto) do 21
 * (crno na bijelom). Za običan tekst treba barem 4,5.
 */
export function contrastRatio(a: string, b: string): number {
  const [light, dark] = [luminance(a), luminance(b)].sort((x, y) => y - x)
  return (light + 0.05) / (dark + 0.05)
}

function luminance(hex: string): number {
  const [r, g, b] = [0, 2, 4].map((i) => {
    const channel = parseInt(hex.slice(i, i + 2), 16) / 255
    return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4
  })
  return 0.2126 * r + 0.7152 * g + 0.0722 * b
}
