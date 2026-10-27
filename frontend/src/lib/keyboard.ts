/** Tipka je pritisnuta u polju za unos, pa je prečaci editora ne smiju preuzeti. */
export function isTyping(target: EventTarget | null) {
  return (
    target instanceof HTMLElement &&
    target.closest('input, textarea, select, [contenteditable="true"]') !== null
  )
}

/** Ctrl (ili Cmd na Macu) i zadano slovo, npr. Ctrl+S. */
export function isCtrl(event: KeyboardEvent, key: string) {
  return (event.ctrlKey || event.metaKey) && !event.altKey && event.key.toLowerCase() === key
}
