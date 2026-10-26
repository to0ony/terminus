import 'maplibre-gl/dist/maplibre-gl.css'
import { Box, type BoxProps, useComputedColorScheme } from '@mantine/core'
import {
  type LngLatBoundsLike,
  Map,
  NavigationControl,
  setWorkerUrl,
  type StyleSpecification,
} from 'maplibre-gl'
import workerUrl from 'maplibre-gl/dist/maplibre-gl-worker.mjs?url'
import { useEffect, useRef, useState } from 'react'
import { MapStylePicker } from './MapStylePicker'
import { type MapStyleSetting, mapStyleUrl, sharedMapStyle, useMapStyle } from './mapStyles'

// MapLibre traži worker pokraj svog modula, a Vite ga pakira drugamo; ovako
// worker postaje zasebna datoteka i u razvoju i u buildu.
setWorkerUrl(workerUrl)

interface MapViewProps extends BoxProps {
  /** Početno područje; bez njega cijeli svijet. */
  bounds?: LngLatBoundsLike
  /** Karta je spremna za dodavanje izvora i slojeva. */
  onReady?: (map: Map) => void
  /** Vlastita zapamćena podloga; bez nje zajednička. */
  styleSetting?: MapStyleSetting
}

/**
 * MapLibre karta s kontrolama zuma i izborom podloge (zadano prema temi:
 * svijetla ili tamna).
 */
export function MapView({
  bounds,
  onReady,
  styleSetting = sharedMapStyle,
  ...boxProps
}: MapViewProps) {
  const container = useRef<HTMLDivElement>(null)
  const [map, setMap] = useState<Map | null>(null)
  const colorScheme = useComputedColorScheme('light')
  const [style] = useMapStyle(styleSetting)
  const styleUrl = mapStyleUrl(style, colorScheme)

  // Karta se stvara jednom; početno područje, podloga i onReady čitaju se tada.
  const initial = useRef({ bounds, styleUrl, onReady })

  useEffect(() => {
    const { bounds, styleUrl, onReady } = initial.current
    const instance = new Map({
      container: container.current!,
      style: styleUrl,
      bounds,
      fitBoundsOptions: { padding: 32 },
      attributionControl: { compact: true },
      dragRotate: false,
      pitchWithRotate: false,
    })
    instance.addControl(new NavigationControl({ showCompass: false }), 'top-right')
    instance.touchZoomRotate.disableRotation()
    instance.once('load', () => {
      setMap(instance)
      onReady?.(instance)
    })
    return () => instance.remove()
  }, [])

  // Promjena podloge ili teme mijenja samo podlogu; izvori i slojevi stranice ostaju.
  useEffect(() => {
    if (map && initial.current.styleUrl !== styleUrl) {
      initial.current.styleUrl = styleUrl
      map.setStyle(styleUrl, { transformStyle: keepOwnLayers })
    }
  }, [map, styleUrl])

  return (
    <Box pos="relative" {...boxProps}>
      <Box ref={container} pos="absolute" inset={0} />
      <Box pos="absolute" left={10} bottom={10}>
        <MapStylePicker setting={styleSetting} />
      </Box>
    </Box>
  )
}

function keepOwnLayers(
  previous: StyleSpecification | undefined,
  next: StyleSpecification,
): StyleSpecification {
  if (!previous) {
    return next
  }
  const ownSources = Object.fromEntries(
    Object.entries(previous.sources).filter(([id]) => !(id in next.sources)),
  )
  const ownLayers = previous.layers.filter(
    (layer) => 'source' in layer && typeof layer.source === 'string' && layer.source in ownSources,
  )
  return {
    ...next,
    sources: { ...next.sources, ...ownSources },
    layers: [...next.layers, ...ownLayers],
  }
}
