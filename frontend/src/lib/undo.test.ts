import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { deferDelete, undoDelay } from './undo'

const { show, hide } = vi.hoisted(() => ({ show: vi.fn(() => 'notice'), hide: vi.fn() }))
vi.mock('@mantine/notifications', () => ({ notifications: { show, hide } }))

/** Gumb „Poništi” iz zadnje obavijesti. */
function clickUndo() {
  const calls = show.mock.calls as unknown as [{ message: { props: { children: unknown[] } } }][]
  const message = calls.at(-1)![0].message
  const button = message.props.children[1] as { props: { onClick: () => void } }
  button.props.onClick()
}

function setup(commit = vi.fn(() => Promise.resolve())) {
  const apply = vi.fn()
  const revert = vi.fn()
  deferDelete({ message: 'Obrisano.', apply, revert, commit, errorTitle: 'Nije obrisano' })
  return { apply, revert, commit }
}

describe('deferDelete', () => {
  beforeEach(() => vi.useFakeTimers())
  afterEach(() => {
    vi.useRealTimers()
    show.mockClear()
  })

  it('removes the item at once and deletes it when the undo time runs out', () => {
    const { apply, commit, revert } = setup()

    expect(apply).toHaveBeenCalledOnce()
    expect(commit).not.toHaveBeenCalled()
    vi.advanceTimersByTime(undoDelay)
    expect(commit).toHaveBeenCalledOnce()
    expect(revert).not.toHaveBeenCalled()
  })

  it('undo brings the item back and never deletes it', () => {
    const { commit, revert } = setup()

    clickUndo()
    vi.advanceTimersByTime(undoDelay * 2)
    expect(revert).toHaveBeenCalledOnce()
    expect(commit).not.toHaveBeenCalled()
    expect(hide).toHaveBeenCalledWith('notice')
  })

  it('brings the item back and reports the error when the server refuses', async () => {
    const { revert } = setup(vi.fn(() => Promise.reject(new Error('Koriste je vožnje.'))))

    await vi.advanceTimersByTimeAsync(undoDelay)
    expect(revert).toHaveBeenCalledOnce()
    expect(show).toHaveBeenLastCalledWith(
      expect.objectContaining({ color: 'red', title: 'Nije obrisano' }),
    )
  })

  it('deletes at once when the page is closed', () => {
    const { commit } = setup()

    window.dispatchEvent(new Event('pagehide'))
    expect(commit).toHaveBeenCalledOnce()
    vi.advanceTimersByTime(undoDelay)
    expect(commit).toHaveBeenCalledOnce()
  })
})
