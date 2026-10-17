import { useEffect, useState } from 'react'
import { useParams, useSearchParams } from 'react-router'
import { ApiError } from '@/api/errors'
import {
  clockText,
  minutesShownUntil,
  minutesUntil,
  offsetMinutes,
  parseLimit,
  upcoming,
  whenText,
} from './board'
import classes from './BoardPage.module.css'
import { type BoardDeparture, refreshMs, useBoard, usePublicSettings } from './queries'

const defaultPrimary = '1565C0'
const defaultPrimaryText = 'FFFFFF'

/** Nakon koliko neuspjelih osvježavanja zaslon upozorava da podaci kasne. */
const staleAfterMs = 3 * refreshMs

/**
 * Javni zaslon stanice (bez prijave): sljedeći polasci s linijom, odredištem i
 * vremenom. Namjerno bez Mantine komponenti, da se brzo učitava i na slabijim
 * zaslonima na stanicama. Osvježava se sam; ?limit= mijenja broj polazaka.
 */
export function BoardPage() {
  const { stopCode = '' } = useParams()
  const [searchParams] = useSearchParams()
  const limit = parseLimit(searchParams.get('limit'))

  const board = useBoard(stopCode, limit)
  const settings = usePublicSettings()
  const now = useNow()

  const stopName = board.data?.departures.stop.name
  useEffect(() => {
    document.title = stopName ? `Polasci · ${stopName}` : 'Polasci'
  }, [stopName])

  const primary = settings.data?.primaryColor ?? defaultPrimary
  const primaryText = settings.data?.primaryTextColor ?? defaultPrimaryText
  const operator = settings.data?.displayName ?? board.data?.departures.agencyName

  // Vrijeme poslužitelja, ne preglednika.
  const serverNow = now + (board.data?.clockSkewMs ?? 0)
  const offset = board.data ? offsetMinutes(board.data.departures.now) : null
  const departures = board.data ? upcoming(board.data.departures.departures, serverNow) : []
  // Stari popis ostaje na zaslonu dok poslužitelj ne odgovori.
  const staleSince =
    board.data && board.isError && now - board.dataUpdatedAt > staleAfterMs
      ? clockText(board.dataUpdatedAt + board.data.clockSkewMs, offset ?? 0)
      : null

  return (
    <main className={classes.board}>
      <header
        className={classes.header}
        style={{ background: `#${primary}`, color: `#${primaryText}` }}
      >
        {settings.data?.logoUrl && (
          <img className={classes.logo} src={settings.data.logoUrl} alt="" />
        )}
        <div className={classes.heading}>
          {operator && <p className={classes.operator}>{operator}</p>}
          <h1 className={classes.stop}>
            <span className={classes.stopLabel}>Stanica </span>
            {stopName ?? stopCode}
          </h1>
        </div>
        {offset !== null && <div className={classes.clock}>{clockText(serverNow, offset)}</div>}
      </header>

      {board.isPending ? (
        <div className={classes.message}>Učitavanje polazaka…</div>
      ) : !board.data ? (
        <div className={classes.message}>
          {board.error instanceof ApiError && board.error.status === 404
            ? `Stanica „${stopCode}” ne postoji.`
            : 'Polasci trenutno nisu dostupni. Pokušavam ponovno…'}
        </div>
      ) : departures.length === 0 ? (
        <div className={classes.message}>Nema polazaka u sljedeća 24 sata.</div>
      ) : (
        <ol className={classes.list}>
          {departures.map((departure) => (
            <DepartureRow
              key={`${departure.departureTime}-${departure.routeName}-${departure.headsign}-${departure.platform}`}
              departure={departure}
              now={serverNow}
            />
          ))}
        </ol>
      )}

      {staleSince && (
        <footer className={`${classes.footer} ${classes.stale}`}>
          Polasci nisu osvježeni od {staleSince}.
        </footer>
      )}
    </main>
  )
}

function DepartureRow({ departure, now }: { departure: BoardDeparture; now: number }) {
  const soon = minutesUntil(departure.departureTime, now) <= minutesShownUntil

  return (
    <li className={classes.row}>
      <span
        className={classes.route}
        style={{
          background: departure.routeColor ? `#${departure.routeColor}` : undefined,
          color: departure.routeTextColor ? `#${departure.routeTextColor}` : undefined,
        }}
      >
        {departure.routeName}
      </span>
      <span className={classes.destination}>
        {departure.headsign}
        {departure.platform && <span className={classes.platform}>Peron {departure.platform}</span>}
      </span>
      <span className={soon ? `${classes.when} ${classes.soon}` : classes.when}>
        {whenText(departure.departureTime, now)}
      </span>
    </li>
  )
}

/** Trenutno vrijeme preglednika, osvježeno svake sekunde (za sat i minute do polaska). */
function useNow(): number {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [])
  return now
}
