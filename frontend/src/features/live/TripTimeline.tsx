import { Box, Text, UnstyledButton } from '@mantine/core'
import { IconBus, IconChevronDown, IconChevronUp } from '@tabler/icons-react'
import { type CSSProperties, Fragment, type ReactNode, useState } from 'react'
import { formatTime } from './clock'
import classes from './LiveMapPage.module.css'
import { vehicleAt } from './simulation'
import type { Vehicle } from './tracks'

interface TripTimelineProps {
  vehicle: Vehicle
  stopNames: Map<string, string>
  /** Boja linije, s #. */
  color: string
  /** Stanica s koje se gleda i polazak s nje (sekunde), za vožnje koje stanicu obilaze dvaput. */
  fromStopId: string
  fromSeconds: number
  time: number
}

/**
 * Stanice odabranog polaska kao na Google kartama: ova stanica, nekoliko
 * sljedećih i zadnja, s vremenom dolaska i koliko se vozi od ove stanice;
 * ostale (i ranije) stanice otvaraju se na klik. Vozilo u vožnji vidi se na crti.
 */
export function TripTimeline({
  vehicle,
  stopNames,
  color,
  fromStopId,
  fromSeconds,
  time,
}: TripTimelineProps) {
  const [showEarlier, setShowEarlier] = useState(false)
  const [showAll, setShowAll] = useState(false)
  const stopTimes = vehicle.track.stopTimes
  const last = stopTimes.length - 1

  // Ova stanica u vožnji: ista stanica s polaskom najbližim odabranom.
  let from = 0
  let closest = Infinity
  stopTimes.forEach((st, i) => {
    const gap = Math.abs(st.departureTime - fromSeconds)
    if (st.stopId === fromStopId && gap < closest) {
      closest = gap
      from = i
    }
  })

  const state = vehicleAt(vehicle.track, time)
  const start = showEarlier ? 0 : from

  // Nekoliko sljedećih stanica i zadnja; između njih skupljeno, osim na zahtjev.
  const collapse = !showAll && last - from > nextStops + 1
  const hidden = collapse ? { first: from + nextStops + 1, last: last - 1 } : null
  const rows: Row[] = []
  for (let i = start; i <= last; i++) {
    if (hidden && i === hidden.first) {
      rows.push({ kind: 'more', count: hidden.last - hidden.first + 1 })
      i = hidden.last
    } else {
      rows.push({ kind: 'stop', index: i })
    }
  }
  const vehicleHidden =
    hidden !== null &&
    state !== null &&
    state.stopIndex >= hidden.first &&
    state.stopIndex <= hidden.last

  const rail = { '--rail-color': color } as CSSProperties

  return (
    <Box style={rail}>
      {from > 0 && (
        <Toggle open={showEarlier} onClick={() => setShowEarlier(!showEarlier)} mb={4}>
          {showEarlier ? 'Sakrij ranije stanice' : `Ranije stanice (${from})`}
        </Toggle>
      )}

      {rows.map((row, position) => {
        if (row.kind === 'more') {
          return (
            <div key="more" className={classes.stopRow} style={{ minHeight: 28 }}>
              <div className={classes.rail}>{vehicleHidden && <RailBus color={color} />}</div>
              <Toggle open={false} onClick={() => setShowAll(true)}>
                Još {row.count} {stopsWord(row.count)}
                {vehicleHidden ? ' · vozilo je ovdje' : ''}
              </Toggle>
            </div>
          )
        }

        const i = row.index
        const stopTime = stopTimes[i]
        const here = i === from
        const vehicleHere = state?.atStop && state.stopIndex === i
        const vehicleAfter = state && !state.atStop && state.stopIndex === i && i < last
        const shown = here ? stopTime.departureTime : stopTime.arrivalTime
        const travel = Math.round((stopTime.arrivalTime - stopTimes[from].departureTime) / 60)

        return (
          <Fragment key={`${stopTime.stopId}-${i}`}>
            <div
              className={classes.stopRow}
              data-first={position === 0 || undefined}
              data-last={i === last || undefined}
              data-current={here || undefined}
              data-passed={i < from || undefined}
            >
              <div className={classes.rail}>
                {vehicleHere ? <RailBus color={color} /> : <div className={classes.dot} />}
              </div>
              <Text size="sm" fw={here ? 600 : 400} style={{ flex: 1, minWidth: 0 }} lineClamp={1}>
                {stopNames.get(stopTime.stopId) ?? ''}
              </Text>
              <Text size="sm" fw={here ? 600 : 500} style={{ flexShrink: 0 }}>
                {i > from && (
                  <Text span size="xs" c="dimmed" mr={6}>
                    +{travel} min
                  </Text>
                )}
                {formatTime(shown, false)}
              </Text>
            </div>
            {vehicleAfter && (
              <div className={classes.stopRow} style={{ minHeight: 26 }}>
                <div className={classes.rail}>
                  <RailBus color={color} />
                </div>
                <Text size="xs" c="dimmed">
                  Vozilo je ovdje
                </Text>
              </div>
            )}
          </Fragment>
        )
      })}

      {showAll && last - from > nextStops + 1 && (
        <Toggle open onClick={() => setShowAll(false)} mt={4}>
          Prikaži manje stanica
        </Toggle>
      )}
    </Box>
  )
}

/** "stanica" ili "stanice" uz broj (2, 3, 4, 22… stanice). */
function stopsWord(count: number) {
  return count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 12 || count % 100 > 14)
    ? 'stanice'
    : 'stanica'
}

/** Koliko sljedećih stanica se vidi prije skupljenih. */
const nextStops = 3

type Row = { kind: 'stop'; index: number } | { kind: 'more'; count: number }

function Toggle({
  open,
  onClick,
  children,
  ...margins
}: {
  open: boolean
  onClick: () => void
  children: ReactNode
  mt?: number
  mb?: number
}) {
  return (
    <UnstyledButton onClick={onClick} {...margins}>
      <Text size="xs" c="blue.7" fw={500} span>
        {open ? (
          <IconChevronUp size={12} style={{ verticalAlign: 'middle' }} />
        ) : (
          <IconChevronDown size={12} style={{ verticalAlign: 'middle' }} />
        )}{' '}
        {children}
      </Text>
    </UnstyledButton>
  )
}

function RailBus({ color }: { color: string }) {
  return (
    <Box
      className={classes.vehicleOnRail}
      bg={color}
      c="white"
      w={22}
      h={22}
      style={{
        borderRadius: '50%',
        display: 'grid',
        placeItems: 'center',
        border: '2px solid #fff',
        boxShadow: '0 1px 3px rgba(0,0,0,.35)',
      }}
    >
      <IconBus size={12} />
    </Box>
  )
}
