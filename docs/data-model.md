# Model podataka i GTFS

Model podataka prati [GTFS Schedule](https://gtfs.org/documentation/schedule/reference/)
(General Transit Feed Specification), uz nekoliko razlika koje olakšavaju
uređivanje podataka u aplikaciji.

## Tablice i GTFS datoteke

| Tablica | GTFS datoteka | GTFS ID | Napomena |
|---|---|---|---|
| `agencies` | `agency.txt` | — | Prijevoznik; ujedno tenant. |
| `stops` | `stops.txt` | `code` → `stop_id` | Stajališta, peroni i stanice (`location_type` 0/1). |
| `routes` | `routes.txt` | `code` → `route_id` | Linije. |
| `service_calendars` | `calendar.txt` | `code` → `service_id` | Tjedni obrazac vožnje u razdoblju. |
| `calendar_dates` | `calendar_dates.txt` | — | Iznimke kalendara po datumu. |
| `trips` | `trips.txt` | `code` → `trip_id` | Pojedinačne vožnje. |
| `stop_times` | `stop_times.txt` | — | Vremena vožnje po stanicama. |

Veze:

- `agencies` 1 → N `stops`, `routes`, `service_calendars`, `trips`
- `stops` 1 → N `stops` (peroni preko `parent_station_id`)
- `service_calendars` 1 → N `calendar_dates`
- `routes` 1 → N `trips`, `service_calendars` 1 → N `trips`
- `trips` 1 → N `stop_times`, `stops` 1 → N `stop_times`

## Razlike u odnosu na GTFS

**Surogat ključevi.** Svaka tablica ima `uuid` ključ, a GTFS identifikator
čuva se u stupcu `code`. GTFS ID se tako može mijenjati bez diranja veza, a
jedinstven je samo unutar agencije (`uq_<tablica>_agency_code`).

**Vremena u sekundama.** `arrival_time` i `departure_time` spremaju se kao broj
sekundi od ponoći dana vožnje, umjesto teksta `HH:MM:SS`. Vožnje koje prelaze
ponoć imaju vrijednosti iznad 24 h, kao u GTFS-u (`25:30:00` = `91800`).
Pretvorba u `HH:MM:SS` radi se na rubovima sustava (API, izvoz).

**Datumi.** U bazi su tipa `date`; u GTFS izvozu se zapisuju kao `YYYYMMDD`.

**Boje.** `routes.color` i `routes.text_color` spremaju se kao hex bez znaka
`#`, velikim slovima (`1565C0`), kako ih GTFS i očekuje.

**Aktivnost.** `stops.is_active` i `routes.is_active` nisu dio GTFS-a; služe za
privremeno isključivanje zapisa bez brisanja.

## Pravila koja provodi aplikacija

Ograničenja koja se mogu izraziti u bazi (raspon koordinata, dopuštene
vrijednosti enuma, redoslijed vremena …) postoje kao CHECK ograničenja.
Pravila koja zahtijevaju upit nad drugim recima provodi servisni sloj:

- nadređena stanica (`parent_station_id`) mora pripadati istoj agenciji i biti
  stanica (`location_type = 1`); stanica sama ne smije imati nadređenu;
- stanica koja ima perone ne može se pretvoriti u stajalište;
- linija i kalendar vožnje moraju pripadati istoj agenciji kao i vožnja
  (vožnje su u izradi);
- `agencies.url` i `agencies.timezone` (IANA naziv) obavezni su jer ih GTFS traži.

## Brisanje

| Brisanje | Posljedica |
|---|---|
| agencije | Kaskadno briše sve njezine podatke. |
| stanice | Peroni ostaju bez nadređene stanice (`SET NULL`); odbija se (409) ako se stanica koristi u `stop_times`. |
| linije | Kaskadno briše njezine vožnje i njihova vremena. |
| kalendara | Kaskadno briše iznimke; odbija se (409) ako ga koriste vožnje. |
| vožnje | Kaskadno briše njezina vremena. |
