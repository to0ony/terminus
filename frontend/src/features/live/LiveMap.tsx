import type { BoxProps } from '@mantine/core'
import type { FeatureCollection, LineString, Point } from 'geojson'
import { type GeoJSONSource, LngLatBounds, type Map, Popup } from 'maplibre-gl'
import { type RefObject, useEffect, useRef, useState } from 'react'
import { MapView } from '@/components/map/MapView'
import type { MapStyleSetting } from '@/components/map/mapStyles'
import type { GeoBounds, MapStop, StopAreaParams } from '@/features/stops/queries'
import { useStopsInArea } from '@/features/stops/queries'
import { busIcon, stopColor } from './icons'
import type { LiveRouteDay } from './queries'
import { vehicleAt } from './simulation'
import { routeLines, type Vehicle } from './tracks'

/** Karta uživo pamti svoju podlogu; zadana je detaljna (Liberty, sa zgradama). */
const liveMapStyle: MapStyleSetting = {
  key: 'terminus-live-map-style',
  defaultValue: 'liberty',
}

const stopsSource = 'live-stops'
const routeSource = 'live-route'
const routeStopsSource = 'live-route-stops'
const vehiclesSource = 'live-vehicles'
const stopIcon = 'live-stop'
const vehicleIconPrefix = 'live-vehicle-'

/** Klik na ove slojeve bira stanicu. */
const stopLayers = ['live-stops-dot', 'live-stops-icon', 'live-route-stops']

interface LiveMapProps extends BoxProps {
  extent: GeoBounds | null
  selectedStopId: string | null
  onSelectStop: (stop: MapStop) => void
  /** Odabrana linija tog dana; null bez linije. */
  day: LiveRouteDay | null
  /** Kad se promijeni, karta se prilagodi trasi (npr. drugi smjer); null bez toga. */
  focusKey: string | null
  vehicles: Vehicle[]
  timeRef: RefObject<number>
  selectedVehicle: string | null
  onSelectVehicle: (key: string) => void
}

/**
 * Karta u stilu Google karata: plave stanice s autobusom, trasa odabrane linije
 * u njezinoj boji i okrugla vozila koja se kreću prema voznom redu.
 */
