import { useCallback, useReducer } from 'react'
import type { EditorTrip } from './model'

/** Koliko koraka se pamti za poništavanje. */
const historyLimit = 100

interface State {
  trips: EditorTrip[]
  past: EditorTrip[][]
  future: EditorTrip[][]
  selected: string[]
}

type Action =
  | { type: 'apply'; update: (trips: EditorTrip[]) => EditorTrip[]; select?: string[] }
  | { type: 'select'; keys: string[] }
  | { type: 'undo' }
  | { type: 'redo' }

function reducer(state: State, action: Action): State {
  switch (action.type) {
    case 'apply': {
      const trips = action.update(state.trips)
      if (trips === state.trips) {
        return state
      }
      const keys = new Set(trips.map((t) => t.key))
      return {
        trips,
        past: [...state.past, state.trips].slice(-historyLimit),
        future: [],
        selected: (action.select ?? state.selected).filter((key) => keys.has(key)),
      }
    }
    case 'select':
      return { ...state, selected: action.keys }
    case 'undo': {
      const previous = state.past.at(-1)
      if (!previous) {
        return state
      }
      return {
        ...state,
        trips: previous,
        past: state.past.slice(0, -1),
        future: [state.trips, ...state.future],
      }
    }
    case 'redo': {
      const next = state.future[0]
      if (!next) {
        return state
      }
      return {
        ...state,
        trips: next,
        past: [...state.past, state.trips],
        future: state.future.slice(1),
      }
    }
  }
}

/**
 * Vožnje u uređivanju s poviješću (poništi i ponovi) i odabirom. Svaka izmjena
 * je jedan korak; povlačenje u grafikonu šalje tek konačni rezultat. Za nove
 * podatke (drugi tip dana, nakon spremanja) komponenta se ponovno montira.
 */
export function useScheduleEditor(initial: EditorTrip[]) {
  const [state, dispatch] = useReducer(reducer, initial, (trips) => ({
    trips,
    past: [],
    future: [],
    selected: [],
  }))

  return {
    trips: state.trips,
    selected: state.selected,
    canUndo: state.past.length > 0,
    canRedo: state.future.length > 0,
    apply: useCallback(
      (update: (trips: EditorTrip[]) => EditorTrip[], select?: string[]) =>
        dispatch({ type: 'apply', update, select }),
      [],
    ),
    select: useCallback((keys: string[]) => dispatch({ type: 'select', keys }), []),
    undo: useCallback(() => dispatch({ type: 'undo' }), []),
    redo: useCallback(() => dispatch({ type: 'redo' }), []),
  }
}

export type ScheduleEditor = ReturnType<typeof useScheduleEditor>
