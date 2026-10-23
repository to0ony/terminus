import { Box, useComputedColorScheme } from '@mantine/core'
import { useElementSize } from '@mantine/hooks'
import { useEffect, useRef } from 'react'
import {
  clampView,
  type EditorTrip,
  firstDeparture,
  formatTime,
  headways,
  hour,
  minute,
  resizeView,
  moveStop,
  rowIndexes,
  shiftTrip,
  type View,
} from './model'
import type { ScheduleStop } from './queries'

/** Razmještaj grafikona u pikselima. */
const labelWidth = 180
const overviewHeight = 30
const axisHeight = 24
const headwayHeight = 44
const plotPadding = 10
const rowSpacing = 18
const minPlotHeight = 320
const dotRadius = 3.5
const hitRadius = 7
/** Koliko piksela oko ruba prozora na traci dana hvata hvataljku. */
const edgeGrab = 6

interface ScheduleChartProps {
  stops: ScheduleStop[]
  /** Položaj redaka od 0 do 1 (rowPositions). */
  positions: number[]
  /** Vožnje koje se uređuju. */
  trips: EditorTrip[]
  /** Vožnje drugog tipa dana za usporedbu; samo se prikazuju. */
  ghosts: EditorTrip[]
  /** Mantine boja tipa dana (npr. "blue"). */
  color: string
  selected: string[]
  invalid: Set<string>
  view: View
  /** Raspon cijelog dana voznog reda (za traku pregleda). */
  span: View
  onViewChange: (view: View) => void
  onSelect: (keys: string[]) => void
  onShiftTrips: (keys: string[], delta: number) => void
  onShiftStop: (key: string, index: number, delta: number, cascade: boolean) => void
  /** Okvirna linija: povlačenje prve ili zadnje točkice rasteže vožnju (moveStop). */
  approximate: boolean
  /** Nova vožnja s prvim polaskom u zadano vrijeme; null ako nema predloška. */
  onCreate: ((departure: number) => void) | null
  /** Predložak nove vožnje: redak i pomak od prvog polaska za svako vrijeme. */
  preview: { row: number; offset: number }[] | null
  /**
   * Crtanje vožnje po varijanti: redak svake njezine stanice, kliknute točke
   * (indeks stanice i vrijeme) i vožnja kakva bi nastala; null izvan crtanja.
   */
  drawing: Drawing | null
  onDrawPoint: (index: number, time: number) => void
}

export interface Drawing {
  rows: number[]
  anchors: { index: number; time: number }[]
  preview: EditorTrip | null
  /** Nazivi stanica varijante, za oblačić. */
  names: string[]
}

type Drag =
  | { kind: 'trips'; keys: string[]; startX: number; delta: number }
  | { kind: 'stop'; key: string; index: number; startX: number; delta: number; cascade: boolean }
  | { kind: 'box'; x0: number; y0: number; x1: number; y1: number; additive: boolean }
  | { kind: 'overview'; offset: number }
  | { kind: 'edge'; edge: 'start' | 'end' }
  | { kind: 'click'; x: number; y: number }

interface Hit {
  key: string
  /** Indeks vremena (točkice) ili null za crtu vožnje. */
  index: number | null
}

/**
 * Grafikon voznog reda (Mareyev dijagram): stanice su redci, vrijeme ide
 * vodoravno, a svaka vožnja je crta kroz točkice polazaka. Crta se povlači
 * vodoravno (pomak vožnje), točkica pomiče vrijeme na stanici (sa Shiftom i
 * sve iza nje). Kotačić miša zumira, Shift+kotačić pomiče. Na traci dana
 * prozor se povlači, a rubovi mu se hvataju i razvlače.
 */
