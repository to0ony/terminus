# API

REST API pod `/api`. Točan opis svih zahtjeva i odgovora je OpenAPI dokument
na `/openapi/v1.json` (u okruženju `Development`); iz njega se generiraju i
TypeScript tipovi frontenda. Primjeri zahtjeva su u
[`Terminus.WebApi.http`](../backend/Terminus.WebApi/Terminus.WebApi.http).

## Prijava

Sve osim `/api/public/*`, `/api/auth/login` i `/health` traži prijavu
(inače `401`).

- Prijava: `POST /api/auth/login` s e-mailom i lozinkom postavlja kolačić
  `terminus_auth` (HttpOnly, SameSite=Strict, u produkciji samo preko HTTPS-a).
  Vrijedi 14 dana i produljuje se korištenjem.
- Nakon 5 neuspjelih pokušaja račun je zaključan 15 minuta (`429`).
- Ključevi za kolačiće čuvaju se u bazi, pa prijava preživi restart i deploy.
- Promjena lozinke (`PUT /api/auth/password`) odjavljuje ostale preglednike
  (najkasnije za 5 minuta).
- Prvi administrator dolazi iz konfiguracije (vidi
  [lokalni razvoj](development.md#konfiguracija)).

## Endpointi za administratora

| Resurs | Endpointi |
|---|---|
| Prijava | `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`, `PUT /api/auth/password` |
| Prvo pokretanje | `GET /api/setup`, `POST /api/setup/blank` |
| Postavke instance | `GET`/`PUT /api/settings`, `PUT`/`DELETE /api/settings/logo` |
| Prijevoznici | `/api/agencies`; `DELETE /api/agencies/{id}?moveRoutesTo=` linije i cijene prvo prebacuje na drugog prijevoznika |
| Stanice | `/api/stops`, `/api/stops/area`, `/api/stops/extent` |
| Stanica: linije i polasci | `/api/stops/{id}/routes`, `/api/stops/{id}/departures?date=` |
| Stanica: PDF vozni red | `GET /api/stops/{id}/timetable.pdf?from=&to=&paper=&language=` |
| Linije | `/api/routes`, `/api/routes/{id}/patterns`, `/api/routes/{id}/timetable?date=&directionId=` |
| Varijante linija | `/api/routes/{id}/variants`, `POST /api/routes/{id}/variants/derive` (iz vožnji), `/api/variants/{id}` |
| Linija: grafikon voznog reda | `GET`/`PUT /api/routes/{id}/schedule?directionId=&calendarId=` (vožnje s vremenima u sekundama; spremanje izmjena u jednoj transakciji, 409 kod sukoba) |
| Kalendari i iznimke | `/api/calendars`, `/api/calendars/overview?date=`, `/api/calendars/{id}/dates` |
| Sezone voznog reda | `/api/seasons`, `POST /api/seasons/{id}/copy` (kopija za drugo razdoblje, po želji s vožnjama), `PUT /api/seasons/{id}/day-types`, `PUT`/`DELETE /api/seasons/{id}/days/{date}`, `GET /api/seasons/holiday-countries` |
| Vožnje i vremena | `/api/trips`, `/api/trips/{id}/stop-times` |
| Trase | `/api/shapes` |
| Cijene karata | `/api/fares` |
| Izdavač feeda | `/api/feed-info` |
| GTFS uvoz osnove | `POST /api/gtfs/import/preview`, `POST /api/gtfs/import` (polje `variants`) |
| GTFS izvoz | `GET /api/gtfs` |

Resursi bez navedene metode imaju uobičajeni CRUD: `GET` popis (straničen,
`page` i `pageSize` ≤ 100), `GET /{id}`, `POST`, `PUT /{id}` (zamjenjuje cijeli
resurs) i `DELETE /{id}`.

Detalji pojedinih područja:

- polasci i PDF vozni red: [departures-and-timetables.md](departures-and-timetables.md);
- GTFS uvoz: [gtfs-import.md](gtfs-import.md);
- GTFS izvoz: [gtfs-export.md](gtfs-export.md).

## Javni endpointi (bez prijave)

| Resurs | Endpoint |
|---|---|
| Sljedeći polasci sa stanice | `GET /api/public/departures/{stopCode}?limit=10` |
| Naziv, boje i logo instance | `GET /api/public/settings`, `GET /api/public/settings/logo` |
| GTFS feed (ako je objavljen u postavkama) | `GET /api/public/gtfs` |
| Stanje aplikacije i baze | `GET /health` |

Javne endpointe stalno dohvaćaju zasloni na stanicama, pa imaju cache na
poslužitelju i ograničenje broja zahtjeva po IP adresi (iznad granice `429` s
`Retry-After`). Vrijednosti su u
[departures-and-timetables.md](departures-and-timetables.md#24-opterećenje-i-zaštita).

## Greške

Greške se vraćaju kao [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457):

| Status | Kada | Dodatno |
|---|---|---|
| `400` | neispravni podaci | `errors`: greške po polju |
| `401` | nije prijavljen | |
| `404` | resurs ne postoji | |
| `409` | sukob s postojećim podacima (npr. duplikat koda) | |
| `422` | podaci nisu spremni za operaciju (GTFS izvoz, polasci, PDF) | `problems`: popis što treba ispraviti |
| `429` | previše zahtjeva ili zaključan račun | `Retry-After` kod javnih endpointa |
