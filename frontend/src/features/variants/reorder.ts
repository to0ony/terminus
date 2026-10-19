/**
 * Premješta element s mjesta <paramref name="from"/> na mjesto ispred
 * <paramref name="before"/> (0 je početak, list.length kraj), kao kad se redak
 * spusti iznad drugog retka.
 */
export function moveBefore<T>(list: T[], from: number, before: number): T[] {
  const to = before > from ? before - 1 : before
  if (to === from) {
    return list
  }
  const result = [...list]
  const [item] = result.splice(from, 1)
  result.splice(to, 0, item)
  return result
}