export function ScheduleChart(props: ScheduleChartProps) {
  const { ref: sizeRef, width } = useElementSize()
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const scheme = useComputedColorScheme('light')

  const plotHeight = Math.max(minPlotHeight, (props.stops.length - 1) * rowSpacing)
  const height = overviewHeight + axisHeight + plotHeight + plotPadding * 2 + headwayHeight

  // Stanje povlačenja i lebdenja živi izvan Reacta, da crtanje ne čeka render.
  const drag = useRef<Drag | null>(null)
  const hover = useRef<{ x: number; y: number } | null>(null)
  const latest = useRef(props)
  useEffect(() => {
    latest.current = props
  })

  const geometry = (w: number) => {
    const p = latest.current
    const plotLeft = labelWidth
    const plotWidth = Math.max(10, w - labelWidth - 8)
    const plotTop = overviewHeight + axisHeight + plotPadding
    const [v0, v1] = p.view
    const x = (t: number) => plotLeft + ((t - v0) / (v1 - v0)) * plotWidth
    const time = (px: number) => v0 + ((px - plotLeft) / plotWidth) * (v1 - v0)
    const y = (row: number) => plotTop + (p.positions[row] ?? 0) * plotHeight
    return { plotLeft, plotWidth, plotTop, x, time, y }
  }

  /** Vožnje kakve se trenutno vide (s pomakom povlačenja). */
  const displayed = (): EditorTrip[] => {
    const p = latest.current
    const d = drag.current
    if (d?.kind === 'trips' && d.delta !== 0) {
      const keys = new Set(d.keys)
      return p.trips.map((t) => (keys.has(t.key) ? shiftTrip(t, d.delta) : t))
    }
    if (d?.kind === 'stop' && d.delta !== 0) {
      return p.trips.map((t) =>
        t.key === d.key ? moveStop(t, d.index, d.delta, d.cascade, p.approximate) : t,
      )
    }
    return p.trips
  }

  const rowsCache = useRef(new Map<string, number[]>())
  const rowsOf = (trip: EditorTrip) => {
    const key = trip.stopTimes.map((st) => st.stopId).join(',')
    let rows = rowsCache.current.get(key)
    if (!rows) {
      rows = rowIndexes(
        latest.current.stops.map((s) => s.id),
        trip.stopTimes.map((st) => st.stopId),
      )
      rowsCache.current.set(key, rows)
    }
    return rows
  }
  useEffect(() => {
    rowsCache.current.clear()
  }, [props.stops])

  const draw = () => {
    const canvas = canvasRef.current
    if (!canvas || width === 0) {
      return
    }
    const p = latest.current
    const dpr = window.devicePixelRatio || 1
    if (canvas.width !== Math.round(width * dpr) || canvas.height !== Math.round(height * dpr)) {
      canvas.width = Math.round(width * dpr)
      canvas.height = Math.round(height * dpr)
    }
    const ctx = canvas.getContext('2d')!
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0)
    const colors = palette(p.color)
    const g = geometry(width)
    const [v0, v1] = p.view

    ctx.clearRect(0, 0, width, height)
    ctx.font = '11px system-ui, sans-serif'
    ctx.textBaseline = 'middle'

    // Traka cijelog dana: gustoća polazaka i odabrani prozor.
    const [s0, s1] = p.span
    const ox = (t: number) => g.plotLeft + ((t - s0) / (s1 - s0)) * g.plotWidth
    ctx.fillStyle = colors.subtle
    ctx.fillRect(g.plotLeft, 4, g.plotWidth, overviewHeight - 8)
    const bins = new Map<number, number>()
    for (const trip of p.trips) {
      const bin = Math.floor(firstDeparture(trip) / (15 * minute))
      bins.set(bin, (bins.get(bin) ?? 0) + 1)
    }
    const maxBin = Math.max(1, ...bins.values())
    ctx.fillStyle = colors.trip
    for (const [bin, count] of bins) {
      const bx = ox(bin * 15 * minute)
      const bw = Math.max(1, ox((bin + 1) * 15 * minute) - bx - 1)
      const bh = ((overviewHeight - 10) * count) / maxBin
      ctx.globalAlpha = 0.5
      ctx.fillRect(bx, overviewHeight - 5 - bh, bw, bh)
    }
    ctx.globalAlpha = 1
    // Izvan prozora je zatamnjeno, a rubovi imaju hvataljke.
    const [wx0, wx1] = [ox(v0), Math.max(ox(v0) + 4, ox(v1))]
    ctx.fillStyle = colors.outside
    ctx.fillRect(g.plotLeft, 4, wx0 - g.plotLeft, overviewHeight - 8)
    ctx.fillRect(wx1, 4, g.plotLeft + g.plotWidth - wx1, overviewHeight - 8)
    ctx.strokeStyle = colors.text
    ctx.lineWidth = 1.5
    ctx.strokeRect(wx0, 3, wx1 - wx0, overviewHeight - 6)
    ctx.fillStyle = colors.text
    for (const hx of [wx0, wx1]) {
      ctx.beginPath()
      ctx.roundRect(hx - 3, overviewHeight / 2 - 7, 6, 14, 2)
      ctx.fill()
    }
    ctx.fillStyle = colors.dimmed
    ctx.textAlign = 'right'
    ctx.fillText('Cijeli dan', g.plotLeft - 8, overviewHeight / 2)

    // Os vremena i okomite crte.
    const pxPerMinute = (g.plotWidth / (v1 - v0)) * minute
    const step = [5, 10, 15, 30, 60, 120, 180].find((m) => m * pxPerMinute >= 48) ?? 240
    const top = overviewHeight + axisHeight
    ctx.textAlign = 'center'
    for (let t = Math.ceil(v0 / (step * minute)) * step * minute; t <= v1; t += step * minute) {
      const tx = g.x(t)
      if (tx > g.plotLeft + g.plotWidth - 14) {
        break
      }
      ctx.strokeStyle = t % hour === 0 ? colors.gridStrong : colors.grid
      ctx.lineWidth = 1
      ctx.beginPath()
      ctx.moveTo(tx, top)
      ctx.lineTo(tx, height - headwayHeight)
      ctx.stroke()
      ctx.fillStyle = t % hour === 0 ? colors.text : colors.dimmed
      ctx.fillText(formatTime(t % (24 * hour)), tx, overviewHeight + axisHeight / 2)
    }
    if (v0 < 24 * hour && v1 > 24 * hour) {
      // Ponoć: vožnje desno od nje kreću sutradan, ali pripadaju ovom danu.
      ctx.strokeStyle = colors.midnight
      ctx.setLineDash([4, 3])
      ctx.beginPath()
      ctx.moveTo(g.x(24 * hour), top)
      ctx.lineTo(g.x(24 * hour), height - headwayHeight)
      ctx.stroke()
      ctx.setLineDash([])
    }

    // Stanice: vodoravne crte i nazivi.
    ctx.textAlign = 'right'
    p.stops.forEach((stop, row) => {
      const ry = g.y(row)
      ctx.strokeStyle = colors.grid
      ctx.lineWidth = 1
      ctx.beginPath()
      ctx.moveTo(g.plotLeft, ry)
      ctx.lineTo(g.plotLeft + g.plotWidth, ry)
      ctx.stroke()
      const label = stop.platformCode ? `${stop.name} · ${stop.platformCode}` : stop.name
      if (stop.isActive) {
        ctx.fillStyle = colors.text
        ctx.fillText(truncate(ctx, label, labelWidth - 14), g.plotLeft - 8, ry)
        return
      }
      // Neaktivnu stanicu vožnje preskaču: blijedi naziv s crvenom točkom.
      const text = truncate(ctx, label, labelWidth - 28)
      ctx.fillStyle = colors.dimmed
      ctx.fillText(text, g.plotLeft - 8, ry)
      ctx.fillStyle = colors.invalid
      ctx.beginPath()
      ctx.arc(g.plotLeft - 16 - ctx.measureText(text).width, ry, 3.5, 0, Math.PI * 2)
      ctx.fill()
    })

    ctx.save()
    ctx.beginPath()
    ctx.rect(g.plotLeft, top, g.plotWidth, plotHeight + plotPadding * 2)
    ctx.clip()

    // Vožnje za usporedbu (blijede), pa vožnje koje se uređuju.
    const drawTrip = (trip: EditorTrip, style: string, lineWidth: number, dots: boolean) => {
      const times = trip.stopTimes
      if (times.length === 0 || times.at(-1)!.departureTime < v0 || times[0].arrivalTime > v1) {
        return
      }
      const rows = rowsOf(trip)
      ctx.strokeStyle = style
      ctx.lineWidth = lineWidth
      ctx.beginPath()
      times.forEach((st, i) => {
        const ry = g.y(rows[i])
        if (i === 0) {
          ctx.moveTo(g.x(st.arrivalTime), ry)
        } else {
          ctx.lineTo(g.x(st.arrivalTime), ry)
        }
        if (st.departureTime !== st.arrivalTime) {
          ctx.lineTo(g.x(st.departureTime), ry)
        }
      })
      ctx.stroke()
      if (dots) {
        ctx.fillStyle = style
        times.forEach((st, i) => {
          ctx.beginPath()
          ctx.arc(g.x(st.departureTime), g.y(rows[i]), dotRadius, 0, Math.PI * 2)
          ctx.fill()
        })
      }
    }

    for (const ghost of p.ghosts) {
      drawTrip(ghost, colors.ghost, 1, false)
    }
    const selected = new Set(p.selected)
    const trips = displayed()
    const showDots = pxPerMinute >= 2.5
    for (const trip of trips) {
      if (!selected.has(trip.key)) {
        drawTrip(trip, p.invalid.has(trip.key) ? colors.invalid : colors.trip, 1.5, showDots)
      }
    }
    for (const trip of trips) {
      if (selected.has(trip.key)) {
        drawTrip(trip, p.invalid.has(trip.key) ? colors.invalid : colors.selected, 3, true)
      }
    }

    // Crtanje: prijedlog vožnje, kliknute točke i crta do pokazivača.
    const h = hover.current
    let createLabel: string | null = null
    let drawLabel: { x: number; y: number; text: string } | null = null
    if (p.drawing) {
      if (p.drawing.preview) {
        ctx.setLineDash([5, 4])
        drawTrip(p.drawing.preview, colors.selected, 2, false)
        ctx.setLineDash([])
      }
      ctx.fillStyle = colors.selected
      for (const anchor of p.drawing.anchors) {
        ctx.beginPath()
        ctx.arc(
          g.x(anchor.time),
          g.y(p.drawing.rows[anchor.index]),
          dotRadius + 1.5,
          0,
          Math.PI * 2,
        )
        ctx.fill()
      }
      const target = h ? drawTarget(h.x, h.y) : null
      if (target) {
        const tx = g.x(target.time)
        const ty = g.y(p.drawing.rows[target.index])
        const last = p.drawing.anchors.at(-1)
        ctx.strokeStyle = colors.selected
        ctx.lineWidth = 1.5
        ctx.setLineDash([2, 3])
        if (last) {
          ctx.beginPath()
          ctx.moveTo(g.x(last.time), g.y(p.drawing.rows[last.index]))
          ctx.lineTo(tx, ty)
          ctx.stroke()
        }
        ctx.setLineDash([])
        ctx.beginPath()
        ctx.arc(tx, ty, dotRadius + 1.5, 0, Math.PI * 2)
        ctx.stroke()
        drawLabel = {
          x: tx,
          y: ty - 16,
          text: `${p.drawing.names[target.index]} · ${formatTime(target.time)}`,
        }
      }
    } else if (
      h &&
      !drag.current &&
      p.preview &&
      p.onCreate &&
      hoversFirstRow(h.y, g.y(p.preview[0].row))
    ) {
      const start = Math.round(g.time(h.x) / minute) * minute
      ctx.strokeStyle = colors.trip
      ctx.setLineDash([5, 4])
      ctx.lineWidth = 1.5
      ctx.beginPath()
      p.preview.forEach((point, i) => {
        const px = g.x(start + point.offset)
        const py = g.y(point.row)
        if (i === 0) {
          ctx.moveTo(px, py)
        } else {
          ctx.lineTo(px, py)
        }
      })
      ctx.stroke()
      ctx.setLineDash([])
      createLabel = `Nova vožnja ${formatTime(start)}`
    }

    const d = drag.current
    if (d?.kind === 'box') {
      ctx.fillStyle = colors.box
      ctx.strokeStyle = colors.selected
      ctx.lineWidth = 1
      const [bx, by] = [Math.min(d.x0, d.x1), Math.min(d.y0, d.y1)]
      ctx.fillRect(bx, by, Math.abs(d.x1 - d.x0), Math.abs(d.y1 - d.y0))
      ctx.strokeRect(bx, by, Math.abs(d.x1 - d.x0), Math.abs(d.y1 - d.y0))
    }
    ctx.restore()

    // Oblačić s vremenom ispod pokazivača ili pomakom pri povlačenju.
    if (drawLabel) {
      tooltip(ctx, colors, drawLabel.x, drawLabel.y, drawLabel.text)
    } else if (createLabel && h && p.preview) {
      tooltip(ctx, colors, h.x, g.y(p.preview[0].row) - 14, createLabel)
    } else if (d?.kind === 'trips' || d?.kind === 'stop') {
      if (h) {
        const sign = d.delta >= 0 ? '+' : '−'
        tooltip(ctx, colors, h.x, h.y - 18, `${sign}${Math.abs(d.delta / minute)} min`)
      }
    } else if (h && !d) {
      const hit = hitTest(h.x, h.y)
      if (hit?.index !== null && hit) {
        const trip = p.trips.find((t) => t.key === hit.key)!
        const st = trip.stopTimes[hit.index]
        const stop = p.stops.find((s) => s.id === st.stopId)
        tooltip(ctx, colors, h.x, h.y - 18, `${stop?.name ?? ''} · ${formatTime(st.departureTime)}`)
      }
    }

    // Traka intervala između polazaka.
    const hy = height - headwayHeight
    ctx.fillStyle = colors.dimmed
    ctx.textAlign = 'right'
    ctx.fillText('Interval', g.plotLeft - 8, hy + headwayHeight / 2)
    ctx.save()
    ctx.beginPath()
    ctx.rect(g.plotLeft, hy, g.plotWidth, headwayHeight)
    ctx.clip()
    ctx.textAlign = 'center'
    const gaps = headways(trips)
    // Najdulji interval u prikazu (barem 15 min) je puna visina trake.
    const longest = Math.max(
      15 * minute,
      ...gaps.filter(({ at }) => at >= v0 && at <= v1).map(({ gap }) => gap),
    )
    for (const { at, gap } of gaps) {
      if (at < v0 - hour || at > v1 + hour) {
        continue
      }
      const bh = Math.min(headwayHeight - 14, ((headwayHeight - 14) * gap) / longest)
      const bx = g.x(at)
      ctx.fillStyle = colors.trip
      ctx.globalAlpha = 0.35
      ctx.fillRect(bx - 2, hy + headwayHeight - 2 - bh, 4, bh)
      ctx.globalAlpha = 1
      if (pxPerMinute * (gap / minute) > 26) {
        ctx.fillStyle = colors.dimmed
        ctx.fillText(`${Math.round(gap / minute)}′`, bx, hy + 7)
      }
    }
    ctx.restore()
  }

  /**
   * Stanica varijante najbliža pokazivaču pri crtanju i vrijeme na minutu.
   * Prva točka je uvijek na prvoj stanici, a svaka sljedeća iza prethodne.
   */
  const drawTarget = (px: number, py: number): { index: number; time: number } | null => {
    const p = latest.current
    if (!p.drawing) {
      return null
    }
    const g = geometry(width)
    if (px < g.plotLeft || py < overviewHeight + axisHeight || py > height - headwayHeight) {
      return null
    }
    const last = p.drawing.anchors.at(-1)
    const candidates = last ? p.drawing.rows.map((_, i) => i).filter((i) => i > last.index) : [0]
    let best: number | null = null
    for (const i of candidates) {
      const distance = Math.abs(g.y(p.drawing.rows[i]) - py)
      if (
        distance <= 14 &&
        (best === null || distance < Math.abs(g.y(p.drawing.rows[best]) - py))
      ) {
        best = i
      }
    }
    if (best === null) {
      return null
    }
    const time = Math.round(g.time(px) / minute) * minute
    return { index: best, time: last ? Math.max(time, last.time) : time }
  }

  /** Točkica ili crta vožnje ispod pokazivača; odabrane imaju prednost. */
  const hitTest = (px: number, py: number): Hit | null => {
    const g = geometry(width)
    const p = latest.current
    if (px < g.plotLeft || py < overviewHeight + axisHeight || py > height - headwayHeight) {
      return null
    }
    const selected = new Set(p.selected)
    const ordered = [...p.trips].sort(
      (a, b) => Number(selected.has(b.key)) - Number(selected.has(a.key)),
    )
    let line: Hit | null = null
    let lineDistance = 5
    for (const trip of ordered) {
      const rows = rowsOf(trip)
      for (let i = 0; i < trip.stopTimes.length; i++) {
        const tx = g.x(trip.stopTimes[i].departureTime)
        const ty = g.y(rows[i])
        if (Math.hypot(tx - px, ty - py) <= hitRadius) {
          return { key: trip.key, index: i }
        }
        if (i > 0) {
          const distance = segmentDistance(
            px,
            py,
            g.x(trip.stopTimes[i - 1].departureTime),
            g.y(rows[i - 1]),
            g.x(trip.stopTimes[i].arrivalTime),
            ty,
          )
          if (distance < lineDistance) {
            lineDistance = distance
            line = { key: trip.key, index: null }
          }
        }
      }
    }
    return line
  }

  // Crtanje nakon svake promjene podataka, veličine ili teme.
  useEffect(() => {
    draw()
  })

  // Kotačić: zum oko pokazivača; Shift ili vodoravno pomicanje: pomak.
  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) {
      return
    }
    const onWheel = (event: WheelEvent) => {
      const p = latest.current
      const g = geometry(canvas.clientWidth)
      if (event.offsetX < g.plotLeft) {
        return
      }
      event.preventDefault()
      const [v0, v1] = p.view
      const length = v1 - v0
      if (event.shiftKey || Math.abs(event.deltaX) > Math.abs(event.deltaY)) {
        const delta = ((event.shiftKey ? event.deltaY : event.deltaX) / g.plotWidth) * length
        p.onViewChange(clampView([v0 + delta, v1 + delta], p.span))
        return
      }
      const factor = Math.exp(event.deltaY * 0.0015)
      const at = g.time(event.offsetX)
      p.onViewChange(clampView([at - (at - v0) * factor, at + (v1 - at) * factor], p.span))
    }
    canvas.addEventListener('wheel', onWheel, { passive: false })
    return () => canvas.removeEventListener('wheel', onWheel)
  })

  /** Rub prozora na traci dana ispod pokazivača. */
  const overviewEdge = (x: number, y: number): 'start' | 'end' | null => {
    const p = latest.current
    const g = geometry(width)
    if (y >= overviewHeight || x < g.plotLeft - edgeGrab) {
      return null
    }
    const [s0, s1] = p.span
    const ox = (t: number) => g.plotLeft + ((t - s0) / (s1 - s0)) * g.plotWidth
    const [start, end] = [ox(p.view[0]), ox(p.view[1])]
    const toStart = Math.abs(x - start)
    const toEnd = Math.abs(x - end)
    if (Math.min(toStart, toEnd) > edgeGrab) {
      return null
    }
    // Kad su rubovi blizu, desni se hvata desno od sredine prozora.
    return toEnd < toStart || (toEnd === toStart && x > (start + end) / 2) ? 'end' : 'start'
  }

  const position = (event: React.PointerEvent) => {
    const rect = canvasRef.current!.getBoundingClientRect()
    return { x: event.clientX - rect.left, y: event.clientY - rect.top }
  }

  const onPointerDown = (event: React.PointerEvent<HTMLCanvasElement>) => {
    if (event.button !== 0) {
      return
    }
    const p = latest.current
    const { x, y } = position(event)
    const g = geometry(width)
    event.currentTarget.setPointerCapture(event.pointerId)

    const edge = overviewEdge(x, y)
    if (edge) {
      drag.current = { kind: 'edge', edge }
      return
    }
    if (y < overviewHeight && x >= g.plotLeft) {
      // Traka dana: povlačenje prozora ili skok na kliknuto mjesto.
      const [s0, s1] = p.span
      const at = s0 + ((x - g.plotLeft) / g.plotWidth) * (s1 - s0)
      const [v0, v1] = p.view
      if (at < v0 || at > v1) {
        const half = (v1 - v0) / 2
        p.onViewChange(clampView([at - half, at + half], p.span))
        drag.current = { kind: 'overview', offset: half }
      } else {
        drag.current = { kind: 'overview', offset: at - v0 }
      }
      return
    }

    if (p.drawing) {
      const target = drawTarget(x, y)
      if (target) {
        p.onDrawPoint(target.index, target.time)
      }
      return
    }

    const hit = hitTest(x, y)
    const additive = event.ctrlKey || event.metaKey
    if (hit) {
      if (additive) {
        p.onSelect(
          p.selected.includes(hit.key)
            ? p.selected.filter((k) => k !== hit.key)
            : [...p.selected, hit.key],
        )
        return
      }
      const keys = p.selected.includes(hit.key) ? p.selected : [hit.key]
      if (!p.selected.includes(hit.key)) {
        p.onSelect(keys)
      }
      drag.current =
        hit.index === null
          ? { kind: 'trips', keys, startX: x, delta: 0 }
          : {
              kind: 'stop',
              key: hit.key,
              index: hit.index,
              startX: x,
              delta: 0,
              cascade: event.shiftKey,
            }
      return
    }
    drag.current = { kind: 'click', x, y }
    if (event.shiftKey || additive) {
      drag.current = { kind: 'box', x0: x, y0: y, x1: x, y1: y, additive: true }
    }
  }

  const onPointerMove = (event: React.PointerEvent<HTMLCanvasElement>) => {
    const p = latest.current
    const { x, y } = position(event)
    hover.current = { x, y }
    const g = geometry(width)
    const d = drag.current
    const secondsPerPixel = (p.view[1] - p.view[0]) / g.plotWidth

    if (d?.kind === 'overview') {
      const [s0, s1] = p.span
      const at = s0 + ((x - g.plotLeft) / g.plotWidth) * (s1 - s0)
      const length = p.view[1] - p.view[0]
      p.onViewChange(clampView([at - d.offset, at - d.offset + length], p.span))
    } else if (d?.kind === 'edge') {
      const [s0, s1] = p.span
      const at = s0 + ((x - g.plotLeft) / g.plotWidth) * (s1 - s0)
      p.onViewChange(resizeView(p.view, d.edge, Math.round(at / minute) * minute, p.span))
    } else if (d?.kind === 'trips' || d?.kind === 'stop') {
      d.delta = Math.round(((x - d.startX) * secondsPerPixel) / minute) * minute
    } else if (d?.kind === 'click' && Math.hypot(x - d.x, y - d.y) > 4) {
      drag.current = { kind: 'box', x0: d.x, y0: d.y, x1: x, y1: y, additive: false }
    } else if (d?.kind === 'box') {
      d.x1 = x
      d.y1 = y
    }

    const canvas = event.currentTarget
    const hit = d ? null : hitTest(x, y)
    canvas.style.cursor =
      d?.kind === 'edge' || (!d && overviewEdge(x, y))
        ? 'ew-resize'
        : d?.kind === 'overview' || (!d && y < overviewHeight && x >= g.plotLeft)
          ? 'grab'
          : p.drawing
            ? 'crosshair'
            : d?.kind === 'trips' || d?.kind === 'stop'
              ? 'grabbing'
              : hit?.index !== null && hit
                ? 'ew-resize'
                : hit
                  ? 'grab'
                  : p.preview && p.onCreate && hoversFirstRow(y, g.y(p.preview[0].row))
                    ? 'copy'
                    : 'default'
    draw()
  }

  const onPointerUp = (event: React.PointerEvent<HTMLCanvasElement>) => {
    const p = latest.current
    const d = drag.current
    drag.current = null
    const { x, y } = position(event)
    const g = geometry(width)

    if (d?.kind === 'trips' && d.delta !== 0) {
      p.onShiftTrips(d.keys, d.delta)
    } else if (d?.kind === 'stop' && d.delta !== 0) {
      p.onShiftStop(d.key, d.index, d.delta, d.cascade)
    } else if (d?.kind === 'click') {
      if (p.preview && p.onCreate && hoversFirstRow(y, g.y(p.preview[0].row))) {
        p.onCreate(Math.round(g.time(x) / minute) * minute)
      } else {
        p.onSelect([])
      }
    } else if (d?.kind === 'box') {
      const [t0, t1] = [g.time(Math.min(d.x0, d.x1)), g.time(Math.max(d.x0, d.x1))]
      const [y0, y1] = [Math.min(d.y0, d.y1), Math.max(d.y0, d.y1)]
      const inside = p.trips
        .filter((trip) => {
          const rows = rowsOf(trip)
          return trip.stopTimes.some((st, i) => {
            const ty = g.y(rows[i])
            return st.departureTime >= t0 && st.departureTime <= t1 && ty >= y0 && ty <= y1
          })
        })
        .map((t) => t.key)
      p.onSelect(d.additive ? [...new Set([...p.selected, ...inside])] : inside)
    }
    draw()
  }

  return (
    <Box ref={sizeRef} w="100%">
      <canvas
        ref={canvasRef}
        style={{ width: '100%', height, display: 'block', touchAction: 'none' }}
        data-scheme={scheme}
        onPointerDown={onPointerDown}
        onPointerMove={onPointerMove}
        onPointerUp={onPointerUp}
        onPointerLeave={() => {
          hover.current = null
          draw()
        }}
      />
    </Box>
  )
}

