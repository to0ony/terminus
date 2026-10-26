import { Box, Text, UnstyledButton } from '@mantine/core'
import { IconBus, IconChevronDown, IconChevronUp } from '@tabler/icons-react'
import { type CSSProperties, Fragment, useState } from 'react'
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
 * Stanice odabranog polaska kao na Google kartama: od ove stanice do kraja,
 * s vremenom dolaska i koliko se vozi od ove stanice; ranije stanice su
 * skupljene. Vozilo u vožnji vidi se na crti.
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
  const stopTimes = vehicle.track.stopTimes

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
  const rail = { '--rail-color': color } as CSSProperties

  return (
    <Box style={rail}>
      {from > 0 && (
        <UnstyledButton onClick={() => setShowEarlier(!showEarlier)} mb={4}>
          <Text size="xs" c="dimmed" span>
            {showEarlier ? (
              <IconChevronUp size={12} style={{ verticalAlign: 'middle' }} />
            ) : (
              <IconChevronDown size={12} style={{ verticalAlign: 'middle' }} />
            )}{' '}
            {showEarlier ? 'Sakrij ranije stanice' : `Ranije stanice (${from})`}
          </Text>
        </UnstyledButton>
      )}

      {stopTimes.slice(start).map((stopTime, offset) => {
        const i = start + offset
        const here = i === from
        const vehicleHere = state?.atStop && state.stopIndex === i
        const vehicleAfter =
          state && !state.atStop && state.stopIndex === i && i < stopTimes.length - 1
        const shown = here ? stopTime.departureTime : stopTime.arrivalTime
        const travel = Math.round((stopTime.arrivalTime - stopTimes[from].departureTime) / 60)

        return (
          <Fragment key={`${stopTime.stopId}-${i}`}>
            <div
              className={classes.stopRow}
              data-first={i === start || undefined}
              data-last={i === stopTimes.length - 1 || undefined}
              data-current={here || undefined}
              data-passed={i < from || undefined}
            >
              <div className={classes.rail}>
                {vehicleHere ? <RailBus color={color} /> : <div className={classes.dot} />}
              </div>
              <Text size="sm" fw={here ? 600 : 400} style={{ flex: 1, minWidth: 0 }} lineClamp={1}>
                {stopNames.get(stopTime.stopId) ?? ''}
              </Text>
              <div style={{ textAlign: 'right', flexShrink: 0 }}>
                <Text size="sm" fw={here ? 600 : 500}>
                  {formatTime(shown, false)}
                </Text>
                {i > from && (
                  <Text size="xs" c="dimmed">
                    +{travel} min
                  </Text>
                )}
              </div>
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
    </Box>
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
