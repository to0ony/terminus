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

// MapLibre traži worker pokraj svog modula, a Vite ga pakira drugamo; ovako
// worker postaje zasebna datoteka i u razvoju i u buildu.
setWorkerUrl(workerUrl)

// OpenFreeMap: vektorske karte iz OSM-a, bez ključa i bez ograničenja prometa.
const styles = {
  light: 'https://tiles.openfreemap.org/styles/positron',
  dark: 'https://tiles.openfreemap.org/styles/dark',
}

interface MapViewProps extends BoxProps {
  /** Početno područje; bez njega cijeli svijet. */
  bounds?: LngLatBoundsLike
  /** Karta je spremna za dodavanje izvora i slojeva. */
  onReady?: (map: Map) => void
}

/** MapLibre karta s podlogom u boji teme (svijetla ili tamna) i kontrolama zuma. */
export function MapView({ bounds, onReady, ...boxProps }: MapViewProps) {
  const container = useRef<HTMLDivElement>(null)
  const [map, setMap] = useState<Map | null>(null)
  const colorScheme = useComputedColorScheme('light')

  // Karta se stvara jednom; početno područje, tema i onReady čitaju se tada.
  const initial = useRef({ bounds, colorScheme, onReady })

  useEffect(() => {
    const { bounds, colorScheme, onReady } = initial.current
    const instance = new Map({
      container: container.current!,
      style: styles[colorScheme],
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

  // Promjena teme mijenja samo podlogu; izvori i slojevi stranice ostaju.
  useEffect(() => {
    if (map && initial.current.colorScheme !== colorScheme) {
      initial.current.colorScheme = colorScheme
      map.setStyle(styles[colorScheme], { transformStyle: keepOwnLayers })
    }
  }, [map, colorScheme])

  return <Box ref={container} {...boxProps} />
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
