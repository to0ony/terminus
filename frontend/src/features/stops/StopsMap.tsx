import { Badge, Box, type BoxProps } from '@mantine/core'
import type { FeatureCollection, Point } from 'geojson'
import { type GeoJSONSource, type Map, Popup } from 'maplibre-gl'
import { useEffect, useRef, useState } from 'react'
import { MapView } from '@/components/map/MapView'
import { type GeoBounds, type MapStop, type StopAreaParams, useStopsInArea } from './queries'

const source = 'terminus-stops'

interface StopsMapProps extends BoxProps {
  /** Početno područje (GET /api/stops/extent). */
  extent: GeoBounds | null
  /** Istaknuta stanica. */
  selectedId?: string | null
  onSelect?: (stop: MapStop) => void
}

/**
 * Stanice vidljivog dijela karte; nakon svakog pomicanja traže se iznova.
 * Kolodvori su veći, neaktivne stanice sive.
 */
export function StopsMap({ extent, selectedId = null, onSelect, ...boxProps }: StopsMapProps) {
  const [map, setMap] = useState<Map | null>(null)
  const [area, setArea] = useState<StopAreaParams | null>(null)
  const stops = useStopsInArea(area)
  const onSelectRef = useRef(onSelect)
  useEffect(() => {
    onSelectRef.current = onSelect
  })
  const byId = useRef(new globalThis.Map<string, MapStop>())

  const handleReady = (instance: Map) => {
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
      instance.getCanvas().style.cursor = ''
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
      {stops.data?.truncated && (
        <Badge pos="absolute" top={10} left={10} color="yellow" variant="filled" tt="none">
          Prikazan je dio stanica. Približi kartu za sve.
        </Badge>
      )}
    </Box>
  )
}

const emptyCollection: FeatureCollection<Point> = { type: 'FeatureCollection', features: [] }

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