function hoversFirstRow(y: number, rowY: number): boolean {
  return Math.abs(y - rowY) <= 6
}

function segmentDistance(px: number, py: number, x1: number, y1: number, x2: number, y2: number) {
  const dx = x2 - x1
  const dy = y2 - y1
  const length = dx * dx + dy * dy
  const t = length === 0 ? 0 : Math.max(0, Math.min(1, ((px - x1) * dx + (py - y1) * dy) / length))
  return Math.hypot(px - (x1 + t * dx), py - (y1 + t * dy))
}

function truncate(ctx: CanvasRenderingContext2D, text: string, max: number): string {
  if (ctx.measureText(text).width <= max) {
    return text
  }
  let result = text
  while (result.length > 1 && ctx.measureText(`${result}…`).width > max) {
    result = result.slice(0, -1)
  }
  return `${result}…`
}

type Palette = ReturnType<typeof palette>

function tooltip(
  ctx: CanvasRenderingContext2D,
  colors: Palette,
  x: number,
  y: number,
  text: string,
) {
  ctx.font = '12px system-ui, sans-serif'
  const w = ctx.measureText(text).width + 12
  const left = Math.max(2, x - w / 2)
  ctx.fillStyle = colors.tooltip
  ctx.fillRect(left, y - 10, w, 20)
  ctx.fillStyle = colors.tooltipText
  ctx.textAlign = 'center'
  ctx.fillText(text, left + w / 2, y)
  ctx.font = '11px system-ui, sans-serif'
}