export function LiveMap({
  extent,
  selectedStopId,
  onSelectStop,
  day,
  focusKey,
  vehicles,
  timeRef,
  selectedVehicle,
  onSelectVehicle,
  ...boxProps
}: LiveMapProps) {
  const [map, setMap] = useState<Map | null>(null)
  const [area, setArea] = useState<StopAreaParams | null>(null)
  const stops = useStopsInArea(area)
  const byId = useRef(new globalThis.Map<string, MapStop>())
  const handlers = useRef({ onSelectStop, onSelectVehicle })
  useEffect(() => {
    handlers.current = { onSelectStop, onSelectVehicle }
  })

  const handleReady = (instance: Map) => {
    // Ikone se crtaju odmah; karta ih traži i nakon promjene podloge.
    instance.on('styleimagemissing', (event) => {
      const id = event.id
      if (id === stopIcon) {
        const { image, pixelRatio } = busIcon(stopColor, 18)
        instance.addImage(id, image, { pixelRatio })
      } else if (id.startsWith(vehicleIconPrefix)) {
        const { image, pixelRatio } = busIcon(`#${id.slice(vehicleIconPrefix.length)}`, 30)
        instance.addImage(id, image, { pixelRatio })
      }
    })

    instance.addSource(routeSource, { type: 'geojson', data: emptyLines })
    instance.addLayer({
      id: 'live-route-casing',
      type: 'line',
      source: routeSource,
      layout: { 'line-cap': 'round', 'line-join': 'round' },
      paint: {
        'line-color': '#ffffff',
        'line-width': ['interpolate', ['linear'], ['zoom'], 10, 5, 16, 11],
      },
    })
    instance.addLayer({
      id: 'live-route-line',
      type: 'line',
      source: routeSource,
      layout: { 'line-cap': 'round', 'line-join': 'round' },
      paint: {
        'line-color': ['get', 'color'],
        'line-width': ['interpolate', ['linear'], ['zoom'], 10, 3, 16, 7],
      },
    })

    instance.addSource(stopsSource, { type: 'geojson', data: emptyPoints })
    instance.addLayer({
      id: 'live-stop-selected',
      type: 'circle',
      source: stopsSource,
      filter: ['==', ['get', 'id'], ''],
      paint: {
        'circle-radius': 16,
        'circle-color': stopColor,
        'circle-opacity': 0.2,
        'circle-stroke-color': stopColor,
        'circle-stroke-width': 2,
      },
    })
    instance.addLayer({
      id: 'live-stops-dot',
      type: 'circle',
      source: stopsSource,
      maxzoom: 15,
      paint: {
        'circle-radius': ['interpolate', ['linear'], ['zoom'], 10, 2.5, 15, 5],
        'circle-color': stopColor,
        'circle-stroke-color': '#ffffff',
        'circle-stroke-width': 1,
      },
    })
    instance.addLayer({
      id: 'live-stops-icon',
      type: 'symbol',
      source: stopsSource,
      minzoom: 15,
      layout: {
        'icon-image': stopIcon,
        'icon-allow-overlap': true,
        'text-field': ['get', 'name'],
        'text-font': ['Noto Sans Regular'],
        'text-size': 11,
        'text-offset': [0, 1.3],
        'text-anchor': 'top',
        'text-optional': true,
      },
      paint: { 'text-color': stopColor, 'text-halo-color': '#ffffff', 'text-halo-width': 1.5 },
    })

    instance.addSource(routeStopsSource, { type: 'geojson', data: emptyPoints })
    instance.addLayer({
      id: 'live-route-stops',
      type: 'circle',
      source: routeStopsSource,
      paint: {
        'circle-radius': ['interpolate', ['linear'], ['zoom'], 10, 3, 16, 6],
        'circle-color': '#ffffff',
        'circle-stroke-color': ['get', 'color'],
        'circle-stroke-width': ['interpolate', ['linear'], ['zoom'], 10, 1.5, 16, 3],
      },
    })
    instance.addLayer({
      id: 'live-route-stop-labels',
      type: 'symbol',
      source: routeStopsSource,
      minzoom: 13,
      layout: {
        'text-field': ['get', 'name'],
        'text-font': ['Noto Sans Regular'],
        'text-size': 12,
        'text-offset': [0.9, 0],
        'text-anchor': 'left',
        'text-optional': true,
      },
      paint: { 'text-color': '#202124', 'text-halo-color': '#ffffff', 'text-halo-width': 1.5 },
    })

    instance.addSource(vehiclesSource, { type: 'geojson', data: emptyPoints })
    instance.addLayer({
      id: 'live-vehicle-selected',
      type: 'circle',
      source: vehiclesSource,
      filter: ['==', ['get', 'key'], ''],
      paint: { 'circle-radius': 22, 'circle-color': '#000000', 'circle-opacity': 0.15 },
    })
    instance.addLayer({
      id: 'live-vehicles',
      type: 'symbol',
      source: vehiclesSource,
      layout: {
        'icon-image': ['get', 'icon'],
        'icon-allow-overlap': true,
        'icon-ignore-placement': true,
      },
    })

    const popup = new Popup({ closeButton: false, closeOnClick: false, offset: 10 })
    for (const layer of stopLayers) {
      instance.on('mouseenter', layer, (event) => {
        instance.getCanvas().style.cursor = 'pointer'
        const id = String(event.features?.[0]?.properties.id)
        const stop = byId.current.get(id)
        const name = stop?.name ?? event.features?.[0]?.properties.name
        if (name && event.features?.[0]?.geometry.type === 'Point') {
          popup
            .setLngLat(event.features[0].geometry.coordinates as [number, number])
            .setText(String(name))
            .addTo(instance)
        }
      })
      instance.on('mouseleave', layer, () => {
        instance.getCanvas().style.cursor = ''
        popup.remove()
      })
      instance.on('click', layer, (event) => {
        const properties = event.features?.[0]?.properties
        const stop = properties && byId.current.get(String(properties.id))
        if (stop) {
          handlers.current.onSelectStop(stop)
        }
      })
    }
    instance.on('mouseenter', 'live-vehicles', () => {
      instance.getCanvas().style.cursor = 'pointer'
    })
    instance.on('mouseleave', 'live-vehicles', () => {
      instance.getCanvas().style.cursor = ''
    })
    instance.on('click', 'live-vehicles', (event) => {
      const key = event.features?.[0]?.properties.key
      if (key) {
        handlers.current.onSelectVehicle(String(key))
      }
    })

    const updateArea = () => setArea(visibleArea(instance))
    instance.on('moveend', updateArea)
    updateArea()
    setMap(instance)
  }

  // Stanice vidljivog dijela karte; stanice linije pamte se i kad nisu na karti.
  useEffect(() => {
    if (!map || !stops.data) {
      return
    }
    for (const stop of stops.data.stops) {
      byId.current.set(stop.id, stop)
    }
    map.getSource<GeoJSONSource>(stopsSource)?.setData(stopFeatures(stops.data.stops))
  }, [map, stops.data])

  useEffect(() => {
    map?.setFilter('live-stop-selected', ['==', ['get', 'id'], selectedStopId ?? ''])
  }, [map, selectedStopId])

  // Odabrano vozilo: istakne se i karta se jednom pomakne do njega.
  const flownTo = useRef<string | null>(null)
  useEffect(() => {
    map?.setFilter('live-vehicle-selected', ['==', ['get', 'key'], selectedVehicle ?? ''])
    if (flownTo.current === selectedVehicle) {
      return
    }
    flownTo.current = selectedVehicle
    const vehicle = vehicles.find((v) => v.key === selectedVehicle)
    const state = vehicle && vehicleAt(vehicle.track, timeRef.current)
    if (map && state) {
      map.easeTo({ center: state.position, zoom: Math.max(map.getZoom(), 15), duration: 600 })
    }
  }, [map, selectedVehicle, vehicles, timeRef])

  // Trasa i stanice linije; ostale stanice se utišaju.
  useEffect(() => {
    if (!map) {
      return
    }
    const color = `#${day?.route.color ?? '1a73e8'}`
    map
      .getSource<GeoJSONSource>(routeSource)
      ?.setData(day ? lineFeatures(routeLines(day, vehicles), color) : emptyLines)
    map
      .getSource<GeoJSONSource>(routeStopsSource)
      ?.setData(day ? stopFeatures(day.stops, color) : emptyPoints)
    for (const stop of day?.stops ?? []) {
      byId.current.set(stop.id, stop)
    }
    map.setPaintProperty('live-stops-dot', 'circle-opacity', day ? 0.35 : 1)
    map.setPaintProperty('live-stops-dot', 'circle-stroke-opacity', day ? 0.35 : 1)
    map.setPaintProperty('live-stops-icon', 'icon-opacity', day ? 0.35 : 1)
  }, [map, day, vehicles])

  // Nova trasa: karta joj se prilagodi (ne i kad se promijeni samo dan).
  const fittedRoute = useRef<string | null>(null)
  useEffect(() => {
    if (!map || !day || day.stops.length === 0 || fittedRoute.current === focusKey) {
      return
    }
    fittedRoute.current = focusKey
    const bounds = new LngLatBounds()
    for (const stop of day.stops) {
      if (stop.latitude !== null && stop.longitude !== null) {
        bounds.extend([stop.longitude, stop.latitude])
      }
    }
    map.fitBounds(bounds, {
      padding: { top: 60, bottom: 160, left: 380, right: 60 },
      maxZoom: 15,
      duration: 800,
    })
  }, [map, day, focusKey])

  // Vozila: svaki okvir iz točnog vremena sata.
  useEffect(() => {
    if (!map) {
      return
    }
    const icon = `${vehicleIconPrefix}${day?.route.color ?? '1a73e8'}`
    let frame = 0
    const draw = () => {
      const time = timeRef.current
      const features = vehicles.flatMap((vehicle) => {
        const state = vehicleAt(vehicle.track, time)
        return state
          ? [
              {
                type: 'Feature' as const,
                geometry: { type: 'Point' as const, coordinates: state.position },
                properties: { key: vehicle.key, icon },
              },
            ]
          : []
      })
      map.getSource<GeoJSONSource>(vehiclesSource)?.setData({ type: 'FeatureCollection', features })
      frame = requestAnimationFrame(draw)
    }
    frame = requestAnimationFrame(draw)
    return () => cancelAnimationFrame(frame)
  }, [map, day, vehicles, timeRef])

  return (
    <MapView
      {...boxProps}
      styleSetting={liveMapStyle}
      bounds={
        extent
          ? [extent.minLongitude, extent.minLatitude, extent.maxLongitude, extent.maxLatitude]
          : undefined
      }
      onReady={handleReady}
    />
  )
}

const emptyPoints: FeatureCollection<Point> = { type: 'FeatureCollection', features: [] }
const emptyLines: FeatureCollection<LineString> = { type: 'FeatureCollection', features: [] }

function stopFeatures(stops: MapStop[], color?: string): FeatureCollection<Point> {
  return {
    type: 'FeatureCollection',
    features: stops
      .filter((s) => s.latitude !== null && s.longitude !== null)
      .map((s) => ({
        type: 'Feature',
        geometry: { type: 'Point', coordinates: [s.longitude!, s.latitude!] },
        properties: { id: s.id, name: s.name, color: color ?? stopColor },
      })),
  }
}

function lineFeatures(lines: [number, number][][], color: string): FeatureCollection<LineString> {
  return {
    type: 'FeatureCollection',
    features: lines.map((coordinates) => ({
      type: 'Feature',
      geometry: { type: 'LineString', coordinates },
      properties: { color },
    })),
  }
}

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
