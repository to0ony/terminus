import { Badge, Box, type BoxProps } from '@mantine/core'
import type { FeatureCollection, LineString, Point } from 'geojson'
import { type GeoJSONSource, type Map, Popup } from 'maplibre-gl'
import { useEffect, useRef, useState } from 'react'
import { MapView } from '@/components/map/MapView'
import { PlaceSearch } from '@/components/map/PlaceSearch'
import { type GeoBounds, type MapStop, type StopAreaParams, useStopsInArea } from './queries'

const source = 'terminus-stops'
const overlaySource = 'terminus-overlay'

/** Crta preko karte, npr. varijanta linije: redoslijed stanica s brojevima. */
export interface MapOverlay {
  /** Točke crte redom, [dužina, širina]. */
  line: [number, number][]
  /** Označene točke (npr. redni broj stanice u varijanti). */
  points: { position: [number, number]; label: string }[]
}

interface StopsMapProps extends BoxProps {
  /** Početno područje (GET /api/stops/extent). */
  extent: GeoBounds | null
  /** Istaknuta stanica. */
  selectedId?: string | null
  onSelect?: (stop: MapStop) => void
  overlay?: MapOverlay | null
  /** Klik na kartu izvan stanica (npr. nova stanica); pokazivač je tada križić. */
  onMapClick?: (position: { latitude: number; longitude: number }) => void
  /** Tražilica mjesta gore lijevo. */
  searchable?: boolean
}

/**
 * Stanice vidljivog dijela karte; nakon svakog pomicanja traže se iznova.
 * Kolodvori su veći, neaktivne stanice sive.
 */
export function StopsMap({
  extent,
  selectedId = null,
  onSelect,
  overlay = null,
  onMapClick,
  searchable = false,
  ...boxProps
}: StopsMapProps) {
  const [map, setMap] = useState<Map | null>(null)
  const [area, setArea] = useState<StopAreaParams | null>(null)
  const stops = useStopsInArea(area)
  const onSelectRef = useRef(onSelect)
  const onMapClickRef = useRef(onMapClick)
  useEffect(() => {
    onSelectRef.current = onSelect
    onMapClickRef.current = onMapClick
  })
  const byId = useRef(new globalThis.Map<string, MapStop>())

  const handleReady = (instance: Map) => {
    // Crta ispod stanica, da se stanice i dalje mogu kliknuti.
    instance.addSource(overlaySource, { type: 'geojson', data: emptyOverlay })
    instance.addLayer({
      id: 'terminus-overlay-line',
      type: 'line',
      source: overlaySource,
      filter: ['==', ['geometry-type'], 'LineString'],
      layout: { 'line-cap': 'round', 'line-join': 'round' },
      paint: { 'line-color': '#e8590c', 'line-width': 4, 'line-opacity': 0.8 },
    })
    instance.addSource(source, { type: 'geojson', data: emptyCollection })
    instance.addLayer({
      id: 'terminus-stops-selected',
      type: 'circle',
      source,
      filter: ['==', ['get', 'id'], ''],
      paint: {
        'circle-radius': 12,
        'circle-color': '#fa5252',
        'circle-opacity': 0.25,
        'circle-stroke-color': '#fa5252',
        'circle-stroke-width': 2,
      },
    })
    instance.addLayer({
      id: 'terminus-stops',
      type: 'circle',
      source,
      paint: {
        'circle-radius': [
          'interpolate',
          ['linear'],
          ['zoom'],
          10,
          ['case', ['get', 'station'], 4, 2.5],
          16,
          ['case', ['get', 'station'], 9, 6],
        ],
        'circle-color': ['case', ['get', 'active'], '#228be6', '#868e96'],
        'circle-stroke-color': '#ffffff',
        'circle-stroke-width': ['interpolate', ['linear'], ['zoom'], 10, 0.5, 16, 1.5],
      },
    })

    instance.addLayer({
      id: 'terminus-overlay-points',
      type: 'circle',
      source: overlaySource,
      filter: ['==', ['geometry-type'], 'Point'],
      paint: {
        'circle-radius': 9,
        'circle-color': '#e8590c',
        'circle-stroke-color': '#ffffff',
        'circle-stroke-width': 2,
      },
    })
    instance.addLayer({
      id: 'terminus-overlay-labels',
      type: 'symbol',
      source: overlaySource,
      filter: ['==', ['geometry-type'], 'Point'],
      layout: {
        'text-field': ['get', 'label'],
        'text-font': ['Noto Sans Bold'],
        'text-size': 11,
        'text-allow-overlap': true,
      },
      paint: { 'text-color': '#ffffff' },
    })

    instance.on('click', (event) => {
      const onStop = instance.queryRenderedFeatures(event.point, {
        layers: ['terminus-stops', 'terminus-overlay-points'],
      })
      if (onStop.length === 0) {
        onMapClickRef.current?.({ latitude: event.lngLat.lat, longitude: event.lngLat.lng })
      }
    })

    const popup = new Popup({ closeButton: false, closeOnClick: false, offset: 8 })
    instance.on('mouseenter', 'terminus-stops', (event) => {
      instance.getCanvas().style.cursor = 'pointer'
      const feature = event.features?.[0]
      const stop = feature && byId.current.get(String(feature.properties.id))
      if (stop) {
        popup.setLngLat([stop.longitude!, stop.latitude!]).setText(stopLabel(stop)).addTo(instance)
      }
    })
    instance.on('mouseleave', 'terminus-stops', () => {
      instance.getCanvas().style.cursor = onMapClickRef.current ? 'crosshair' : ''
      popup.remove()
    })
    instance.on('click', 'terminus-stops', (event) => {
      const stop = byId.current.get(String(event.features?.[0]?.properties.id))
      if (stop) {
        onSelectRef.current?.(stop)
      }
    })

    const updateArea = () => setArea(visibleArea(instance))
    instance.on('moveend', updateArea)
    updateArea()
    setMap(instance)
  }

  useEffect(() => {
    if (!map || !stops.data) {
      return
    }
    byId.current = new globalThis.Map(stops.data.stops.map((s) => [s.id, s]))
    map.getSource<GeoJSONSource>(source)?.setData(toFeatures(stops.data.stops))
  }, [map, stops.data])

  useEffect(() => {
    map?.setFilter('terminus-stops-selected', ['==', ['get', 'id'], selectedId ?? ''])
  }, [map, selectedId])

  useEffect(() => {
    map?.getSource<GeoJSONSource>(overlaySource)?.setData(overlayFeatures(overlay))
  }, [map, overlay])

  const placing = Boolean(onMapClick)
  useEffect(() => {
    if (map) {
      map.getCanvas().style.cursor = placing ? 'crosshair' : ''
    }
  }, [map, placing])

  return (
    <Box pos="relative" {...boxProps}>
      <MapView
        h="100%"
        bounds={
          extent
            ? [extent.minLongitude, extent.minLatitude, extent.maxLongitude, extent.maxLatitude]
            : undefined
        }
        onReady={handleReady}
      />
      {searchable && map && (
        <Box pos="absolute" top={10} left={10}>
          <PlaceSearch map={map} />
        </Box>
      )}
      {stops.data?.truncated && (
        <Badge
          pos="absolute"
          top={searchable ? 46 : 10}
          left={10}
          color="yellow"
          variant="filled"
          tt="none"
        >
          Prikazan je dio stanica. Približi kartu za sve.
        </Badge>
      )}
    </Box>
  )
}

