// Autobus iz Tabler ikona (24 × 24, crte), da ikone na karti izgledaju kao u sučelju.
const busPaths = [
  'M4 17a2 2 0 1 0 4 0a2 2 0 1 0 -4 0',
  'M16 17a2 2 0 1 0 4 0a2 2 0 1 0 -4 0',
  'M4 17h-2v-11a1 1 0 0 1 1 -1h14a5 7 0 0 1 5 7v5h-2m-4 0h-8',
  'M16 5l1.5 7l4.5 0',
  'M2 10l15 0',
  'M7 5l0 5',
  'M12 5l0 5',
]

/** Plava stanica, kao na Google kartama. */
export const stopColor = '#1a73e8'

const pixelRatio = 2

/**
 * Okrugla ikona s bijelim autobusom, bijelim rubom i sjenom; crta se odmah
 * (canvas), pa se može dodati i kad je karta zatraži nakon promjene podloge.
 */
export function busIcon(fill: string, size: number): { image: ImageData; pixelRatio: number } {
  const pad = 3
  const total = (size + 2 * pad) * pixelRatio
  const canvas = document.createElement('canvas')
  canvas.width = total
  canvas.height = total
  const context = canvas.getContext('2d')!
  context.scale(pixelRatio, pixelRatio)

  const center = size / 2 + pad
  context.shadowColor = 'rgba(0, 0, 0, 0.35)'
  context.shadowBlur = 3
  context.shadowOffsetY = 1
  context.beginPath()
  context.arc(center, center, size / 2, 0, 2 * Math.PI)
  context.fillStyle = '#ffffff'
  context.fill()

  context.shadowColor = 'transparent'
  context.beginPath()
  context.arc(center, center, size / 2 - Math.max(1.5, size * 0.09), 0, 2 * Math.PI)
  context.fillStyle = fill
  context.fill()

  const glyph = size * 0.56
  context.translate(center - glyph / 2, center - glyph / 2)
  context.scale(glyph / 24, glyph / 24)
  context.strokeStyle = '#ffffff'
  context.lineWidth = 2.2
  context.lineCap = 'round'
  context.lineJoin = 'round'
  for (const path of busPaths) {
    context.stroke(new Path2D(path))
  }

  return { image: context.getImageData(0, 0, total, total), pixelRatio }
}
