# Terminus

Terminus je aplikacija za izradu i upravljanje voznim redovima javnog prijevoza
u formatu [GTFS](https://gtfs.org/). Prijevoznici u njoj vode stanice, linije,
kalendare i vožnje te iz njih izvoze GTFS feed.

Terminus je single-tenant: svaka tvrtka (npr. gradski prijevoznik) dobiva
vlastitu instancu i bazu. Unutar instance može postojati više prijevoznika
(GTFS `agency`), a svaka linija pripada jednom od njih.

Planirane dodatne mogućnosti:

- PDF vozni redovi za pojedine stanice
- prikaz nadolazećih polazaka na stanicama

## Tehnologije

ASP.NET Core Web API (.NET 10), Entity Framework Core, PostgreSQL. Frontend
aplikacija (React, TypeScript, Vite) je u planu.

## Struktura repozitorija

| Mapa | Sadržaj |
|---|---|
| `backend/` | .NET rješenje `Terminus.slnx`: API, slojevi i testovi |
| `frontend/` | frontend aplikacija: dashboard i zaslon na stanici (u planu) |
| `docs/` | dokumentacija |

## Konfiguracija instance

| Ključ | Obavezno | Opis |
|---|---|---|
| `ConnectionStrings:Default` | da | Connection string za PostgreSQL. |
| `Agency:Name` | za seed | Naziv prijevoznika. |
| `Agency:Url` | za seed | Web adresa prijevoznika (GTFS `agency_url`). |
| `Agency:Timezone` | ne | IANA vremenska zona, zadano `Europe/Zagreb`. |
| `Agency:Lang` | ne | Oznaka jezika, zadano `hr`. |

Pri pokretanju, ako tablica `agencies` nema nijedan redak, aplikacija stvara
prijevoznika iz sekcije `Agency` (u svim okruženjima). Ako `Agency:Name` ili
`Agency:Url` nedostaje, u log se zapisuje upozorenje i aplikacija se normalno
pokreće. Postojeći prijevoznici se nikad ne mijenjaju. Ključevi se mogu zadati
i kao varijable okruženja (`Agency__Name`, `Agency__Url` …).

## API

| Resurs | URL |
|---|---|
| Prijevoznici | `/api/agencies` |
| Stanice | `/api/stops` |
| Linije | `/api/routes` (prijevoznik u tijelu zahtjeva: `agencyId`) |
| Kalendari i iznimke | `/api/calendars`, `/api/calendars/{id}/dates` |
| Vožnje i vremena | `/api/trips`, `/api/trips/{id}/stop-times` |
| GTFS izvoz | `/api/gtfs` |
| Polasci sa stanice (javno) | `/api/public/departures/{stopCode}?limit=10` |

## Dokumentacija

- [Lokalni razvoj](docs/development.md)
- [Arhitektura](docs/architecture.md)
- [Model podataka i GTFS](docs/data-model.md)
- [GTFS izvoz](docs/gtfs-export.md)
- [Polasci, zasloni i PDF vozni redovi (plan)](docs/departures-and-timetables.md)
- [Shema baze](docs/terminus_schema.sql)