const emptyCollection: FeatureCollection<Point> = { type: 'FeatureCollection', features: [] }
const emptyOverlay: FeatureCollection<Point | LineString> = {
  type: 'FeatureCollection',
  features: [],
}

function overlayFeatures(overlay: MapOverlay | null): FeatureCollection<Point | LineString> {
  if (!overlay) {
    return emptyOverlay
  }
  return {
    type: 'FeatureCollection',
    features: [
      ...(overlay.line.length > 1
        ? [
            {
              type: 'Feature' as const,
              geometry: { type: 'LineString' as const, coordinates: overlay.line },
              properties: {},
            },
          ]
        : []),
      ...overlay.points.map((point) => ({
        type: 'Feature' as const,
        geometry: { type: 'Point' as const, coordinates: point.position },
        properties: { label: point.label },
      })),
    ],
  }
}

function stopLabel(stop: MapStop) {
  return stop.platformCode ? `${stop.name} · ${stop.platformCode}` : stop.name
}

function toFeatures(stops: MapStop[]): FeatureCollection<Point> {
  return {
    type: 'FeatureCollection',
    features: stops
      .filter((s) => s.latitude !== null && s.longitude !== null)
      .map((s) => ({
        type: 'Feature',
        geometry: { type: 'Point', coordinates: [s.longitude!, s.latitude!] },
        properties: { id: s.id, station: s.locationType === 1, active: s.isActive },
      })),
  }
}

// Rubovi zaokruženi na 4 decimale (~10 m), da sitni pomaci ne ruše cache.
function visibleArea(map: Map): StopAreaParams {
  const bounds = map.getBounds()
  const round = (value: number) => Math.round(value * 1e4) / 1e4
  return {
    MinLatitude: round(Math.max(bounds.getSouth(), -90)),
    MinLongitude: round(Math.max(bounds.getWest(), -180)),
    MaxLatitude: round(Math.min(bounds.getNorth(), 90)),
    MaxLongitude: round(Math.min(bounds.getEast(), 180)),
  }
}
