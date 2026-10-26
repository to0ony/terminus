import { ActionIcon, Box, Group, UnstyledButton } from '@mantine/core'
import { IconChevronLeft, IconChevronRight } from '@tabler/icons-react'
import { useEffect, useRef } from 'react'
import { formatTime } from './clock'
import type { Departure } from './departures'
import classes from './LiveMapPage.module.css'

interface DepartureStripProps {
  departures: Departure[]
  selected: Departure
  onSelect: (departure: Departure) => void
}

/**
 * Vremena polazaka smjera u jednom redu, kao na Google kartama: ranija blago
 * siva, odabrano istaknuto, kasnija normalno. Bira se klikom, strelicama sa
 * strane ili tipkama lijevo i desno.
 */
export function DepartureStrip({ departures, selected, onSelect }: DepartureStripProps) {
  const index = departures.findIndex(
    (d) => d.tripId === selected.tripId && d.seconds === selected.seconds,
  )
  const buttons = useRef<(HTMLButtonElement | null)[]>([])

  // Odabrano vrijeme uvijek je vidljivo, u sredini reda.
  useEffect(() => {
    buttons.current[index]?.scrollIntoView({
      behavior: 'smooth',
      inline: 'center',
      block: 'nearest',
    })
  }, [index])

  const move = (step: number) => {
    const next = departures[index + step]
    if (next) {
      onSelect(next)
    }
  }

  return (
    <Group
      gap={4}
      wrap="nowrap"
      tabIndex={0}
      className={classes.strip}
      onKeyDown={(event) => {
        if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
          event.preventDefault()
          move(event.key === 'ArrowLeft' ? -1 : 1)
        }
      }}
    >
      <ActionIcon
        variant="subtle"
        color="gray"
        radius="xl"
        disabled={index <= 0}
        onClick={() => move(-1)}
        aria-label="Raniji polazak"
      >
        <IconChevronLeft size={18} />
      </ActionIcon>
      <Box className={classes.stripTimes}>
        <Group gap={6} wrap="nowrap" py={2}>
          {departures.map((departure, i) => (
            <UnstyledButton
              key={`${departure.tripId}-${departure.seconds}`}
              ref={(element) => {
                buttons.current[i] = element
              }}
              className={classes.time}
              data-state={i < index ? 'before' : i === index ? 'active' : 'after'}
              onClick={() => onSelect(departure)}
            >
              {formatTime(departure.seconds, false)}
            </UnstyledButton>
          ))}
        </Group>
      </Box>
      <ActionIcon
        variant="subtle"
        color="gray"
        radius="xl"
        disabled={index < 0 || index >= departures.length - 1}
        onClick={() => move(1)}
        aria-label="Kasniji polazak"
      >
        <IconChevronRight size={18} />
      </ActionIcon>
    </Group>
  )
}