/** Boje iz Mantine teme (CSS varijable), da grafikon prati svijetlu i tamnu temu. */
function palette(color: string) {
  const css = getComputedStyle(document.documentElement)
  const v = (name: string) => css.getPropertyValue(name).trim()
  const dark = document.documentElement.getAttribute('data-mantine-color-scheme') === 'dark'
  return {
    text: v('--mantine-color-text'),
    dimmed: v('--mantine-color-dimmed'),
    grid: dark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)',
    gridStrong: dark ? 'rgba(255,255,255,0.16)' : 'rgba(0,0,0,0.16)',
    subtle: dark ? 'rgba(255,255,255,0.04)' : 'rgba(0,0,0,0.035)',
    outside: dark ? 'rgba(0,0,0,0.35)' : 'rgba(255,255,255,0.55)',
    midnight: v('--mantine-color-orange-filled'),
    trip: v(`--mantine-color-${color}-filled`),
    selected: v(`--mantine-color-${color}-${dark ? '3' : '9'}`),
    invalid: v('--mantine-color-red-filled'),
    ghost: dark ? 'rgba(255,255,255,0.18)' : 'rgba(0,0,0,0.16)',
    box: dark ? 'rgba(120,160,255,0.12)' : 'rgba(30,90,200,0.08)',
    tooltip: dark ? 'rgba(240,240,240,0.95)' : 'rgba(20,20,20,0.85)',
    tooltipText: dark ? '#111' : '#fff',
  }
}
