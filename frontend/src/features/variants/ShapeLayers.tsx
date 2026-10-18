import { Paper, Text } from '@mantine/core'
import type { FeatureCollection, LineString, Point } from 'geojson'
import type { GeoJSONSource, Map, MapLayerMouseEvent, MapMouseEvent } from 'maplibre-gl'
import { useEffect, useRef } from 'react'
import type { LngLat } from './osmNetwork'

const lineSource = 'terminus-shape'
const viaSource = 'terminus-shape-via'
const dragSource = 'terminus-shape-drag'
const lineLayer = 'terminus-shape-line'
const pendingLayer = 'terminus-shape-pending'
const viaLayer = 'terminus-shape-via'

/** Dionica za crtanje: null točke znače da još nije izračunata (crtkano, ravno). */
export interface DrawnSegment {
  points: LngLat[]
  failed: boolean
  pending: boolean
  manual: boolean
  via: LngLat[]
}

interface ShapeLayersProps {
  map: Map
  segments: DrawnSegment[]
  selected: number | null
  onSelect: (segment: number) => void
  /** Crta je uhvaćena na <paramref name="grabbed"/> i spuštena na <paramref name="dropped"/>. */
  onDragLine: (segment: number, grabbed: LngLat, dropped: LngLat) => void
  onMoveVia: (segment: number, index: number, to: LngLat) => void
  onRemoveVia: (segment: number, index: number) => void
}

/**
 * Trasa varijante na karti stanica: dionice (crveno kad put nije nađen, crtkano
 * dok nije izračunata) i međutočke. Povlačenje crte dodaje međutočku, povlačenje
 * međutočke je pomiče, a desni klik je briše.
 */
