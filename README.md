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
| `Agency:Code` | ne | GTFS `agency_id`; zadano se izvodi iz naziva (npr. `gpp`). |
| `Agency:Timezone` | ne | IANA vremenska zona, zadano `Europe/Zagreb`. |
| `Agency:Lang` | ne | Oznaka jezika, zadano `hr`. |
| `Admin:Email` | za seed | E-mail (korisničko ime) administratora. |
| `Admin:Password` | za seed | Početna lozinka administratora, barem 10 znakova. |

Pri pokretanju, ako tablica `agencies` nema nijedan redak, aplikacija stvara
prijevoznika iz sekcije `Agency` (u svim okruženjima). Ako `Agency:Name` ili
`Agency:Url` nedostaje, u log se zapisuje upozorenje i aplikacija se normalno
pokreće. Postojeći prijevoznici se nikad ne mijenjaju. Ključevi se mogu zadati
i kao varijable okruženja (`Agency__Name`, `Agency__Url` …).

Isto vrijedi za administratora: pri pokretanju, ako administrator s e-mailom
`Admin:Email` ne postoji, stvara se s lozinkom `Admin:Password`. Postojeći
administratori se nikad ne mijenjaju (promjena `Admin:Password` ne mijenja
lozinku), pa se novi administrator dodaje promjenom `Admin:Email` i ponovnim
pokretanjem. Lozinku administrator mijenja sam (`PUT /api/auth/password`).

## Prijava

Sve osim `/api/public/*`, `/api/auth/login` i `/health` traži prijavu
(inače `401`). Prijava je kolačićem `terminus_auth` (HttpOnly, SameSite=Strict,
u produkciji samo preko HTTPS-a), vrijedi 14 dana i produljuje se korištenjem.
Nakon 5 neuspjelih pokušaja račun je zaključan 15 minuta (`429`). Ključevi za
kolačiće čuvaju se u bazi (`data_protection_keys`), pa prijava preživi restart.

## API

| Resurs | URL |
|---|---|
| Prijava, odjava, prijavljeni korisnik, lozinka | `/api/auth/login`, `/api/auth/logout`, `/api/auth/me`, `/api/auth/password` |
| Postavke instance i logo | `/api/settings`, `/api/settings/logo` |
| Prijevoznici | `/api/agencies` |
| Stanice | `/api/stops`, `/api/stops/area` (karta) |
| Linije i polasci stanice | `/api/stops/{id}/routes`, `/api/stops/{id}/departures?date=` |
| Linije | `/api/routes` (prijevoznik u tijelu zahtjeva: `agencyId`) |
| Stanice na liniji i vozni red linije | `/api/routes/{id}/patterns`, `/api/routes/{id}/timetable?date=&directionId=` |
| Kalendari i iznimke | `/api/calendars`, `/api/calendars/{id}/dates` |
| Vožnje i vremena | `/api/trips`, `/api/trips/{id}/stop-times` |
| Trase | `/api/shapes` |
| Cijene karata | `/api/fares` |
| Izdavač feeda | `/api/feed-info` |
| GTFS izvoz | `/api/gtfs` |
| Polasci sa stanice (javno) | `/api/public/departures/{stopCode}?limit=10` |
| Naziv, boje i logo (javno) | `/api/public/settings`, `/api/public/settings/logo` |
| GTFS feed (javno, ako je uključen u postavkama) | `/api/public/gtfs` |
| Stanje aplikacije i baze (javno) | `/health` |

## Dokumentacija

- [Roadmap](docs/roadmap.md)
- [Lokalni razvoj](docs/development.md)
- [Arhitektura](docs/architecture.md)
- [Model podataka i GTFS](docs/data-model.md)
- [GTFS izvoz](docs/gtfs-export.md)
- [Polasci, zasloni i PDF vozni redovi (plan)](docs/departures-and-timetables.md)
- [Shema baze](docs/terminus_schema.sql)
