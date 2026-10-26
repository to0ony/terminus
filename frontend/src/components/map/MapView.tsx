import 'maplibre-gl/dist/maplibre-gl.css'
import { Box, type BoxProps, useComputedColorScheme } from '@mantine/core'
import {
  GeolocateControl,
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
  /** Razmak gumba za podlogu od dna (npr. iznad ploče na mobitelu). */
  pickerBottom?: number
  /** Gumb "moja lokacija" ispod zuma (traži dopuštenje preglednika i HTTPS). */
  geolocate?: boolean
}

/**
 * MapLibre karta s kontrolama zuma i izborom podloge (zadano prema temi:
 * svijetla ili tamna).
 */
export function MapView({
  bounds,
  onReady,
  styleSetting = sharedMapStyle,
  pickerBottom = 10,
  geolocate = false,
  ...boxProps
}: MapViewProps) {
  const container = useRef<HTMLDivElement>(null)
  const [map, setMap] = useState<Map | null>(null)
  const colorScheme = useComputedColorScheme('light')
  const [style] = useMapStyle(styleSetting)
  const styleUrl = mapStyleUrl(style, colorScheme)

  // Karta se stvara jednom; početno područje, podloga i onReady čitaju se tada.
  const initial = useRef({ bounds, styleUrl, onReady, geolocate })

  useEffect(() => {
    const { bounds, styleUrl, onReady, geolocate } = initial.current
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
    if (geolocate) {
      // Plava točka korisnika; karta ga prati dok se ne pomakne ručno.
      instance.addControl(
        new GeolocateControl({
          positionOptions: { enableHighAccuracy: true },
          trackUserLocation: true,
          fitBoundsOptions: { maxZoom: 16 },
        }),
        'top-right',
      )
    }
    instance.touchZoomRotate.disableRotation()
    // Atribucija podloge (© OpenStreetMap…) počinje skupljena kao gumb "i";
    // MapLibre je inače prvi put prikaže raširenu. Skuplja se kad je popunjena.
    instance.once('idle', () => {
      const attribution = container.current?.querySelector(
        '.maplibregl-ctrl-attrib.maplibregl-compact',
      )
      attribution?.classList.remove('maplibregl-compact-show')
      attribution?.removeAttribute('open')
    })
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
      <Box pos="absolute" left={10} bottom={pickerBottom}>
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
