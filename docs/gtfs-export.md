# GTFS izvoz

```
GET /api/agencies/{agencyId}/gtfs
```

Vraća GTFS Schedule feed agencije kao ZIP (`gtfs-<agencija>-<YYYYMMDD>.zip`).
Ako podaci nisu spremni za izvoz, vraća `422` s popisom problema umjesto
neispravnog feeda.

## Postupak

1. Učitavaju se svi podaci agencije (samo čitanje).
2. Odabire se sadržaj feeda:
   - aktivne linije (`is_active = true`);
   - vožnje tih linija;
   - kalendari koje te vožnje koriste i njihove iznimke;
   - stanice koje te vožnje koriste, uključujući nadređene stanice perona.
3. Provjerava se ispravnost (vidi dolje).
4. Podaci se pretvaraju u GTFS retke i zapisuju u ZIP.

Feed se generira u memoriji, što je za regionalnog prijevoznika reda veličine
nekoliko MB.

## Datoteke

| Datoteka | Sadržaj |
|---|---|
| `agency.txt` | agencija (bez `agency_id`, jer feed sadrži jednu agenciju) |
| `stops.txt` | stanice; `parent_station` je `code` nadređene stanice |
| `routes.txt` | aktivne linije |
| `trips.txt` | vožnje aktivnih linija |
| `stop_times.txt` | vremena vožnji |
| `calendar.txt` | korišteni kalendari |
| `calendar_dates.txt` | iznimke korištenih kalendara (samo ako postoje) |
| `feed_info.txt` | izdavač, jezik, razdoblje važenja i verzija feeda |

GTFS identifikatori (`stop_id`, `route_id`, `service_id`, `trip_id`) su vrijednosti
stupaca `code`. `feed_start_date` i `feed_end_date` obuhvaćaju razdoblja svih
kalendara i dodane datume iz iznimki. `feed_version` je vrijeme generiranja (UTC).

## Format

- UTF-8 bez BOM-a, retci završavaju s CRLF.
- Vrijednosti sa zarezom, navodnikom ili prijelomom retka stavljaju se u navodnike
  (RFC 4180).
- Vremena `HH:MM:SS`, uključujući vrijednosti iznad 24 h (`25:30:00`).
- Datumi `YYYYMMDD`.
- Decimalni brojevi uvijek s točkom, neovisno o kulturi poslužitelja.
- Logičke vrijednosti `1` / `0`; prazne vrijednosti su prazna polja.

## Provjere prije izvoza

Izvoz se odbija (`422`) ako:

- feed nema nijednu vožnju na aktivnoj liniji;
- vožnja nema postavljena vremena ili ima manje od dvije stanice;
- vožnja koristi neaktivnu stanicu;
- kalendar nikad ne vozi (nema označenih dana u tjednu ni dodanih datuma).

Odgovor sadrži sve pronađene probleme:

```json
{
  "status": 422,
  "title": "Podaci nisu spremni za ovu operaciju.",
  "detail": "GTFS feed se ne može izvesti dok se ne isprave navedeni problemi.",
  "problems": [
    "Vožnja 'OS-DJ-0615' nema postavljene stanice i vremena.",
    "Kalendar 'RD' nikad ne vozi: nema označenih dana u tjednu ni dodanih datuma."
  ]
}
```

## Provjera feeda

Izvezeni ZIP može se provjeriti službenim validatorom
[MobilityData GTFS Validator](https://gtfs-validator.mobilitydata.org/).
