import { type RefObject, useEffect, useRef, useState } from 'react'
import { day, nowInZone } from './clock'

export type ClockMode = 'live' | 'simulation'

export interface SimulationClock {
  mode: ClockMode
  setMode: (mode: ClockMode) => void
  /** Dan karte (YYYY-MM-DD): uživo današnji, u simulaciji odabrani. */
  date: string
  setDate: (date: string) => void
  /** Sekunde od ponoći, osvježene nekoliko puta u sekundi (za prikaz). */
  time: number
  setTime: (seconds: number) => void
  /** Točno vrijeme u svakom trenutku, za animaciju vozila. */
  timeRef: RefObject<number>
  /** Koliko puta brže od stvarnog vremena teče simulacija. */
  speed: number
  setSpeed: (speed: number) => void
  playing: boolean
  setPlaying: (playing: boolean) => void
}

/**
 * Sat karte: uživo je današnji dan i trenutno vrijeme u zoni prijevoznika, a u
 * simulaciji odabrani dan i vrijeme koje teče zadanom brzinom (do kraja dana).
 */
export function useSimulationClock(timeZone: string): SimulationClock {
  const [mode, setModeState] = useState<ClockMode>('live')
  const [liveDate, setLiveDate] = useState(() => nowInZone(timeZone).date)
  const [simulationDate, setSimulationDate] = useState(liveDate)
  const [speed, setSpeed] = useState(60)
  const [playing, setPlaying] = useState(true)
  const [initialTime] = useState(() => nowInZone(timeZone).seconds)
  const timeRef = useRef(initialTime)
  const [time, setTimeState] = useState(initialTime)

  useEffect(() => {
    let frame = 0
    let last = performance.now()
    let shown = 0
    const tick = (now: number) => {
      const elapsed = (now - last) / 1000
      last = now
      if (mode === 'live') {
        const current = nowInZone(timeZone)
        timeRef.current = current.seconds
        setLiveDate((previous) => (previous === current.date ? previous : current.date))
      } else if (playing) {
        timeRef.current = Math.min(timeRef.current + elapsed * speed, day - 1)
        if (timeRef.current >= day - 1) {
          setPlaying(false)
        }
      }
      if (now - shown > 200) {
        shown = now
        setTimeState(timeRef.current)
      }
      frame = requestAnimationFrame(tick)
    }
    frame = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(frame)
  }, [mode, playing, speed, timeZone])

  const setTime = (seconds: number) => {
    timeRef.current = seconds
    setTimeState(seconds)
  }

  // Simulacija kreće od trenutnog dana i vremena.
  const setMode = (next: ClockMode) => {
    if (next === 'simulation' && mode === 'live') {
      setSimulationDate(liveDate)
      setPlaying(true)
    }
    setModeState(next)
  }

  return {
    mode,
    setMode,
    date: mode === 'live' ? liveDate : simulationDate,
    setDate: setSimulationDate,
    time,
    setTime,
    timeRef,
    speed,
    setSpeed,
    playing,
    setPlaying,
  }
}