export function ShapeLayers(props: ShapeLayersProps) {
  const { map, segments, selected } = props
  const handlers = useRef(props)
  useEffect(() => {
    handlers.current = props
  })

  // Slojevi ispod stanica i brojeva varijante, da se stanice i dalje vide.
  useEffect(() => {
    const below = map.getLayer('terminus-stops-selected') ? 'terminus-stops-selected' : undefined
    map.addSource(lineSource, { type: 'geojson', data: emptyLines })
    map.addSource(viaSource, { type: 'geojson', data: emptyPoints })
    map.addSource(dragSource, { type: 'geojson', data: emptyPoints })
    map.addLayer(
      {
        id: `${lineLayer}-casing`,
        type: 'line',
        source: lineSource,
        filter: ['==', ['get', 'selected'], true],
        layout: { 'line-cap': 'round', 'line-join': 'round' },
        paint: { 'line-color': '#ffffff', 'line-width': 10, 'line-opacity': 0.9 },
      },
      below,
    )
    map.addLayer(
      {
        id: lineLayer,
        type: 'line',
        source: lineSource,
        filter: ['!', ['get', 'pending']],
        layout: { 'line-cap': 'round', 'line-join': 'round' },
        paint: {
          'line-color': [
            'case',
            ['get', 'failed'],
            '#fa5252',
            ['get', 'manual'],
            '#1098ad',
            '#7048e8',
          ],
          'line-width': ['case', ['get', 'selected'], 6, 4.5],
        },
      },
      below,
    )
    map.addLayer(
      {
        id: pendingLayer,
        type: 'line',
        source: lineSource,
        filter: ['get', 'pending'],
        paint: {
          'line-color': '#868e96',
          'line-width': ['case', ['get', 'selected'], 6, 4.5],
          'line-dasharray': [1, 1.5],
        },
      },
      below,
    )
    map.addLayer({
      id: viaLayer,
      type: 'circle',
      source: viaSource,
      paint: {
        'circle-radius': 6,
        'circle-color': '#ffffff',
        'circle-stroke-color': '#7048e8',
        'circle-stroke-width': 3,
      },
    })
    map.addLayer({
      id: `${viaLayer}-drag`,
      type: 'circle',
      source: dragSource,
      paint: {
        'circle-radius': 7,
        'circle-color': '#7048e8',
        'circle-stroke-color': '#ffffff',
        'circle-stroke-width': 2,
      },
    })

    const setDragPoint = (point: LngLat | null) =>
      map.getSource<GeoJSONSource>(dragSource)?.setData(
        point
          ? {
              type: 'FeatureCollection',
              features: [
                {
                  type: 'Feature',
                  geometry: { type: 'Point', coordinates: point },
                  properties: {},
                },
              ],
            }
          : emptyPoints,
      )

    /** Povlačenje od točke <paramref name="start"/>; na kraju poziva <paramref name="done"/>. */
    const drag = (start: MapLayerMouseEvent, done: (to: LngLat, moved: boolean) => void) => {
      // Karta se ne pomiče dok se vuče trasa.
      start.preventDefault()
      const from = start.point
      let moved = false
      const onMove = (event: MapMouseEvent) => {
        moved ||= Math.hypot(event.point.x - from.x, event.point.y - from.y) > 4
        if (moved) {
          setDragPoint([event.lngLat.lng, event.lngLat.lat])
        }
      }
      map.on('mousemove', onMove)
      map.once('mouseup', (event) => {
        map.off('mousemove', onMove)
        setDragPoint(null)
        done([event.lngLat.lng, event.lngLat.lat], moved)
      })
    }

    const onLineDown = (event: MapLayerMouseEvent) => {
      if (event.originalEvent.button !== 0) {
        return
      }
      // Međutočka je iznad crte i ima prednost.
      if (map.queryRenderedFeatures(event.point, { layers: [viaLayer] }).length > 0) {
        return
      }
      const segment = Number(event.features?.[0]?.properties.index)
      const grabbed: LngLat = [event.lngLat.lng, event.lngLat.lat]
      drag(event, (to, moved) =>
        moved
          ? handlers.current.onDragLine(segment, grabbed, to)
          : handlers.current.onSelect(segment),
      )
    }

    const onViaDown = (event: MapLayerMouseEvent) => {
      if (event.originalEvent.button !== 0) {
        return
      }
      const properties = event.features?.[0]?.properties
      const segment = Number(properties?.segment)
      const index = Number(properties?.index)
      drag(event, (to, moved) =>
        moved ? handlers.current.onMoveVia(segment, index, to) : handlers.current.onSelect(segment),
      )
    }

    const onViaMenu = (event: MapLayerMouseEvent) => {
      event.preventDefault()
      const properties = event.features?.[0]?.properties
      handlers.current.onRemoveVia(Number(properties?.segment), Number(properties?.index))
    }

    const pointer = (cursor: string) => () => {
      map.getCanvas().style.cursor = cursor
    }
    const grab = pointer('grab')
    const move = pointer('move')
    const reset = pointer('')

    const lines = [lineLayer, pendingLayer]
    map.on('mousedown', lines, onLineDown)
    map.on('mousedown', viaLayer, onViaDown)
    map.on('contextmenu', viaLayer, onViaMenu)
    map.on('mouseenter', lines, grab)
    map.on('mouseleave', lines, reset)
    map.on('mouseenter', viaLayer, move)
    map.on('mouseleave', viaLayer, reset)

    return () => {
      map.off('mousedown', lines, onLineDown)
      map.off('mousedown', viaLayer, onViaDown)
      map.off('contextmenu', viaLayer, onViaMenu)
      map.off('mouseenter', lines, grab)
      map.off('mouseleave', lines, reset)
      map.off('mouseenter', viaLayer, move)
      map.off('mouseleave', viaLayer, reset)
      for (const layer of [
        `${viaLayer}-drag`,
        viaLayer,
        pendingLayer,
        lineLayer,
        `${lineLayer}-casing`,
      ]) {
        if (map.getLayer(layer)) {
          map.removeLayer(layer)
        }
      }
      for (const source of [dragSource, viaSource, lineSource]) {
        if (map.getSource(source)) {
          map.removeSource(source)
        }
      }
      map.getCanvas().style.cursor = ''
    }
  }, [map])

  useEffect(() => {
    map.getSource<GeoJSONSource>(lineSource)?.setData({
      type: 'FeatureCollection',
      features: segments.map((segment, index) => ({
        type: 'Feature',
        geometry: { type: 'LineString', coordinates: segment.points },
        properties: {
          index,
          failed: segment.failed,
          pending: segment.pending,
          manual: segment.manual,
          selected: index === selected,
        },
      })),
    })
    map.getSource<GeoJSONSource>(viaSource)?.setData({
      type: 'FeatureCollection',
      features: segments.flatMap((segment, s) =>
        segment.via.map((point, index) => ({
          type: 'Feature' as const,
          geometry: { type: 'Point' as const, coordinates: point },
          properties: { segment: s, index },
        })),
      ),
    })
  }, [map, segments, selected])

  const failed = segments.filter((s) => s.failed).length

  return (
    <Paper withBorder shadow="xs" p="xs" maw={260}>
      <Text size="xs" c="dimmed">
        Povuci crtu da trasa prođe drugom ulicom; povuci bijelu točku da je pomakneš, desni klik je
        briše.
      </Text>
      {failed > 0 && (
        <Text size="xs" c="red" mt={4}>
          {failed === 1 ? '1 dionica nema' : `${failed} dionica nema`} put po mreži (crveno).
        </Text>
      )}
    </Paper>
  )
}

const emptyLines: FeatureCollection<LineString> = { type: 'FeatureCollection', features: [] }
const emptyPoints: FeatureCollection<Point> = { type: 'FeatureCollection', features: [] }
