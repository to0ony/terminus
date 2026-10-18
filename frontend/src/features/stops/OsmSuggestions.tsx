import { ActionIcon, Button, Group, Paper, Progress, Stack, Text } from '@mantine/core'
import { notifications } from '@mantine/notifications'
import { IconX } from '@tabler/icons-react'
import { useQueryClient } from '@tanstack/react-query'
import type { FeatureCollection, Point } from 'geojson'
import type { GeoJSONSource, Map, MapLayerMouseEvent } from 'maplibre-gl'
import { useEffect, useRef, useState } from 'react'
import { api } from '@/api/client'
import { errorMessage, unwrap } from '@/api/errors'
import { maxOsmArea, type OsmStop, overpassQuery, parseOverpass, withoutExisting } from './osm'
import { createStop, stopKeys } from './queries'
import { suggestStopCode } from './stopCode'

const source = 'terminus-osm'
const layer = 'terminus-osm-stops'

/**
 * Stajališta iz OpenStreetMapa za vidljivi dio karte (javni Overpass API):
 * klik ih odabire, a odabrana postaju stanice. Ona koja već postoje (unutar
 * 20 m) se ne nude.
 */
export function OsmSuggestions({ map, onClose }: { map: Map; onClose: () => void }) {
  const queryClient = useQueryClient()
  const [candidates, setCandidates] = useState<OsmStop[] | null>(null)
  const [skipped, setSkipped] = useState({ existing: 0, unnamed: 0 })
  const [selected, setSelected] = useState<Set<number>>(new Set())
  const [loading, setLoading] = useState(false)
  const [progress, setProgress] = useState<{ done: number; total: number } | null>(null)
  const [tooLarge, setTooLarge] = useState(false)

  // Sloj kandidata iznad stanica; uklanja se pri zatvaranju.
  const toggleRef = useRef((_id: number) => {})
  useEffect(() => {
    toggleRef.current = (id: number) =>
      setSelected((current) => {
        const next = new Set(current)
        if (!next.delete(id)) {
          next.add(id)
        }
        return next
      })
  })
  useEffect(() => {
    map.addSource(source, { type: 'geojson', data: empty })
    map.addLayer({
      id: layer,
      type: 'circle',
      source,
      paint: {
        'circle-radius': ['interpolate', ['linear'], ['zoom'], 12, 5, 17, 9],
        'circle-color': ['case', ['get', 'selected'], '#e8590c', '#adb5bd'],
        'circle-stroke-color': ['case', ['get', 'selected'], '#ffffff', '#495057'],
        'circle-stroke-width': 2,
      },
    })
    map.addLayer({
      id: `${layer}-labels`,
      type: 'symbol',
      source,
      minzoom: 15,
      layout: {
        'text-field': ['get', 'label'],
        'text-font': ['Noto Sans Regular'],
        'text-size': 11,
        'text-offset': [0, 1.2],
        'text-anchor': 'top',
      },
      paint: { 'text-color': '#495057', 'text-halo-color': '#ffffff', 'text-halo-width': 1.5 },
    })
    const onClick = (event: MapLayerMouseEvent) => {
      const id = event.features?.[0]?.properties.id
      if (id !== undefined) {
        toggleRef.current(Number(id))
      }
    }
    const onEnter = () => (map.getCanvas().style.cursor = 'pointer')
    const onLeave = () => (map.getCanvas().style.cursor = '')
    map.on('click', layer, onClick)
    map.on('mouseenter', layer, onEnter)
    map.on('mouseleave', layer, onLeave)
    return () => {
      map.off('click', layer, onClick)
      map.off('mouseenter', layer, onEnter)
      map.off('mouseleave', layer, onLeave)
      if (map.getStyle()) {
        map.removeLayer(`${layer}-labels`)
        map.removeLayer(layer)
        map.removeSource(source)
      }
    }
  }, [map])

  useEffect(() => {
    map.getSource<GeoJSONSource>(source)?.setData({
      type: 'FeatureCollection',
      features: (candidates ?? []).map((c) => ({
        type: 'Feature',
        geometry: { type: 'Point', coordinates: [c.longitude, c.latitude] },
        properties: {
          id: c.id,
          selected: selected.has(c.id),
          label: c.platformCode ? `${c.name} · ${c.platformCode}` : c.name,
        },
      })),
    })
  }, [map, candidates, selected])

  const load = async () => {
    const bounds = map.getBounds()
    const [south, west, north, east] = [
      bounds.getSouth(),
      bounds.getWest(),
      bounds.getNorth(),
      bounds.getEast(),
    ]
    if (north - south > maxOsmArea || east - west > maxOsmArea) {
      setTooLarge(true)
      return
    }
    setTooLarge(false)
    setLoading(true)
    try {
      const response = await fetch('https://overpass-api.de/api/interpreter', {
        method: 'POST',
        body: new URLSearchParams({ data: overpassQuery(south, west, north, east) }),
      })
      if (!response.ok) {
        throw new Error(`Overpass ${response.status}`)
      }
      const parsed = parseOverpass(await response.json())
      // Postojeće stanice istog područja (malo šire, za stajališta na rubu).
      const existing = unwrap(
        await api.GET('/api/stops/area', {
          params: {
            query: {
              MinLatitude: south - 0.001,
              MinLongitude: west - 0.001,
              MaxLatitude: north + 0.001,
              MaxLongitude: east + 0.001,
            },
          },
        }),
      )
      const fresh = withoutExisting(parsed.stops, existing.stops)
      setCandidates(fresh)
      setSelected(new Set())
      setSkipped({ existing: parsed.stops.length - fresh.length, unnamed: parsed.unnamed })
    } catch (error) {
      notifications.show({
        color: 'red',
        title: 'Stajališta iz OSM-a nisu učitana',
        message:
          error instanceof Error && error.message.startsWith('Overpass')
            ? 'OpenStreetMap poslužitelj je zauzet. Pokušaj za minutu.'
            : errorMessage(error),
      })
    } finally {
      setLoading(false)
    }
  }

  const add = async () => {
    const chosen = (candidates ?? []).filter((c) => selected.has(c.id))
    setProgress({ done: 0, total: chosen.length })
    const added = new Set<number>()
    let failed: unknown = null
    // Nekoliko zahtjeva odjednom: brže, a ne zaguši poslužitelj.
    const queue = [...chosen]
    const worker = async () => {
      for (let next = queue.shift(); next; next = queue.shift()) {
        try {
          await createStop(
            {
              name: next.name,
              code: suggestStopCode(next.name, next.platformCode),
              platformCode: next.platformCode,
              publicCode: next.publicCode,
              latitude: Math.round(next.latitude * 1e6) / 1e6,
              longitude: Math.round(next.longitude * 1e6) / 1e6,
              locationType: 0,
              wheelchairBoarding: next.wheelchair,
              isActive: true,
            },
            true,
          )
          added.add(next.id)
        } catch (error) {
          failed = error
        }
        setProgress((p) => p && { ...p, done: p.done + 1 })
      }
    }
    await Promise.all([worker(), worker(), worker(), worker()])
    setProgress(null)
    setCandidates((current) => current?.filter((c) => !added.has(c.id)) ?? null)
    setSelected(new Set())
    void queryClient.invalidateQueries({ queryKey: stopKeys.all })
    notifications.show(
      failed
        ? {
            color: 'yellow',
            title: `Dodano ${added.size} od ${chosen.length} stanica`,
            message: errorMessage(failed),
          }
        : { color: 'green', message: `Dodano ${added.size} stanica iz OpenStreetMapa.` },
    )
  }

  const count = candidates?.length ?? 0

  return (
    <Paper withBorder shadow="sm" p="sm" w={300}>
      <Stack gap="xs">
        <Group justify="space-between" wrap="nowrap">
          <Text size="sm" fw={600}>
            Stajališta iz OpenStreetMapa
          </Text>
          <ActionIcon variant="subtle" size="sm" onClick={onClose} aria-label="Zatvori">
            <IconX size={14} />
          </ActionIcon>
        </Group>
        {candidates === null ? (
          <Text size="xs" c="dimmed">
            Pomakni kartu na područje (najviše oko 25 km), pa učitaj stajališta. Klik na sivu točku
            je odabire.
          </Text>
        ) : (
          <Text size="xs" c="dimmed">
            {count} novih stajališta, odabrano {selected.size}.
            {skipped.existing > 0 && ` ${skipped.existing} već postoji.`}
            {skipped.unnamed > 0 && ` ${skipped.unnamed} bez naziva preskočeno.`}
          </Text>
        )}
        {tooLarge && (
          <Text size="xs" c="red">
            Područje je preveliko; približi kartu.
          </Text>
        )}
        {progress ? (
          <Stack gap={4}>
            <Progress value={(progress.done / progress.total) * 100} />
            <Text size="xs" c="dimmed">
              Dodano {progress.done} od {progress.total}
            </Text>
          </Stack>
        ) : (
          <Group gap="xs">
            <Button size="xs" variant="default" loading={loading} onClick={() => void load()}>
              {candidates === null ? 'Učitaj za ovo područje' : 'Učitaj ponovno'}
            </Button>
            {count > 0 && (
              <Button
                size="xs"
                variant="subtle"
                onClick={() =>
                  setSelected(
                    selected.size === count ? new Set() : new Set(candidates!.map((c) => c.id)),
                  )
                }
              >
                {selected.size === count ? 'Poništi odabir' : 'Odaberi sve'}
              </Button>
            )}
          </Group>
        )}
        {selected.size > 0 && !progress && (
          <Button size="xs" onClick={() => void add()}>
            Dodaj odabrane ({selected.size})
          </Button>
        )}
      </Stack>
    </Paper>
  )
}

const empty: FeatureCollection<Point> = { type: 'FeatureCollection', features: [] }
