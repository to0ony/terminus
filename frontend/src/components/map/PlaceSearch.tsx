import { Autocomplete, Loader } from '@mantine/core'
import { useDebouncedValue } from '@mantine/hooks'
import { IconSearch } from '@tabler/icons-react'
import { useQuery } from '@tanstack/react-query'
import type { Map } from 'maplibre-gl'
import { useState } from 'react'

/** Mjesto iz Photon geokodera (OpenStreetMap podaci, bez ključa). */
interface Place {
  label: string
  center: [number, number]
  /** [zapad, sjever, istok, jug], ako mjesto ima površinu. */
  extent: [number, number, number, number] | null
}

interface PhotonFeature {
  geometry: { coordinates: [number, number] }
  properties: {
    name?: string
    street?: string
    housenumber?: string
    city?: string
    county?: string
    country?: string
    extent?: [number, number, number, number]
  }
}

async function searchPlaces(query: string, map: Map, signal: AbortSignal): Promise<Place[]> {
  const center = map.getCenter()
  const params = new URLSearchParams({
    q: query,
    limit: '6',
    // Nazivi na jeziku mjesta (Hrvatska, ne Croatia).
    lang: 'default',
    // Bliža mjesta imaju prednost.
    lat: center.lat.toFixed(4),
    lon: center.lng.toFixed(4),
  })
  const response = await fetch(`https://photon.komoot.io/api/?${params}`, { signal })
  if (!response.ok) {
    return []
  }
  const body = (await response.json()) as { features: PhotonFeature[] }
  const seen = new Set<string>()
  return body.features.flatMap((feature) => {
    const p = feature.properties
    const street = p.street ? [p.street, p.housenumber].filter(Boolean).join(' ') : null
    const parts = [p.name ?? street, p.city ?? p.county, p.country].filter(Boolean)
    const label = [...new Set(parts)].join(', ')
    if (!label || seen.has(label)) {
      return []
    }
    seen.add(label)
    return [{ label, center: feature.geometry.coordinates, extent: p.extent ?? null }]
  })
}

/** Tražilica mjesta i adresa preko karte; odabir pomiče kartu na njega. */
export function PlaceSearch({ map }: { map: Map }) {
  const [value, setValue] = useState('')
  const [query] = useDebouncedValue(value.trim(), 350)
  const places = useQuery({
    queryKey: ['places', query],
    queryFn: ({ signal }) => searchPlaces(query, map, signal),
    enabled: query.length >= 3,
    staleTime: 5 * 60_000,
  })

  const go = (label: string) => {
    const place = places.data?.find((p) => p.label === label)
    if (!place) {
      return
    }
    if (place.extent) {
      const [west, north, east, south] = place.extent
      map.fitBounds(
        [
          [west, south],
          [east, north],
        ],
        { padding: 32, maxZoom: 16 },
      )
    } else {
      map.flyTo({ center: place.center, zoom: 16 })
    }
  }

  return (
    <Autocomplete
      w={260}
      size="xs"
      placeholder="Traži mjesto ili adresu"
      aria-label="Traži mjesto ili adresu"
      leftSection={places.isFetching ? <Loader size={12} /> : <IconSearch size={14} />}
      value={value}
      onChange={setValue}
      data={places.data?.map((p) => p.label) ?? []}
      // Photon već filtrira; Mantine ne smije dodatno skrivati rezultate.
      filter={({ options }) => options}
      onOptionSubmit={go}
      comboboxProps={{ withinPortal: false }}
    />
  )
}
